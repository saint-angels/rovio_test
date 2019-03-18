using Assets.Scripts.Presentation.Entities;
using Assets.Scripts.Presentation.Levels;
using Assets.Scripts.Promises;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    [SelectionBase]
    public class Entity : MonoBehaviour
    {
        public event Action<Vector2Int, int, float> OnStep = (newPosition, stepIndex, stepDuration) => { };
        public event Action<Entity, Vector2Int, Vector2Int> OnMovementFinished = (entity, oldPosition, newPosition) => { };
        public event Action<float> OnDamaged = (currentHealthPercentage) => { };
        public event Action<Entity, bool> OnSelected = (entity, isSelected) => { };
        public event Action<bool> OnTargeted = (isTargeted) => { };
        public event Action<Entity> OnDestroyed = (entity) => { };

        public EntityType Type { get; private set; }
        public EntityFaction Faction { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public EntityView EntityView { get; private set; }

        //TODO: Move to additional components
        public int MaxWalkDistance { get; private set; }
        public int AttackDamage { get; private set; }
        public int HealthPoints { get; private set; }
        public int AttackRange { get; private set; }

        //TODO: move to properties maybe
        public List<Vector2Int> possibleMoveTargets = new List<Vector2Int>();
        public List<Entity> possibleAttackTargets = new List<Entity>();

        private int maxHealth;
        private float stepDuration;
        private LevelService levelService;

        public void Init(int x, int y, Sprite sprite, EntityType type, EntityFaction faction, LevelService levelService)
        {
            this.levelService = levelService;
            GridPosition = new Vector2Int(x, y);
            Type = type;
            Faction = faction;
            EntityView = GetComponent<EntityView>() ?? gameObject.AddComponent<EntityView>();
            EntityView.Init(this, sprite, type, x, y, levelService);
        }

        public void AddCharacterParams(int health, int attackDamge, int walkDistance, int attackRange, float stepDuration)
        {
            this.AttackRange = attackRange;
            this.AttackDamage = attackDamge;
            this.maxHealth = health;
            this.HealthPoints = health;
            this.MaxWalkDistance = walkDistance;
            this.stepDuration = stepDuration;
        }

        public void SetTargeted(bool isTargeted)
        {
            OnTargeted(isTargeted);
        }

        public void SetSelected(bool isSelected)
        {
            OnSelected(this, isSelected);
        }

        public IPromise Move(GridNavigator gridNavigator, Vector2Int target)
        {
            List<Vector2Int> path = gridNavigator.GetPath(this, target, MaxWalkDistance);
            if (path != null)
            {
                Deferred moveDeferred = Deferred.GetFromPool();
                Vector2Int oldPosition = GridPosition;

                for (int stepIdx = 0; stepIdx < path.Count; stepIdx++)
                {
                    Vector2Int newPosition = path[stepIdx];
                    OnStep(newPosition, stepIdx, stepDuration);
                }

                Timers.Instance.Wait(path.Count * stepDuration)
                    .Done(() =>
                    {
                        GridPosition = path[path.Count - 1];
                        OnMovementFinished(this, oldPosition, GridPosition);
                        moveDeferred.Resolve();
                    });
                return moveDeferred;
            }
            else
            {
                return Deferred.GetFromPool().Resolve();
            }
        }

        public void Select(List<Vector2Int> possibleMoveTargets, bool canAttack)
        {
            this.possibleMoveTargets = possibleMoveTargets;
            this.possibleAttackTargets.Clear();

            if (canAttack)
            {
                EntityFaction opposingFaction = Faction == EntityFaction.Player ? EntityFaction.Enemy : EntityFaction.Player;
                List <Entity> entitiesInRange = levelService.GetEntitiesInRange(this, opposingFaction);
                foreach (Entity entity in entitiesInRange)
                {
                    entity.SetTargeted(true);
                    possibleAttackTargets.Add(entity);
                }
            }
            OnSelected(this, true);
        }

        public bool CanAttack(Entity entity)
        {
            return possibleAttackTargets.Contains(entity);
        }

        public void Damage(int damage)
        {
            HealthPoints -= damage;
            if (HealthPoints <= 0)
            {
                OnDestroyed(this);
            }

            float currentHealthPercentage = (float)HealthPoints / (float)maxHealth;
            OnDamaged(currentHealthPercentage);
        }

        public void Attack(Entity target)
        {
            target.Damage(AttackDamage);
        }

        public bool CanMove(Vector2Int position)
        {
            return possibleMoveTargets.Contains(position);
        }
    }
}