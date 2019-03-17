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
        public event Action<Vector2Int, int> OnStep = (newPosition, stepIndex) => { };
        public event Action<Entity, Vector2Int, Vector2Int> OnMovementFinished = (entity, oldPosition, newPosition) => { };
        public event Action<float> OnDamaged = (currentHealthPercentage) => { };
        public event Action<bool> OnSelected = (isSelected) => { };
        public event Action<bool> OnTargeted = (isTargeted) => { };
        public event Action<Entity> OnDestroyed = (entity) => { };

        public int AttackDamage { get; private set; }
        public int HealthPoints { get; private set; }
        public EntityType Type { get; private set; }
        public EntityFaction Faction { get; private set; }
        public Vector2Int GridPosition { get; private set; }

        private EntityView view;
        private int maxHealth;

        public void Init(int x, int y, Sprite sprite, EntityType type, EntityFaction faction)
        {
            view = GetComponent<EntityView>() ?? gameObject.AddComponent<EntityView>();
            view.Init(this, sprite, type, x, y);

            GridPosition = new Vector2Int(x, y);
            Type = type;
            Faction = faction;

            transform.position = LevelGrid.ToWorldCoordinates(x, y);
        }

        public void AddCharacterParams(int health, int attackDamge)
        {
            this.AttackDamage = attackDamge;
            this.maxHealth = health;
            this.HealthPoints = health;
        }

        public void SetTargeted(bool isTargeted)
        {
            OnTargeted(isTargeted);
        }

        public void SetSelected(bool isSelected)
        {
            OnSelected(isSelected);
        }

        public IPromise Move(List<Vector2Int> pathDirections)
        {
            Deferred moveDeferred = Deferred.GetFromPool();
            Vector2Int oldPosition = GridPosition;

            for (int stepIdx = 0; stepIdx < pathDirections.Count; stepIdx++)
            {
                Vector2Int newPosition = pathDirections[stepIdx];
                OnStep(newPosition, stepIdx);
            }

            GridPosition = pathDirections[pathDirections.Count - 1];
            OnMovementFinished(this, oldPosition, GridPosition);
            moveDeferred.Resolve();

            return moveDeferred;
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
    }
}