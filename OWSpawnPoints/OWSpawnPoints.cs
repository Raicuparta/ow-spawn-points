using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using OWML.ModHelper.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using OWML.Common.Menus;

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

            var shipSpawnMenu = ModHelper.Menus.PauseMenu.Copy("Ship Spawn Points");
            shipSpawnMenu.Buttons.ForEach(button => button.Hide());
            shipSpawnMenu.Menu.transform.localScale *= 0.5f;
            shipSpawnMenu.Menu.transform.localPosition *= 0.5f;

            var playerSpawnMenu = ModHelper.Menus.PauseMenu.Copy("Player Spawn Points");
            playerSpawnMenu.Buttons.ForEach(button => button.Hide());
            playerSpawnMenu.Menu.transform.localScale *= 0.5f;
            playerSpawnMenu.Menu.transform.localPosition *= 0.5f;

            var sourceButton = shipSpawnMenu.Buttons[0];

            mainButton.OnClick += () => (PlayerState.IsInsideShip() ? shipSpawnMenu : playerSpawnMenu).Open();

            var astroObjects = FindObjectsOfType<AstroObject>().ToList();
            var astroSpawnPoints = new Dictionary<AstroObject, SpawnPoint[]>();

            foreach (var astroObject in astroObjects)
            {
                astroSpawnPoints[astroObject] = astroObject.GetComponentsInChildren<SpawnPoint>(true);
            }

            astroObjects.Sort((a, b) => astroSpawnPoints[a].Length.CompareTo(astroSpawnPoints[b].Length));

            foreach (var astroObject in astroObjects)
            {
                var allSpawnPoints = astroSpawnPoints[astroObject];
                if (allSpawnPoints.Length == 0)
                {
                    continue;
                }

                var shipSpawnPoints = allSpawnPoints.Where(point => point.IsShipSpawn()).ToList();
                var playerSpawnPoints = allSpawnPoints.Where(point => !point.IsShipSpawn()).ToList();

                var astroNameEnum = astroObject.GetAstroObjectName();
                var astroName = astroNameEnum.ToString();

                if (astroNameEnum == AstroObject.Name.CustomString)
                {
                    astroName = astroObject.GetCustomName();
                }
                else if (astroNameEnum == AstroObject.Name.None || astroName == null || astroName == "")
                {
                    astroName = astroObject.name;
                }

                void CreateSpawnPointButton(SpawnPoint spawnPoint, IModPopupMenu spawnMenu, string name)
                {
                    var subButton = spawnMenu.AddButton(sourceButton.Copy(name));
                    subButton.OnClick += () =>
                    {
                        spawnMenu.Close();
                        shipSpawnMenu.Close();
                        playerSpawnMenu.Close();
                        ModHelper.Menus.PauseMenu.Close();
                        SpawnAt(spawnPoint);
                    };
                    subButton.Show();
                }

                void CreateSpawnPointList(List<SpawnPoint> spawnPoints, IModPopupMenu spawnMenu)
                {
                    var subMenu = ModHelper.Menus.PauseMenu.Copy("Spawn Points");
                    subMenu.Buttons.ForEach(button => button.Hide());
                    subMenu.Menu.transform.localScale *= 0.5f;
                    subMenu.Menu.transform.localPosition *= 0.5f;

                    var subButton = spawnMenu.AddButton(sourceButton.Copy($"{astroName}..."));
                    subButton.OnClick += () => subMenu.Open();
                    subButton.Show();

                    for (var i = 0; i < spawnPoints.Count; i++)
                    {
                        var point = spawnPoints[i];
                        CreateSpawnPointButton(point, subMenu, point.name);
                    }
                }

                if (shipSpawnPoints.Count > 1)
                {
                    CreateSpawnPointList(shipSpawnPoints, shipSpawnMenu);
                }
                else if (shipSpawnPoints.Count == 1)
                {
                    CreateSpawnPointButton(shipSpawnPoints[0], shipSpawnMenu, astroName);
                }

                if (playerSpawnPoints.Count > 1)
                {
                    CreateSpawnPointList(playerSpawnPoints, playerSpawnMenu);
                }
                else if (playerSpawnPoints.Count == 1)
                {
                    CreateSpawnPointButton(playerSpawnPoints[0], playerSpawnMenu, astroName);
                }
            }
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
        }
    }
}
