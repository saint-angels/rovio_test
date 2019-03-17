﻿using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Presentation.Entities;
using DG.Tweening;
using UnityEngine;

namespace Assets.Scripts.Presentation.Levels
{
    public class LevelService
    {
        public LevelData LevelData;

        public Vector2Int GridSize { get; private set; }

		private Sprite[] tileSprites;
		private GameObject tilePrefab;

		private Sprite[] entitySprites;
		private Entity entityPrefab;

		private Transform levelContainer;
		private Transform tilesContainer;
		private Transform entitiesContainer;

		private float quakeAnimationCooldown;
		private AudioComponent audio;

        private int defaultCharacterHealth = 2;
        private int defaultCharacterAttackDamage = 1;
        private int defaultCharacterWalkDistance = 6;


		public LevelService()
		{
			tileSprites = Resources.LoadAll<Sprite>("Sprites/Tileset");
			tilePrefab = Resources.Load<GameObject>("Prefabs/Tile");

			entitySprites = Resources.LoadAll<Sprite>("Sprites/Entities");
			entityPrefab = Resources.Load<Entity>("Prefabs/Entity");

			levelContainer = GameObject.Find("Level").transform;
			tilesContainer = levelContainer.transform.Find("Tiles");
			entitiesContainer = levelContainer.transform.Find("Entities");

			audio = GameObject.Find("Audio").GetComponent<AudioComponent>();
		}

        public List<Entity> GetCharacters(EntityFaction faction)
        {
            return LevelData.Entities.Where(p => p.Type == EntityType.Character && p.Faction == faction).ToList();
        }

        public List<Entity> GetEntities()
        {
            return LevelData.Entities;
        }

        public bool IsPointOnLevelGrid(int x, int y)
        {
            bool outOfGridBounds = x >= GridSize.x || x < 0 || y >= GridSize.y || y < 0;
            return outOfGridBounds == false;
        }

        //Note: there could be only 1 entity at each tile at a time.
        public Entity GetEntityAtPosition(int x, int y)
        {
            if (IsPointOnLevelGrid(x, y))
            {
                return LevelData.TilesEntities[x, y];
            }
            else
            {
                return null;
            }
        }

        public List<Entity> GetEntitiesInRangeCross(Entity aroundEntity, int range)
        {
            Vector2Int position = aroundEntity.GridPosition;

            List<Entity> entitiesList = new List<Entity>();

            //Check x axis
            for (int xOffset = -range; xOffset <= range; xOffset++)
            {
                Vector2Int offsetPosition = new Vector2Int(position.x + xOffset, position.y);
                Entity entity = GetEntityAtPosition(offsetPosition.x, offsetPosition.y);
                if (entity != null && entity != aroundEntity)
                {
                    entitiesList.Add(entity);
                }
            }

            //Check y axis
            for (int yOffset = -range; yOffset <= range; yOffset++)
            {
                Vector2Int offsetPosition = new Vector2Int(position.x, position.y + yOffset);
                Entity entity = GetEntityAtPosition(offsetPosition.x, offsetPosition.y);
                if (entity != null && entity != aroundEntity)
                {
                    entitiesList.Add(entity);
                }
            }

            return entitiesList;
        }

		public void LoadLevel(string levelName)
		{
			var levelText = Resources.Load<TextAsset>($"Levels/{levelName}").text;
			var rows = levelText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			var width = int.Parse(rows[0]);
			var height = int.Parse(rows[1]);
            GridSize = new Vector2Int(width, height);

            LevelData = new LevelData
			{
				Width = width,
				Height = height,
				Tiles = new LevelTileComponent[width, height],
				Entities = new List<Entity>(),
                TilesEntities = new Entity[width, height]
			};

			// Ground
			for (int y = 0; y < height; y++)
			{
				var row = rows[3 + y];

				for (int x = 0; x < width; x++)
				{
					var tile = int.Parse(row[x].ToString()) - 1;
					InstantiateTile(x, y, tile);
				}
			}

			// Entities
			Sprite entitySprite;
			for (int y = 0; y < height; y++)
			{
				var row = rows[4 + height + y];

				for (int x = 0; x < width; x++)
				{
					switch (row[x])
					{
						case 'e':
							entitySprite = entitySprites[UnityEngine.Random.Range(0, 5)];
							InstantiateEntity(x, y, entitySprite, EntityType.Character, EntityFaction.Enemy);
							break;

						case 'p':
							entitySprite = entitySprites[UnityEngine.Random.Range(5, 10)];
							InstantiateEntity(x, y, entitySprite, EntityType.Character, EntityFaction.Player);
							break;

						case '#':
							entitySprite = tileSprites[49];
							InstantiateEntity(x, y, entitySprite, EntityType.Obstacle, EntityFaction.Neutral);
							break;
					}
				}
			}

			CenterCamera(height);
		}

