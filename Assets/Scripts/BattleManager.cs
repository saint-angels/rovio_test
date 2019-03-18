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

		private LevelService levelService;
		private UIComponent ui;
        private GridNavigator gridNavigator;
        private InputSystem inputSystem;

        private Entity selectedCharacter;

        private List<Entity> movablePlayerCharacters = new List<Entity>();
        private List<Entity> attackingPlayerCharacters = new List<Entity>();

        private TurnState turnState;

        //Currently selected character move/attack targets cache
        private List<Vector2Int> possibleMoveTargetsCache = new List<Vector2Int>();
        private List<Entity> possibleAttackTargetsCache = new List<Entity>();

        private void Start()
		{
			// Load the level
			levelService = new LevelService();
			levelService.LoadLevel("Level2");

			ui = GameObject.Find("Canvas").GetComponent<UIComponent>();
            ui.OnEndTurnClicked += OnEndTurnClicked;

            gridNavigator = GetComponent<GridNavigator>() ?? gameObject.AddComponent<GridNavigator>();
            gridNavigator.Init(levelService);

            inputSystem = GetComponent<InputSystem>() ?? gameObject.AddComponent<InputSystem>();
            inputSystem.Init(levelService);
            inputSystem.OnCharacterClicked += OnCharacterClicked;
            inputSystem.OnEmptyTileClicked += OnEmptyTileClicked;
            inputSystem.OnOutOfBoundsClick += OnOutOfBoundsClick;

            StartPlayerTurn();
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
                bool attackSuccess = EntityTryAttackFractionInRange(enemy, EntityFaction.Player);
                if (attackSuccess == false)
                {
                    Entity closestPlayerCharacter = levelService.GetClosestCharacter(enemy.GridPosition, EntityFaction.Player);
                    if (closestPlayerCharacter != null)
                    {
                        List<Vector2Int> path = gridNavigator.GetPath(enemy, closestPlayerCharacter.GridPosition, enemy.MaxWalkDistance, closestPlayerCharacter);
                        Vector2Int moveTarget = path.Last() == closestPlayerCharacter.GridPosition ? path[path.Count - 2] : path[path.Count - 1];
                        IPromise movePromise = MoveCharacter(enemy, moveTarget);
                        movePromise.Done(() => EntityTryAttackFractionInRange(enemy, EntityFaction.Player));
                        enemyTurnPromises.Add(movePromise);
                    }
                }
            }
            CheckForGameOver();

            Deferred.All(enemyTurnPromises).Done(() => StartPlayerTurn());
        }

        private bool EntityTryAttackFractionInRange(Entity attacker, EntityFaction enemyFaction)
        {
            List<Entity> entitiesInRange = levelService.GetEntitiesInRange(attacker, enemyFaction);
            if (entitiesInRange.Count > 0)
            {
                entitiesInRange[0].Damage(attacker.AttackDamage);
                return true;
            }
            return false;
        }

        private void OnEndTurnClicked()
        {
            DeselectCharacters();
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
                        SetState(TurnState.ANIMATION_IN_PROGRESS);
                        var movingCharacter = selectedCharacter;
                        DeselectCharacters();
                        MoveCharacter(movingCharacter, gridPosition)
                            .Done(() =>
                            {
                                movablePlayerCharacters.Remove(movingCharacter);
                                SelectUserCharacter(movingCharacter);
                            });
                    }
                    else
                    {
                        DeselectCharacters();
                    }
                    break;
            }
        }

        private void OnOutOfBoundsClick()
        {
            switch (turnState)
            {
                case TurnState.USER_CHAR_SELECTED:
                    DeselectCharacters();
                    break;
            }
        }

        private IPromise MoveCharacter(Entity character, Vector2Int targetPosition)
        {
            List<Vector2Int> path = gridNavigator.GetPath(character, targetPosition, character.MaxWalkDistance);
            if (path != null)
            {
                possibleMoveTargetsCache.Clear();
                return character.Move(path);
            }
            else
            {
                return Deferred.GetFromPool().Resolve();
            }
        }

        private void AttackCharacter(Entity attacker, Entity target)
        {
            foreach (var entity in levelService.GetEntities())
            {
                entity.SetTargeted(false);
            }
            attackingPlayerCharacters.Remove(attacker);
            possibleAttackTargetsCache.Clear();

            target.Damage(attacker.AttackDamage);

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

            foreach (var entity in levelService.GetEntities())
            {
                entity.SetTargeted(false);
                entity.SetSelected(entity == selectedCharacter);
            }

            possibleMoveTargetsCache.Clear();
            bool characterCanMove = movablePlayerCharacters.Contains(selectedCharacter);
            if (characterCanMove)
            {
                //Show walk breadcrumbs & cache possible destinations
                gridNavigator.DoActionOnNeighbours(selectedCharacter.GridPosition, selectedCharacter.MaxWalkDistance, true,
                    (depth, gridPosition) =>
                    {
                        levelService.SetBreadCrumbVisible(gridPosition.x, gridPosition.y, true, .1f * depth);
                        possibleMoveTargetsCache.Add(gridPosition);
                    });

            }

            bool characterCanAttack = attackingPlayerCharacters.Contains(selectedCharacter);
            if (characterCanAttack)
            {
                List<Entity> entitiesInRange = levelService.GetEntitiesInRange(selectedCharacter, EntityFaction.Enemy);
                foreach (Entity entity in entitiesInRange)
                {
                    if (entity.Faction == EntityFaction.Enemy)
                    {
                        entity.SetTargeted(true);
                        possibleAttackTargetsCache.Add(entity);
                    }
                }
            }
            SetState(TurnState.USER_CHAR_SELECTED);
        }

        private void DeselectCharacters()
        {
            selectedCharacter = null;
            foreach (var entity in levelService.GetEntities())
            {
                entity.SetSelected(false);
                entity.SetTargeted(false);
            }

            SetState(TurnState.USER_IDLE);
        }
	}
}