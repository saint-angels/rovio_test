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
        enum TurnState
        {
            USER_IDLE,
            USER_CHAR_SELECTED,
            CHARACTER_ANIMATION,
        }

		private LevelService levelService;
		private UIComponent ui;
        private GridNavigator gridNavigator;
        private InputSystem inputSystem;

        private EntityComponent selectedCharacter;
		private List<EntityComponent> enemies;
		private bool selectionToggle;
		private bool selectionAttackTargetToggle;
		private bool bannerToggle;
        private int walkDistance = 6;

        private List<EntityComponent> movablePlayerCharacters = new List<EntityComponent>();
        private List<EntityComponent> attackingPlayerCharacters = new List<EntityComponent>();

        private TurnState turnState;

        private List<Vector2Int> characterAvailableDestinationsCache = new List<Vector2Int>();

        private void Start()
		{
			// Load the level
			levelService = new LevelService();
			levelService.LoadLevel("Level1");

            // Grab all enemies
            //TODO: Remove enemies from list after death
            enemies = levelService.GetCharacters(EntityFaction.Enemy);

			ui = GameObject.Find("Canvas").GetComponent<UIComponent>();
            ui.OnEndTurnClicked += OnEndTurnClicked;

            gridNavigator = GetComponent<GridNavigator>() ?? gameObject.AddComponent<GridNavigator>();
            gridNavigator.Init(levelService);

            inputSystem = GetComponent<InputSystem>() ?? gameObject.AddComponent<InputSystem>();
            inputSystem.Init(levelService);
            inputSystem.OnCharacterClicked += OnCharacterClicked;
            inputSystem.OnEmptyTileClicked += OnEmptyTileClicked;
            inputSystem.OnOutOfBoundsClick += OnOutOfBoundsClick;

            StartTurn();
        }

        private void SetState(TurnState newTurnState)
        {
            switch (newTurnState)
            {
                case TurnState.USER_IDLE:
                    break;
                case TurnState.USER_CHAR_SELECTED:
                    break;
                case TurnState.CHARACTER_ANIMATION:
                    break;
            }
            turnState = newTurnState;
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
            attackingPlayerCharacters.Clear();
            attackingPlayerCharacters.AddRange(movablePlayerCharacters);

            SetState(TurnState.USER_IDLE);
        }

        private void OnEndTurnClicked()
        {
            //TODO: Start AI logic here
            switch (turnState)
            {
                case TurnState.USER_IDLE:
                    StartTurn();
                    break;
                case TurnState.USER_CHAR_SELECTED:
                    DeselectCharacter();
                    StartTurn();
                    break;
            }
        }

        private void OnCharacterClicked(EntityComponent character)
        {
            switch (turnState)
            {
                case TurnState.USER_IDLE:
                case TurnState.USER_CHAR_SELECTED:
                    if (character.Faction == EntityFaction.Player)
                    {
                        SelectUserCharacter(character);
                    }
                    break;
                case TurnState.CHARACTER_ANIMATION:
                    break;
            }
        }

        private void OnEmptyTileClicked(Vector2Int gridPosition)
        {
            switch (turnState)
            {
                case TurnState.USER_IDLE:
                    break;
                case TurnState.USER_CHAR_SELECTED:
                    bool characterCanMove = movablePlayerCharacters.Contains(selectedCharacter);
                    if (characterAvailableDestinationsCache.Contains(gridPosition) && characterCanMove)
                    {
                        MoveCharacter(selectedCharacter, gridPosition);
                    }
                    else
                    {
                        DeselectCharacter();
                    }
                    break;
                case TurnState.CHARACTER_ANIMATION:
                    break;
            }
        }

        private void OnOutOfBoundsClick()
        {
            switch (turnState)
            {
                case TurnState.USER_IDLE:
                    break;
                case TurnState.USER_CHAR_SELECTED:
                    DeselectCharacter();
                    break;
                case TurnState.CHARACTER_ANIMATION:
                    break;
            }
        }

        private void MoveCharacter(EntityComponent character, Vector2Int targetPosition)
        {
            List<Vector2Int> path = gridNavigator.GetPath(character.GridPosition, targetPosition);
            if (path != null)
            {
                SetState(TurnState.CHARACTER_ANIMATION);
                levelService.HideAllBreadCrumbs();
                movablePlayerCharacters.Remove(character);
                character.Move(path).Done(() => SetState(TurnState.USER_CHAR_SELECTED));
            }
        }

        private void SelectUserCharacter(EntityComponent selectedCharacter)
        {
            this.selectedCharacter = selectedCharacter;

            //Show HUD selection
            foreach (var entity in levelService.GetEntities())
            {
                entity.ShowSelection(entity == selectedCharacter);
            }
            SetState(TurnState.USER_CHAR_SELECTED);

            bool characterCanMove = movablePlayerCharacters.Contains(selectedCharacter);
            if (characterCanMove)
            {
                //Show walk breadcrumbs & cache possible destinations
                characterAvailableDestinationsCache.Clear();
                levelService.HideAllBreadCrumbs();
                gridNavigator.DoActionOnNeighbours(selectedCharacter.GridPosition, walkDistance, true,
                    (depth, gridPosition) =>
                    {
                        levelService.SetBreadCrumbVisible(gridPosition.x, gridPosition.y, true, .1f * depth);
                        characterAvailableDestinationsCache.Add(gridPosition);
                    });

            }
        }

        private void DeselectCharacter()
        {
            selectedCharacter = null;
            levelService.HideAllBreadCrumbs();
            foreach (var entity in levelService.GetEntities())
            {
                entity.ShowSelection(false);
            }

            SetState(TurnState.USER_IDLE);
        }

        private void Update()
		{
            Demo();
        }

        private void Demo()
        {
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