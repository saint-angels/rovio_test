using System;
using System.Collections;
using Assets.Scripts.Presentation.Levels;
using DG.Tweening;
using UnityEngine;

namespace Assets.Scripts.Presentation.Entities
{
    [SelectionBase]
	public class EntityComponent : MonoBehaviour
	{
        public event Action<EntityComponent> OnDestroyed = (e) => { };

		public SpriteRenderer Renderer;
		public GameObject SlashPrefab;
		public GameObject Selection;
		public GameObject AttackTargetSelection;
		public GameObject HealthBarContainer;
		public GameObject HealthBar;

		public EntityType Type { get; private set; }
        public EntityFaction Faction { get; private set; }
        public Vector2Int GridPosition { get; private set; }

		private AudioComponent audio;

		private void Awake()
		{
			audio = GameObject.Find("Audio").GetComponent<AudioComponent>();
		}

		public void Initialize(int x, int y, Sprite sprite, EntityType type, EntityFaction faction)
		{
			GridPosition = new Vector2Int(x, y);
			Renderer.sprite = sprite;
			Type = type;
            Faction = faction;

			Renderer.sortingOrder = LevelGrid.GetSortingOrder(x, y);
			transform.position = LevelGrid.ToWorldCoordinates(x, y);

			if (type == EntityType.Character)
			{
				HealthBarContainer.SetActive(true);
				HealthBar.SetActive(true);
				HealthBar.transform.localScale = Vector3.one;
			}
		}

		public void Move(Direction direction, float delay = 0)
		{
			StartCoroutine(MoveRoutine(direction, delay));
		}

		private IEnumerator MoveRoutine(Direction direction, float delay = 0)
		{
			yield return new WaitForSeconds(delay);

			switch (direction)
			{
				case Direction.Up:
					GridPosition = new Vector2Int(GridPosition.x, GridPosition.y - 1);
					break;

				case Direction.Down:
					GridPosition = new Vector2Int(GridPosition.x, GridPosition.y + 1);
					break;

				case Direction.Left:
					GridPosition = new Vector2Int(GridPosition.x - 1, GridPosition.y);
					break;

				case Direction.Right:
					GridPosition = new Vector2Int(GridPosition.x + 1, GridPosition.y);
					break;
			}

			var targetPosition = LevelGrid.ToWorldCoordinates(GridPosition.x, GridPosition.y);
			transform.DOJump(targetPosition, 0.25f, 1, 0.3f).SetEase(Ease.InQuint);
			Renderer.sortingOrder = LevelGrid.GetSortingOrder(GridPosition.x, GridPosition.y);
			audio.PlayMove();
		}

		public void PlayTakeDamageAnimation(float delay = 0)
		{
			transform.DOPunchRotation(new Vector3(0, 0, 20), 0.5f)
				.SetDelay(delay)
				.OnStart(() =>
				{
					InstantiateSlash(GridPosition.x, GridPosition.y);
					audio.PlayTakeDamage();
				})
				.OnComplete(() =>
				{
					transform.rotation = Quaternion.identity;
				});
		}

		public void PlayHealthBarAnimation(float healthPercentage)
		{
			if (healthPercentage < 0)
			{
				healthPercentage = 0;
			}
			else if (healthPercentage > 1)
			{
				healthPercentage = 1;
			}

			HealthBarContainer.transform.DOShakePosition(0.5f, new Vector3(0.1f, 0.1f, 0));
			HealthBar.transform.DOScaleX(healthPercentage, 0.25f);
		}

		public void ShowSelection(bool state)
		{
			Selection.gameObject.SetActive(state);
			Selection.gameObject.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.5f);

			if (state)
			{
				audio.PlaySelect();
			}
		}

		public void ShowSelectionAttackTarget(bool state)
		{
			AttackTargetSelection.gameObject.SetActive(state);

			if (state)
			{
				AttackTargetSelection.gameObject.transform.localScale = Vector3.one;
				AttackTargetSelection.gameObject.transform.DOScale(new Vector3(1.2f, 1.2f, 1f), 0.5f).SetEase(Ease.OutQuint).SetLoops(int.MaxValue, LoopType.Yoyo);
				audio.PlaySelectTarget();
			}
			else
			{
				DOTween.Kill(AttackTargetSelection.gameObject.transform);
			}
		}

		public void PlayDeathAnimation()
		{
			transform.DOMoveX(UnityEngine.Random.Range(-3f, 3f), 4f).SetEase(Ease.OutQuart);
			transform.GetChild(0).DOLocalRotate(new Vector3(0, 0, 180f), 2f);

			transform.DOMoveY(transform.position.y + 1f, 0.5f)
				.SetEase(Ease.OutQuint)
				.OnComplete(() =>
				{
					transform.DOMoveY(-10, 5f).SetEase(Ease.OutQuint).OnComplete(() =>
					{
						gameObject.SetActive(false);
                        OnDestroyed(this);
                    });
				});

			audio.PlayDeath();
		}

		public void InstantiateSlash(int x, int y)
		{
			var slash = GameObject.Instantiate(SlashPrefab, Vector3.zero, Quaternion.identity);
			slash.name = SlashPrefab.name;
			slash.transform.position = LevelGrid.ToWorldCoordinates(x, y);
			slash.transform.GetChild(0).rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360));
			GameObject.Destroy(slash, 1f);
		}
	}
}