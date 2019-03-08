using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Presentation.Entities;
using Assets.Scripts.Presentation.Levels;
using Assets.Scripts.UI;
using UnityEngine;

namespace Assets.Scripts
{
	public class LevelComponent : MonoBehaviour
	{
		private LevelService levelService;
		private UiComponent ui;

		private List<EntityComponent> enemies;
		private bool selectionToggle;
		private bool selectionAttackTargetToggle;
		private bool bannerToggle;

		private void Awake()
		{
			// Load the level
			levelService = new LevelService();
			levelService.LoadLevel("Level1");

            // Grab all enemies
            //TODO: Remove enemies from list after death
            enemies = levelService.GetCharacters(EntityFaction.Enemy);

			ui = GameObject.Find("Canvas").GetComponent<UiComponent>();
		}

        private void StartTurn()
        {
            //Select player characters
            //levelService.GetEntities(EntityType.Player);

        }

        private void Update()
		{
			// How to detect what grid tile was clicked
			if (Input.GetMouseButtonDown(0))
			{
				Debug.Log("Clicked at grid coordinate " + LevelGrid.MouseToGridCoordinates());
			}

			if (Input.GetKeyDown(KeyCode.T))
			{
				bannerToggle = !bannerToggle;

				if (bannerToggle)
				{
					ui.ShowAndHideBanner("Player's turn");
				}
				else
				{
					ui.ShowAndHideBanner("Enemy turn");
				}
			}

			// This is how you move a character
			if (Input.GetKeyDown(KeyCode.M))
			{
				var enemy = enemies[0];
				enemy.Move(Direction.Down);
				enemy.Move(Direction.Right, 0.5f);
				enemy.Move(Direction.Down, 1);
				enemy.Move(Direction.Down, 1.5f);
				enemy.Move(Direction.Left, 2f);
			}

			// This is how you can trigger a quake animation :)
			if (Input.GetKeyDown(KeyCode.Q))
			{
				var x = Random.Range(0, levelService.LevelData.Width);
				var y = Random.Range(0, levelService.LevelData.Height);
				var radius = Random.Range(2, 8);
				levelService.PlayQuakeAnimation(x, y, radius);
			}

			// And this is how you trigger a damage animation
			if (Input.GetKeyDown(KeyCode.A))
			{
				enemies[0].PlayTakeDamageAnimation();
			}

			// This is how you alter the healthbar for an entity
			if (Input.GetKeyDown(KeyCode.H))
			{
				enemies[0].PlayHealthBarAnimation(Random.Range(0f, 1f));
			}

			// How to select a character
			if (Input.GetKeyDown(KeyCode.S))
			{
				selectionToggle = !selectionToggle;
				enemies[0].ShowSelection(selectionToggle);
			}

			// How to select a character (as an attack target)
			if (Input.GetKeyDown(KeyCode.X))
			{
				selectionAttackTargetToggle = !selectionAttackTargetToggle;
				enemies[0].ShowSelectionAttackTarget(selectionAttackTargetToggle);
			}

			// How to kill a character
			if (Input.GetKeyDown(KeyCode.D))
			{
				enemies[0].PlayDeathAnimation();
			}

			// How to show a breadcrumbs path
			if (Input.GetKeyDown(KeyCode.B))
			{
				levelService.ShowBreadCrumb(5, 1, true);
				levelService.ShowBreadCrumb(5, 2, true, 0.1f);
				levelService.ShowBreadCrumb(5, 3, true, 0.2f);
				levelService.ShowBreadCrumb(5, 4, true, 0.3f);
				levelService.ShowBreadCrumb(4, 4, true, 0.4f);
			}
			// And how to hide it...
			else if (Input.GetKeyDown(KeyCode.V))
			{
				levelService.HideAllBreadCrumbs();
			}
		}
	}
}