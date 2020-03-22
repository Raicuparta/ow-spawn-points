using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using OWML.ModHelper.Events;



namespace OWSpawnPoints
{
    public class OWSpawnPoints : ModBehaviour
    {
        private FluidDetector _fluidDetector;

        private void Start()
        {
            ModHelper.Events.Subscribe<Flashlight>(Events.AfterStart);
            ModHelper.Events.OnEvent += OnEvent;
        }

        private void OnEvent(MonoBehaviour behaviour, Events ev)
        {
            if (behaviour.GetType() == typeof(Flashlight) && ev == Events.AfterStart)
            {
                Init();
            }
        }

        private void Init()
        {
            _fluidDetector = Locator.GetPlayerCamera().GetComponentInChildren<FluidDetector>();

            var mainButton = ModHelper.Menus.PauseMenu.OptionsButton.Duplicate("Teleport to...");
            var spawnMenu = ModHelper.Menus.PauseMenu.Copy("Spawn Points");
            var sourceButton = spawnMenu.Buttons[0];

            foreach (var button in spawnMenu.Buttons)
            {
                button.Hide();
            }

            mainButton.OnClick += () => spawnMenu.Open();
            spawnMenu.Menu.transform.localScale *= 0.5f;
            spawnMenu.Menu.transform.localPosition *= 0.5f;

            var astroObjects = FindObjectsOfType<AstroObject>();

            foreach (var astroObject in astroObjects)
            {
                var spawnPoints = astroObject.GetComponentsInChildren<SpawnPoint>(true);

                var astroName = astroObject.GetAstroObjectName();
                var name = astroName.ToString();

                if (astroName == AstroObject.Name.CustomString)
                {
                    name = astroObject.GetCustomName();
                }
                else if (astroName == AstroObject.Name.None || name == null || name == "")
                {
                    name = astroObject.name;
                }

                if (spawnPoints.Length == 0)
                {
                    continue;
                }

                var subMenu = ModHelper.Menus.PauseMenu.Copy(name);
                foreach (var button in subMenu.Buttons)
                {
                    button.Hide();
                }

                var subButton = spawnMenu.AddButton(sourceButton.Copy(name));
                subButton.OnClick += () => subMenu.Open();
                subButton.Show();

                var shipMenu = ModHelper.Menus.PauseMenu.Copy("Ship Spawns");
                foreach (var button in shipMenu.Buttons)
                {
                    button.Hide();
                }

                var playerMenu = ModHelper.Menus.PauseMenu.Copy("Player Spawns");
                playerMenu.Menu.transform.localScale *= 0.5f;
                playerMenu.Menu.transform.localPosition *= 0.5f;
                foreach (var button in playerMenu.Buttons)
                {
                    button.Hide();
                }

                var shipMenuButton = subMenu.AddButton(sourceButton.Copy("Ship Spawns"));
                shipMenuButton.OnClick += () => shipMenu.Open();
                shipMenuButton.Show();

                var playerMenuButton = subMenu.AddButton(sourceButton.Copy("Player Spawns"));
                playerMenuButton.OnClick += () => playerMenu.Open();
                playerMenuButton.Show();

                var shipCount = 0;
                var playerCount = 0;
                for (var i = 0; i < spawnPoints.Length; i++)
                {
                    var point = spawnPoints[i];

                    var menu = point.IsShipSpawn() ? shipMenu : playerMenu;

                    if (point.IsShipSpawn())
                    {
                        shipCount++;
                    }
                    else
                    {
                        playerCount++;
                    }

                    var spawnButton = menu.AddButton(sourceButton.Copy(point.name));
                    spawnButton.OnClick += () => SpawnAt(point);
                    spawnButton.Show();
                }

                if (shipCount == 0)
                {
                    shipMenuButton.Hide();
                }
                if (playerCount == 0)
                {
                    playerMenuButton.Hide();
                }
            }
        }

        private void SpawnAt(SpawnPoint point)
        {
            OWRigidbody playerBody = Locator.GetPlayerBody();
            playerBody.WarpToPositionRotation(point.transform.position, point.transform.rotation);
            playerBody.SetVelocity(point.GetPointVelocity());
            point.AddObjectToTriggerVolumes(Locator.GetPlayerDetector().gameObject);
            point.AddObjectToTriggerVolumes(_fluidDetector.gameObject);
            point.OnSpawnPlayer();
        }
    }
}
