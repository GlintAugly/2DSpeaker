using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CharParts))]
public class Blink : MonoBehaviour
{
	class BlinkAnimationData
	{
		public Sprite[] animationSprites;
		public float changeSpan;
	}
	private Dictionary<string, BlinkAnimationData> m_blinkSprites = new();
	private SpriteRenderer m_eyeRenderer;
	private CharParts m_charParts;
    private float m_nextTimer;
	private Coroutine m_blinkCoroutine = null;
    private const float TWICE_SPAN = 0.3f;
	private const float TWICE_RATIO = 0.1f;
    private const float MIN_BLINK_SPAN = 1f;
    private const float MAX_BLINK_SPAN = 3f;

	public void Initialize(BlinkInitializeDataItem[] blinkInitializeData)
	{
		foreach (var initializeData in blinkInitializeData)
		{
			if (string.IsNullOrEmpty(initializeData.startSprite) == false)
			{
				Sprite[] animSprites = new Sprite[initializeData.animationSprites.Length];
				for (var i = 0; i < initializeData.animationSprites.Length; i++)
				{
					Sprite animSprite = ModifiableAssetsUtils.LoadSprite(Definition.CHARACTER_FOLDER, initializeData.animationSprites[i]);
					if (animSprite == null)
					{
						AppDebug.LogError("Blinkアニメーション用Spriteの読み込み失敗: {0}", initializeData.animationSprites[i]);
						continue;
					}
					animSprites[i] = animSprite;
				}
				m_blinkSprites[initializeData.startSprite] = new BlinkAnimationData
				{
					animationSprites = animSprites,
					changeSpan = initializeData.changeSpan
				};
			}
		}
	}

	void Start ()
	{
        m_nextTimer = Random.Range(MIN_BLINK_SPAN, MAX_BLINK_SPAN);
		m_eyeRenderer = gameObject.GetComponent<SpriteRenderer>();
		m_charParts = gameObject.GetComponent<CharParts>();
	}

	void Update ()
	{
		if (m_blinkCoroutine == null)
		{
			m_nextTimer -= Time.deltaTime;
			if (m_nextTimer < 0 )
			{
				m_blinkCoroutine = StartCoroutine(BlinkCoroutine(m_eyeRenderer.sprite, m_charParts.EmotionFile));
				if (Random.value < TWICE_RATIO)
				{
					m_nextTimer = TWICE_SPAN;
				}
				else
				{
					m_nextTimer = Random.Range(MIN_BLINK_SPAN, MAX_BLINK_SPAN);
				}
			}
		}
	}
	IEnumerator BlinkCoroutine(Sprite defaultSprite, string spriteName)
	{
		if (m_blinkSprites.TryGetValue(spriteName, out BlinkAnimationData animationData) == false)
		{
			m_blinkCoroutine = null;
			yield break;
		}

		float blinkTimer = 0f;
		while(m_charParts.EmotionFile == spriteName)
		{
			blinkTimer += Time.deltaTime;
			int blinkIndex = (int)(blinkTimer / animationData.changeSpan);
			if (blinkIndex >= animationData.animationSprites.Length)
			{
				m_eyeRenderer.sprite = defaultSprite;
				break;
			}
			m_eyeRenderer.sprite = animationData.animationSprites[blinkIndex];
			yield return null;
		}
		m_blinkCoroutine = null;
	}

}
