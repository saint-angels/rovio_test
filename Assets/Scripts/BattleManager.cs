using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Presentation.Entities;
using Assets.Scripts.Presentation.Levels;
using Assets.Scripts.UI;
using Pathfinding;
using UnityEngine;

namespace Assets.Scripts
{
	public class BattleManager : MonoBehaviour
	{
		private LevelService levelService;
		private UiComponent ui;

        private EntityComponent selectedCharacter;
		private List<EntityComponent> enemies;
		private bool selectionToggle;
		private bool selectionAttackTargetToggle;
		private bool bannerToggle;
        private int walkDistance = 6;

        private List<EntityComponent> movablePlayerCharacters = new List<EntityComponent>();
        private List<EntityComponent> attackingPlayerCharacters = new List<EntityComponent>();

        private void Awake()
		{
			// Load the level
			levelService = new LevelService();
			levelService.LoadLevel("Level1");

            var graph = AstarPath.active.data.pointGraph;
            graph.limits = new Vector3(LevelGrid.TileSize.x, LevelGrid.TileSize.y);
            graph.Scan();

            foreach (var node in graph.nodes)
            {
                Vector3 nodeWorldPosition = (Vector3)node.position;
                Vector2Int nodeGridPosition = LevelGrid.ToGridCoordinates(nodeWorldPosition.x, nodeWorldPosition.y);
                EntityComponent entityAtNode = levelService.GetEntityAtPosition(nodeGridPosition.x, nodeGridPosition.y);
                if (entityAtNode != null && entityAtNode.Type == EntityType.Obstacle)
                {
                    node.Walkable = false;
                }
            }

            // Grab all enemies
            //TODO: Remove enemies from list after death
            enemies = levelService.GetCharacters(EntityFaction.Enemy);

			ui = GameObject.Find("Canvas").GetComponent<UiComponent>();
        }

        private void StartTurn()
        {
            //Select player characters
            List<EntityComponent> playerCharacters = levelService.GetCharacters(EntityFaction.Player);
            if (playerCharacters.Count == 0)
            {
                Debug.LogError("Turn started without any player characters!");
                return;
            }

            movablePlayerCharacters.Clear();
            movablePlayerCharacters.AddRange(levelService.GetCharacters(EntityFaction.Player));
            attackingPlayerCharacters.AddRange(movablePlayerCharacters);
        }

        private void SelectPlayerCharacter(EntityComponent selectedCharacter)
        {
            SelectCharacter(selectedCharacter);

            int minX = Mathf.Max(selectedCharacter.GridPosition.x - walkDistance, 0);
            int maxX = Mathf.Min(selectedCharacter.GridPosition.x + walkDistance, levelService.GridSize.x);
            int minY = Mathf.Max(selectedCharacter.GridPosition.y - walkDistance, 0);
            int maxY = Mathf.Min(selectedCharacter.GridPosition.y + walkDistance, levelService.GridSize.y);

            levelService.HideAllBreadCrumbs();

            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    int characterOffsetX = Mathf.Abs(x - selectedCharacter.GridPosition.x);
                    int characterOffsetY = Mathf.Abs(y - selectedCharacter.GridPosition.y);
                    if (characterOffsetX + characterOffsetY <= walkDistance)
                    {
                        levelService.SetBreadCrumbVisible(x, y, true, .1f*(characterOffsetX + characterOffsetY));
                    }
                }
            }
        }

        private void MoveCharacter(EntityComponent character, Vector2Int coordinates)
        {
            Vector2 worldPositionTarget = LevelGrid.ToWorldCoordinates(coordinates.x, coordinates.y);
            Vector2 worldPositionCurrent = LevelGrid.ToWorldCoordinates(character.GridPosition.x, character.GridPosition.y);

            var path = ABPath.Construct(worldPositionCurrent, worldPositionTarget, null);
            AstarPath.StartPath(path);
            path.BlockUntilCalculated();

            if (path.error)
            {
                Debug.LogError("No path was found");
                return;
            }

            Vector2Int previousCoordinates = character.GridPosition;
            for (int nodeIdx = 1; nodeIdx < path.vectorPath.Count; nodeIdx++)
            {
                float moveDelay = .5f * (nodeIdx - 1);
                Vector3 nodePosition = path.vectorPath[nodeIdx];
                Vector2Int nextNodeCoordinates = LevelGrid.ToGridCoordinates(nodePosition.x, nodePosition.y);
                if (nextNodeCoordinates.x > previousCoordinates.x)
                {
                    character.Move(Direction.Right, moveDelay);
                }
                else if (nextNodeCoordinates.x < previousCoordinates.x)
                {
                    character.Move(Direction.Left, moveDelay);
                }
                else if (nextNodeCoordinates.y > previousCoordinates.y)
                {
                    character.Move(Direction.Down, moveDelay);
                }
                else if (nextNodeCoordinates.y < previousCoordinates.y)
                {
                    character.Move(Direction.Up, moveDelay);
                }

                previousCoordinates = nextNodeCoordinates;
            }

            foreach (Vector3 nodePosition in path.vectorPath)
            {
                print(LevelGrid.ToGridCoordinates(nodePosition.x, nodePosition.y));
            }

            SelectCharacter(null);
        }

        private void SelectCharacter(EntityComponent target)
        {
            this.selectedCharacter = target;

            foreach (var entity in levelService.GetEntities())
            {
                entity.ShowSelection(entity == target);
            }
        }

        private void Update()
		{
			// How to detect what grid tile was clicked
			if (Input.GetMouseButtonDown(0))
			{
                Vector2Int clickedCoordinates = LevelGrid.MouseToGridCoordinates();
                print("Clicked on " + clickedCoordinates);
                EntityComponent clickedEntity = levelService.GetEntityAtPosition(clickedCoordinates.x, clickedCoordinates.y);
                bool clickedOnPlayerCharacter = clickedEntity != null && clickedEntity.Type == EntityType.Character && clickedEntity.Faction == EntityFaction.Player;
                if (clickedOnPlayerCharacter)
                {
                    SelectPlayerCharacter(clickedEntity);
                }
                else if(clickedEntity == null && selectedCharacter != null)
                {
                    MoveCharacter(selectedCharacter, clickedCoordinates);
                }
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
				levelService.SetBreadCrumbVisible(5, 1, true);
				levelService.SetBreadCrumbVisible(5, 2, true, 0.1f);
				levelService.SetBreadCrumbVisible(5, 3, true, 0.2f);
				levelService.SetBreadCrumbVisible(5, 4, true, 0.3f);
				levelService.SetBreadCrumbVisible(4, 4, true, 0.4f);
			}
			// And how to hide it...
			else if (Input.GetKeyDown(KeyCode.V))
			{
				levelService.HideAllBreadCrumbs();
			}
		}
	}
}