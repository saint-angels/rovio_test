using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Presentation.Entities;
using Assets.Scripts.Presentation.Levels;
using Assets.Scripts.Promises;
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

        public Action OnPlayerTurnEnded = () => { };

		private LevelService levelService;
		private UIComponent ui;
        private InputSystem inputSystem;

        private Entity selectedCharacter;

        private TurnState turnState;
        private List<Entity> movablePlayerCharacters = new List<Entity>();
        private List<Entity> attackingPlayerCharacters = new List<Entity>();

        private void Start()
		{
			// Load the level
			levelService = new LevelService();
            GridNavigator gridNavigator = GetComponent<GridNavigator>() ?? gameObject.AddComponent<GridNavigator>();
			levelService.Init("Level2", this, gridNavigator);

			ui = GameObject.Find("Canvas").GetComponent<UIComponent>();
            ui.OnEndTurnClicked += OnEndTurnClicked;

            inputSystem = GetComponent<InputSystem>() ?? gameObject.AddComponent<InputSystem>();
            inputSystem.Init(levelService);
            inputSystem.OnCharacterClicked += OnCharacterClicked;
            inputSystem.OnEmptyTileClicked += OnEmptyTileClicked;

            StartPlayerTurn();
        }

        private void SetState(TurnState newTurnState)
        {
            turnState = newTurnState;
        }

        private void StartPlayerTurn()
        {
            movablePlayerCharacters.Clear();
            attackingPlayerCharacters.Clear();
            movablePlayerCharacters.AddRange(levelService.GetCharacters(EntityFaction.Player));
            attackingPlayerCharacters.AddRange(movablePlayerCharacters);

            SetState(TurnState.USER_IDLE);
        }

        private void StartEnemyTurn()
        {
            SetState(TurnState.ANIMATION_IN_PROGRESS);

            List<IPromise> enemyTurnPromises = new List<IPromise>();
            var enemies = levelService.GetCharacters(EntityFaction.Enemy);
            foreach (var enemy in enemies)
            {
                enemyTurnPromises.Add(enemy.MakeAITurn());
            }
            CheckForGameOver();

            Deferred.All(enemyTurnPromises).Done(() => StartPlayerTurn());
        }

        private void OnEndTurnClicked()
        {
            OnPlayerTurnEnded();
            StartEnemyTurn();
        }

        private void OnCharacterClicked(Entity clickedCharacter)
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
                            if (selectedCharacter.CanAttack(clickedCharacter))
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
                    if (selectedCharacter.CanMove(gridPosition))
                    {
                        SetState(TurnState.ANIMATION_IN_PROGRESS);
                        var movingCharacter = selectedCharacter;

                        movingCharacter.Move(gridPosition)
                            .Done(() =>
                            {
                                movablePlayerCharacters.Remove(movingCharacter);
                                bool canAttack = attackingPlayerCharacters.Contains(movingCharacter);
                                if (canAttack)
                                {
                                    SelectUserCharacter(movingCharacter);
                                }
                            });
                    }
                    break;
            }
        }

        private void AttackCharacter(Entity attacker, Entity target)
        {
            foreach (var entity in levelService.GetEntities())
            {
                entity.SetTargeted(false);
            }
            attackingPlayerCharacters.Remove(attacker);

            attacker.Attack(target);

            CheckForGameOver();
        }

        private void CheckForGameOver()
        {
            var enemyCharacters = levelService.GetCharacters(EntityFaction.Enemy);
            var playerCharacters = levelService.GetCharacters(EntityFaction.Player);

            if (enemyCharacters.Count == 0)
            {
                ui.ShowAndHideBanner("Player wins!");
            }
            else if (playerCharacters.Count == 0)
            {
                ui.ShowAndHideBanner("AI wins!");
            }
        }

        private void SelectUserCharacter(Entity selectedCharacter)
        {
            this.selectedCharacter = selectedCharacter;

            List<Vector2Int> possibleMoveTargets = new List<Vector2Int>();
            bool characterCanMove = movablePlayerCharacters.Contains(selectedCharacter);
            bool characterCanAttack = attackingPlayerCharacters.Contains(selectedCharacter);
            selectedCharacter.Select(characterCanMove, characterCanAttack);
            SetState(TurnState.USER_CHAR_SELECTED);
        }
	}
}