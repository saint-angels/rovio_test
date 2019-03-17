using System;
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
		private GameObject entityPrefab;

		private Transform levelContainer;
		private Transform tilesContainer;
		private Transform entitiesContainer;

		private float quakeAnimationCooldown;
		private AudioComponent audio;

		public LevelService()
		{
			tileSprites = Resources.LoadAll<Sprite>("Sprites/Tileset");
			tilePrefab = Resources.Load<GameObject>("Prefabs/Tile");

			entitySprites = Resources.LoadAll<Sprite>("Sprites/Entities");
			entityPrefab = Resources.Load<GameObject>("Prefabs/Entity");

			levelContainer = GameObject.Find("Level").transform;
			tilesContainer = levelContainer.transform.Find("Tiles");
			entitiesContainer = levelContainer.transform.Find("Entities");

			audio = GameObject.Find("Audio").GetComponent<AudioComponent>();
		}

        public List<EntityComponent> GetCharacters(EntityFaction faction)
        {
            return LevelData.Entities.Where(p => p.Type == EntityType.Character && p.Faction == faction).ToList();
        }

        public List<EntityComponent> GetEntities()
        {
            return LevelData.Entities;
        }

        public bool IsPointOnLevelGrid(int x, int y)
        {
            bool outOfGridBounds = x >= GridSize.x || x < 0 || y >= GridSize.y || y < 0;
            return outOfGridBounds == false;
        }

        //Note: there could be only 1 entity at each tile at a time.
        public EntityComponent GetEntityAtPosition(int x, int y)
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
				Entities = new List<EntityComponent>(),
                TilesEntities = new EntityComponent[width, height]
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
			var entity = GameObject.Instantiate(entityPrefab, Vector3.zero, Quaternion.identity, entitiesContainer).GetComponent<EntityComponent>();
            entity.name = type.ToString();
            entity.Initialize(x, y, sprite, type, faction);
			LevelData.Entities.Add(entity);
            LevelData.TilesEntities[x, y] = entity;
            entity.OnMove += OnEntityMove;
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

        private void OnEntityMove(EntityComponent entity, Vector2Int oldPosition, Vector2Int newPosition)
        {
            LevelData.TilesEntities[oldPosition.x, oldPosition.y] = null;
            LevelData.TilesEntities[newPosition.x, newPosition.y] = entity;
        }
    }
}