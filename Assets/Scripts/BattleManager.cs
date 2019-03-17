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
            ANIMATION_IN_PROGRESS,
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

        //Currently selected character move/attack targets cache
        private List<Vector2Int> possibleMoveTargetsCache = new List<Vector2Int>();
        private List<EntityComponent> possibleAttackTargetsCache = new List<EntityComponent>();

        private void Start()
		{
			// Load the level
			levelService = new LevelService();
			levelService.LoadLevel("Level2");

            // Grab all enemies
            //TODO: Remove enemies from list after death?
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
                case TurnState.ANIMATION_IN_PROGRESS:
                    break;
            }
            turnState = newTurnState;
        }

        private void StartTurn()
        {
            movablePlayerCharacters.Clear();
            attackingPlayerCharacters.Clear();
            movablePlayerCharacters.AddRange(levelService.GetCharacters(EntityFaction.Player));
            attackingPlayerCharacters.AddRange(movablePlayerCharacters);

            SetState(TurnState.USER_IDLE);
        }

        private void OnEndTurnClicked()
        {
            //TODO: Start AI logic here
            //TODO: Check if battle finished?
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

        private void OnCharacterClicked(EntityComponent clickedCharacter)
        {
            switch (turnState)
            {
                case TurnState.USER_IDLE:
                    if (clickedCharacter.Faction == EntityFaction.Player)
                    {
                        SelectUserCharacter(clickedCharacter);
                    }
                    break;
                case TurnState.USER_CHAR_SELECTED:
                    switch (clickedCharacter.Faction)
                    {
                        case EntityFaction.Player:
                            SelectUserCharacter(clickedCharacter);
                            break;
                        case EntityFaction.Enemy:
                            if (possibleAttackTargetsCache.Contains(clickedCharacter))
                            {
                                AttackCharacter(selectedCharacter, clickedCharacter);
                            }
                            break;
                    }
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
                    if (possibleMoveTargetsCache.Contains(gridPosition))
                    {
                        MoveCharacter(selectedCharacter, gridPosition);
                    }
                    else
                    {
                        DeselectCharacter();
                    }
                    break;
                case TurnState.ANIMATION_IN_PROGRESS:
                    break;
            }
        }

        private void OnOutOfBoundsClick()
        {
            switch (turnState)
            {
                case TurnState.USER_CHAR_SELECTED:
                    DeselectCharacter();
                    break;
            }
        }

        private void MoveCharacter(EntityComponent character, Vector2Int targetPosition)
        {
            List<Vector2Int> path = gridNavigator.GetPath(character.GridPosition, targetPosition);
            if (path != null)
            {
                levelService.HideAllBreadCrumbs();
                movablePlayerCharacters.Remove(character);
                possibleMoveTargetsCache.Clear();
                SetState(TurnState.ANIMATION_IN_PROGRESS);
                character.Move(path).Done(() => SetState(TurnState.USER_CHAR_SELECTED));
            }
        }

        private void AttackCharacter(EntityComponent attacker, EntityComponent target)
        {
            foreach (var entity in levelService.GetEntities())
            {
                entity.ShowSelectionAttackTarget(false);
            }
            attackingPlayerCharacters.Remove(attacker);
            possibleAttackTargetsCache.Clear();
            target.PlayTakeDamageAnimation();
            target.PlayHealthBarAnimation(0);
        }

        private void SelectUserCharacter(EntityComponent selectedCharacter)
        {
            this.selectedCharacter = selectedCharacter;

            //Update HUD selection
            foreach (var entity in levelService.GetEntities())
            {
                entity.ShowSelection(entity == selectedCharacter);
                entity.ShowSelectionAttackTarget(false);
            }
            levelService.HideAllBreadCrumbs();

            possibleMoveTargetsCache.Clear();
            bool characterCanMove = movablePlayerCharacters.Contains(selectedCharacter);
            if (characterCanMove)
            {
                //Show walk breadcrumbs & cache possible destinations
                gridNavigator.DoActionOnNeighbours(selectedCharacter.GridPosition, walkDistance, true,
                    (depth, gridPosition) =>
                    {
                        levelService.SetBreadCrumbVisible(gridPosition.x, gridPosition.y, true, .1f * depth);
                        possibleMoveTargetsCache.Add(gridPosition);
                    });

            }

            bool characterCanAttack = attackingPlayerCharacters.Contains(selectedCharacter);
            if (characterCanAttack)
            {
                List<EntityComponent> entitiesInRange = levelService.GetEntitiesInRangeCross(selectedCharacter, 1);
                foreach (EntityComponent entity in entitiesInRange)
                {
                    if (entity.Faction == EntityFaction.Enemy)
                    {
                        entity.ShowSelectionAttackTarget(true);
                        possibleAttackTargetsCache.Add(entity);
                    }
                }
            }
            SetState(TurnState.USER_CHAR_SELECTED);
        }

        private void DeselectCharacter()
        {
            selectedCharacter = null;
            levelService.HideAllBreadCrumbs();
            foreach (var entity in levelService.GetEntities())
            {
                entity.ShowSelection(false);
                entity.ShowSelectionAttackTarget(false);
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