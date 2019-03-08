﻿using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
	public class UiComponent : MonoBehaviour
	{
        [SerializeField] private RawImage Overlay;
        [SerializeField] private GameObject Banner;
        [SerializeField] private Text BannerText;
		[SerializeField] private Text BannerTextShadow;

		private void Awake()
		{
			Overlay.color = new Color(0, 0, 0, 0);
			Banner.gameObject.SetActive(false);
		}

		public void ShowAndHideBanner(string text, float showDelay = 0, float hideDelay = 2)
		{
			ShowBanner(text, showDelay);
			HideBanner(showDelay + hideDelay);
		}

		public void ShowBanner(string text, float delay = 0)
		{
			Overlay.DOColor(new Color(0, 0, 0, 0.5f), 0.25f)
				.SetDelay(delay)
				.OnComplete(() =>
				{
					Banner.gameObject.transform.DOPunchPosition(new Vector3(2f, 2f, 2f), 1f);
					BannerText.text = text;
					BannerTextShadow.text = text;
					Banner.gameObject.SetActive(true);
				});
		}

		public void HideBanner(float delay = 0)
		{
			Overlay.DOColor(new Color(0, 0, 0, 0), 0.25f)
				.SetDelay(delay)
				.OnStart(() =>
				{
					Banner.gameObject.SetActive(false);
				});
		}
	}
}