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

            var astroObjects = FindObjectsOfType<AstroObject>();

            foreach (var astroObject in astroObjects)
            {
                var allSpawnPoints = astroObject.GetComponentsInChildren<SpawnPoint>(true);
                var shipSpawnPoints = allSpawnPoints.Where(point => point.IsShipSpawn()).ToList();
                var playerSpawnPoints = allSpawnPoints.Where(point => !point.IsShipSpawn()).ToList();

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

                if (allSpawnPoints.Length == 0)
                {
                    continue;
                }

                void CreateSpawnPointList(List<SpawnPoint> spawnPoints, IModPopupMenu spawnMenu)
                {
                    var subMenu = ModHelper.Menus.PauseMenu.Copy("Spawn Points");
                    subMenu.Buttons.ForEach(button => button.Hide());
                    subMenu.Menu.transform.localScale *= 0.5f;
                    subMenu.Menu.transform.localPosition *= 0.5f;

                    var subButton = spawnMenu.AddButton(sourceButton.Copy(name));
                    subButton.OnClick += () => subMenu.Open();
                    subButton.Show();

                    var shipCount = 0;
                    var playerCount = 0;
                    for (var i = 0; i < spawnPoints.Count; i++)
                    {
                        var point = spawnPoints[i];

                        if (point.IsShipSpawn())
                        {
                            shipCount++;
                        }
                        else
                        {
                            playerCount++;
                        }

                        var spawnButton = subMenu.AddButton(sourceButton.Copy(point.name));
                        spawnButton.OnClick += () => SpawnAt(point);
                        spawnButton.Show();
                    }
                }

                if (shipSpawnPoints.Count > 0)
                {
                    CreateSpawnPointList(shipSpawnPoints, shipSpawnMenu);
                }

                if (playerSpawnPoints.Count > 0)
                {
                    CreateSpawnPointList(playerSpawnPoints, playerSpawnMenu);
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