		private void CenterCamera(int levelHeight)
		{
			var numberOfRowsBeforeAdjustmentIsNeeded = 9;
			var difference = Mathf.Max(0, levelHeight - numberOfRowsBeforeAdjustmentIsNeeded);

			var adjustment = -(0.33f * difference);
			adjustment = Mathf.RoundToInt(adjustment);
			Camera.main.transform.position = new Vector3(adjustment, adjustment, Camera.main.transform.position.z);
		}

		private void InstantiateTile(int x, int y, int index)
		{
			var tile = GameObject.Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, tilesContainer);
			var renderer = tile.GetComponent<SpriteRenderer>();
			renderer.sortingOrder = LevelGrid.GetSortingOrder(x, y);
			renderer.sprite = tileSprites[index];
			renderer.color = (x ^ y) % 2 == 0 ? Color.white : new Color(0.95f, 0.95f, 0.95f);
			tile.name = $"Tile{x}_{y}";

			tile.transform.localPosition = LevelGrid.ToWorldCoordinates(x, y);

			LevelData.Tiles[x, y] = tile.GetComponent<LevelTileComponent>();
		}

		private void InstantiateEntity(int x, int y, Sprite sprite, EntityType type, EntityFaction faction)
		{
            Entity entity = GameObject.Instantiate(entityPrefab, Vector3.zero, Quaternion.identity, entitiesContainer);
            entity.name = type.ToString();
            entity.Init(x, y, sprite, type, faction);
            if (type == EntityType.Character)
            {
                entity.AddCharacterParams(defaultCharacterHealth, defaultCharacterAttackDamage, defaultCharacterWalkDistance);
                entity.OnMovementFinished += OnEntityMoved;
                entity.OnDestroyed += OnEntityDestroyed;
            }
			LevelData.Entities.Add(entity);
            LevelData.TilesEntities[x, y] = entity;
		}

		public void SetBreadCrumbVisible(int x, int y, bool isVisible, float delay = 0)
		{
			LevelData.Tiles[x, y].SetBreadCrumbVisible(isVisible, delay);
		}

		public void HideAllBreadCrumbs()
		{
			for (int y = 0; y < LevelData.Height; y++)
			{
				for (int x = 0; x < LevelData.Width; x++)
				{
					LevelData.Tiles[x, y].SetBreadCrumbVisible(false);
				}
			}
		}

		public void PlayQuakeAnimation(int x, int y, int radius)
		{
			if (Time.realtimeSinceStartup < quakeAnimationCooldown)
			{
				return;
			}

			var calculatedTotalDurationOfAnimation = ((radius * 0.25f) * 0.5f) + 0.75f;
			quakeAnimationCooldown = Time.realtimeSinceStartup + calculatedTotalDurationOfAnimation;

			var center = new Vector2(x, y);
			var current = Vector2.zero;

			for (int y2 = y - radius; y2 <= y + radius; y2++)
			{
				for (int x2 = x - radius; x2 <= x + radius; x2++)
				{
					if (x2 < 0 || x2 >= LevelData.Width ||
						y2 < 0 || y2 >= LevelData.Height)
					{
						continue;
					}

					current.x = x2;
					current.y = y2;

					var distance = Vector2.Distance(current, center);

					if (distance <= radius)
					{
						var tile = LevelData.Tiles[x2, y2].transform;
						var originalY = tile.position.y;

						var delay = (distance * 0.25f) * 0.5f;

						var sequence = DOTween.Sequence();
						sequence.PrependInterval(delay);
						sequence.Append(tile.DOLocalMoveY(originalY + 0.1f, 0.25f).SetEase(Ease.OutBack));
						sequence.Append(tile.DOLocalMoveY(originalY - 0.1f, 0.25f).SetEase(Ease.OutBack));
						sequence.Append(tile.DOLocalMoveY(originalY, 0.25f));

						for (int i = 0; i < LevelData.Entities.Count; i++)
						{
							var entity = LevelData.Entities[i];
							if (entity.GridPosition == current)
							{
								var sequence2 = DOTween.Sequence();
								sequence2.PrependInterval(delay);
								var entityTransform = entity.gameObject.transform;
								sequence2.Append(entityTransform.DOLocalMoveY(originalY + 0.1f, 0.25f).SetEase(Ease.OutBack));
								sequence2.Append(entityTransform.DOLocalMoveY(originalY - 0.1f, 0.25f).SetEase(Ease.OutBack));
								sequence2.Append(entityTransform.DOLocalMoveY(originalY, 0.25f));
							}
						}
					}
				}
			}

			audio.PlayQuake();
		}

        private void OnEntityDestroyed(Entity entity)
        {
            LevelData.Entities.Remove(entity);
            LevelData.TilesEntities[entity.GridPosition.x, entity.GridPosition.y] = null;
        }

        private void OnEntityMoved(Entity entity, Vector2Int oldPosition, Vector2Int newPosition)
        {
            LevelData.TilesEntities[oldPosition.x, oldPosition.y] = null;
            LevelData.TilesEntities[newPosition.x, newPosition.y] = entity;
        }
    }
}