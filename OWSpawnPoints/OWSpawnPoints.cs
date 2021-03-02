using OWML.Common;
using OWML.Common.Menus;
using OWML.ModHelper;
using OWML.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OWSpawnPoints
{
    public class OWSpawnPoints : ModBehaviour
    {
        private FluidDetector _fluidDetector;
        private SpawnPoint _prevSpawnPoint;
        private SaveFile _saveFile;
        private bool _isSolarSystemLoaded;
        private const string SAVE_FILE = "savefile.json";
        private bool _suitUpOnTravel = true;

        private void Start()
        {
            ModHelper.Events.Subscribe<Flashlight>(Events.AfterStart);
            ModHelper.Events.Event += OnEvent;

            _saveFile = ModHelper.Storage.Load<SaveFile>(SAVE_FILE);

            LoadManager.OnCompleteSceneLoad += OnSceneLoaded;
        }

        public override void Configure(IModConfig config)
            => _suitUpOnTravel = config.GetSettingsValue<bool>("suitUpOnTravel");

        private void OnSceneLoaded(OWScene originalScene, OWScene scene)
        {
            if (scene == OWScene.SolarSystem
                || scene == OWScene.EyeOfTheUniverse)
            {
                _isSolarSystemLoaded = true;
                SpawnAtInitialPoint();
            }
        }

        private void OnEvent(MonoBehaviour behaviour, Events ev)
        {
            if (behaviour.GetType() == typeof(Flashlight)
                && ev == Events.AfterStart)
            {
                Init();
                SpawnAtInitialPoint();
            }
        }

        private void Init()
        {
            _fluidDetector = Locator.GetPlayerCamera().GetComponentInChildren<FluidDetector>();

            var mainButton = ModHelper.Menus.PauseMenu.OptionsButton.Duplicate("TELEPORT");

            var shipSpawnMenu = ModHelper.Menus.PauseMenu.Copy("Ship Spawn Points");
            shipSpawnMenu.Buttons.ForEach(button => button.Hide());
            shipSpawnMenu.Menu.transform.localScale *= 0.5f;
            shipSpawnMenu.Menu.transform.localPosition *= 0.5f;

            var playerSpawnMenu = ModHelper.Menus.PauseMenu.Copy("Player Spawn Points");
            playerSpawnMenu.Buttons.ForEach(button => button.Hide());
            playerSpawnMenu.Menu.transform.localScale *= 0.5f;
            playerSpawnMenu.Menu.transform.localPosition *= 0.5f;

            var sourceButton = shipSpawnMenu.Buttons[0];

            mainButton.OnClick += ()
                => (PlayerState.IsInsideShip()
                    ? shipSpawnMenu
                    : playerSpawnMenu)
                .Open();

            var spawnPointsWithNoAO = Resources.FindObjectsOfTypeAll<SpawnPoint>().ToList();
            var astroObjects = Resources.FindObjectsOfTypeAll<AstroObject>().ToList();
            var astroSpawnPoints = new Dictionary<AstroObject, SpawnPoint[]>();

            foreach (var astroObject in astroObjects)
            {
                var attachedSpawnPoints = astroObject.GetComponentsInChildren<SpawnPoint>(true);
                astroSpawnPoints[astroObject] = attachedSpawnPoints;
                spawnPointsWithNoAO = spawnPointsWithNoAO.Except(attachedSpawnPoints).ToList();
            }

            astroObjects.Sort((a, b) => astroSpawnPoints[a].Length.CompareTo(astroSpawnPoints[b].Length));

            void CloseMenu()
            {
                shipSpawnMenu.Close();
                playerSpawnMenu.Close();
                ModHelper.Menus.PauseMenu.Close();
            }

            void CreateSpawnPointButton(SpawnPoint spawnPoint, IModPopupMenu spawnMenu, string name)
            {
                var subButton = spawnMenu.AddButton(sourceButton.Copy(name));
                subButton.OnClick += () =>
                {
                    spawnMenu.Close();
                    CloseMenu();
                    SpawnAt(spawnPoint);
                    _prevSpawnPoint = spawnPoint;
                };
                subButton.Show();
            }

            void CreateSpawnPointList(List<SpawnPoint> spawnPoints, AstroObject astroObject, IModPopupMenu spawnMenu)
            {
                var subMenu = ModHelper.Menus.PauseMenu.Copy("Spawn Points");
                subMenu.Buttons.ForEach(button => button.Hide());
                subMenu.Menu.transform.localScale *= 0.5f;
                subMenu.Menu.transform.localPosition *= 0.5f;

                var subButton = spawnMenu.AddButton(sourceButton.Copy($"{GetAstroObjectName(astroObject)}..."));
                subButton.OnClick += () => subMenu.Open();
                subButton.Show();

                for (var i = 0; i < spawnPoints.Count; i++)
                {
                    var point = spawnPoints[i];
                    CreateSpawnPointButton(point, subMenu, point.name);
                }
            }

            void CreateMiscSpawnPointList(List<SpawnPoint> spawnPoints, IModPopupMenu spawnMenu)
            {
                var subMenu = ModHelper.Menus.PauseMenu.Copy("Spawn Points");
                subMenu.Buttons.ForEach(button => button.Hide());
                subMenu.Menu.transform.localScale *= 0.5f;
                subMenu.Menu.transform.localPosition *= 0.5f;

                var subButton = spawnMenu.AddButton(sourceButton.Copy("<No Set AstroObject>"));
                subButton.OnClick += () => subMenu.Open();
                subButton.Show();

                for (var i = 0; i < spawnPoints.Count; i++)
                {
                    var point = spawnPoints[i];
                    CreateSpawnPointButton(point, subMenu, point.name);
                }
            }

            foreach (var astroObject in astroObjects)
            {
                var allSpawnPoints = astroSpawnPoints[astroObject];
                if (allSpawnPoints.Length == 0)
                {
                    continue;
                }

                var shipSpawnPoints = allSpawnPoints.Where(point => point.IsShipSpawn()).ToList();
                var playerSpawnPoints = allSpawnPoints.Where(point => !point.IsShipSpawn()).ToList();

                var astroName = GetAstroObjectName(astroObject);

                if (shipSpawnPoints.Count > 1)
                {
                    CreateSpawnPointList(shipSpawnPoints, astroObject, shipSpawnMenu);
                }
                else if (shipSpawnPoints.Count == 1)
                {
                    CreateSpawnPointButton(shipSpawnPoints[0], shipSpawnMenu, $"{astroName} - {shipSpawnPoints[0].name}");
                }

                if (playerSpawnPoints.Count > 1)
                {
                    CreateSpawnPointList(playerSpawnPoints, astroObject, playerSpawnMenu);
                }
                else if (playerSpawnPoints.Count == 1)
                {
                    CreateSpawnPointButton(playerSpawnPoints[0], playerSpawnMenu, $"{astroName} - {shipSpawnPoints[0].name}");
                }
            }

            var miscShipSpawns = spawnPointsWithNoAO.Where(point => point.IsShipSpawn()).ToList();
            var miscPlayerSpawns = spawnPointsWithNoAO.Where(point => !point.IsShipSpawn()).ToList();

            if (miscShipSpawns.Count > 1)
            {
                CreateMiscSpawnPointList(miscShipSpawns, shipSpawnMenu);
            }
            else if (miscShipSpawns.Count == 1)
            {
                CreateSpawnPointButton(miscShipSpawns[0], shipSpawnMenu, miscShipSpawns[0].name);
            }

            if (miscPlayerSpawns.Count > 1)
            {
                CreateMiscSpawnPointList(miscPlayerSpawns, playerSpawnMenu);
            }
            else if (miscPlayerSpawns.Count == 1)
            {
                CreateSpawnPointButton(miscPlayerSpawns[0], playerSpawnMenu, miscPlayerSpawns[0].name);
            }

            var clearSaveButton = sourceButton.Copy("RESET INITIAL SPAWN POINT");
            clearSaveButton.OnClick += () =>
            {
                ResetInitialSpawnPoint();
                CloseMenu();
            };
            clearSaveButton.Show();
            shipSpawnMenu.AddButton(clearSaveButton);
            playerSpawnMenu.AddButton(clearSaveButton);

            var saveButton = sourceButton.Copy("SAVE LAST USED AS INITIAL");
            saveButton.OnClick += () =>
            {
                SetInitialSpawnPoint();
                CloseMenu();
            };
            saveButton.Show();
            shipSpawnMenu.AddButton(saveButton);
            playerSpawnMenu.AddButton(saveButton);
        }

        private string GetAstroObjectName(AstroObject astroObject)
        {
            var astroNameEnum = astroObject.GetAstroObjectName();
            var astroName = astroNameEnum.ToString();

            if (astroNameEnum == AstroObject.Name.CustomString)
            {
                return astroObject.GetCustomName();
            }
            else if (astroNameEnum == AstroObject.Name.None
                || astroName == null
                || astroName == "")
            {
                return astroObject.name;
            }

            return astroName;
        }

        private void SetInitialSpawnPoint()
        {
            _saveFile.initialSpawnPoint = _prevSpawnPoint.gameObject.name;
            ModHelper.Storage.Save(_saveFile, SAVE_FILE);
        }

        private void ResetInitialSpawnPoint()
        {
            _saveFile.initialSpawnPoint = "";
            ModHelper.Storage.Save(_saveFile, SAVE_FILE);
        }

        private void SpawnAtInitialPoint()
        {
            var spawnPointName = _saveFile.initialSpawnPoint;
            if (spawnPointName == "")
            {
                return;
            }
            var point = FindObjectsOfType<SpawnPoint>().First(x => x.gameObject.name == spawnPointName);
            FindObjectOfType<PlayerSpawner>().SetInitialSpawnPoint(point);
        }

        private void SpawnAt(SpawnPoint point)
        {
            var body = PlayerState.IsInsideShip() ? Locator.GetShipBody() : Locator.GetPlayerBody();

            body.WarpToPositionRotation(point.transform.position, point.transform.rotation);
            body.SetVelocity(point.GetPointVelocity());
            point.AddObjectToTriggerVolumes(Locator.GetPlayerDetector().gameObject);
            point.AddObjectToTriggerVolumes(_fluidDetector.gameObject);
            point.OnSpawnPlayer();
            OWTime.Unpause(OWTime.PauseType.Menu);

            if (_suitUpOnTravel)
            {
                Locator.GetPlayerSuit().SuitUp();
            }
        }

        private void InstantWakeUp()
        {
            _isSolarSystemLoaded = false;
            // Skip wake up animation.
            var cameraEffectController = FindObjectOfType<PlayerCameraEffectController>();
            cameraEffectController.OpenEyes(0, true);
            cameraEffectController.SetValue("_wakeLength", 0f);
            cameraEffectController.SetValue("_waitForWakeInput", false);

            // Skip wake up prompt.
            LateInitializerManager.pauseOnInitialization = false;
            Locator.GetPauseCommandListener().RemovePauseCommandLock();
            Locator.GetPromptManager().RemoveScreenPrompt(cameraEffectController.GetValue<ScreenPrompt>("_wakePrompt"));
            OWTime.Unpause(OWTime.PauseType.Sleeping);
            cameraEffectController.Invoke("WakeUp");

            // Enable all inputs immedeately.
            OWInput.ChangeInputMode(InputMode.Character);
            typeof(OWInput).SetValue("_inputFadeFraction", 0f);
            GlobalMessenger.FireEvent("TakeFirstFlashbackSnapshot");

            Locator.GetPlayerSuit().SuitUp();
        }

        private void LateUpdate()
        {
            if (_isSolarSystemLoaded
                && _saveFile.initialSpawnPoint != "")
            {
                InstantWakeUp();
            }
        }
    }
}
