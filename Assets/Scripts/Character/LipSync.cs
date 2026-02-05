using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CharParts))]
public class LipSync : MonoBehaviour
{
	class LipSyncAnimationData
	{
		public Sprite[] animationSprites;
		public float changeSpan;
	}
    private AudioSource m_talkAudio = null;
	private SpriteRenderer m_mouthRenderer;
	private CharParts m_charParts;
	private Dictionary<string, LipSyncAnimationData> m_talkingSprites = new();
	private Coroutine m_lipSyncCoroutine = null;

	public void Initialize(LipSyncInitializeDataItem[] lipSyncInitializeData)
	{
		foreach (var initializeData in lipSyncInitializeData)
		{
			if (string.IsNullOrEmpty(initializeData.defaultMouthOpenSprite) == false)
			{
				Sprite[] animSprites = new Sprite[initializeData.animationSprites.Length];
				for (var i = 0; i < initializeData.animationSprites.Length; i++)
				{
					Sprite animSprite = ModifiableAssetsUtils.LoadSprite(Definition.CHARACTER_FOLDER, initializeData.animationSprites[i]);
					if (animSprite == null)
					{
						AppDebug.LogError("LipSyncアニメーション用Spriteの読み込み失敗: {0}", initializeData.animationSprites[i]);
						continue;
					}
					animSprites[i] = animSprite;
				}
				m_talkingSprites[initializeData.defaultMouthOpenSprite] = new LipSyncAnimationData
				{
					animationSprites = animSprites,
					changeSpan = initializeData.changeSpan
				};
			}
		}
	}
	public void SetTalkAudio(AudioSource audioSource)
	{
		m_talkAudio = audioSource;
	}

	// Use this for initialization
	void Start () 
	{
		m_mouthRenderer = gameObject.GetComponent<SpriteRenderer>();
		m_charParts = gameObject.GetComponent<CharParts>();

		// 親にイベントを登録.
		var parentEvents = GetComponentInParent<CharBody>().Events;
		if (parentEvents == null)
		{
			AppDebug.LogError("ERR: parentEvents is null");
			return;
		}
		parentEvents.OnStartTalking.AddListener(SetTalkAudio);
	}

	// Update is called once per frame
	void Update () 
	{
        if (m_talkAudio != null && m_talkAudio.isPlaying)
		{
			if(m_talkingSprites.ContainsKey(m_charParts.EmotionFile))
			{
				m_lipSyncCoroutine ??= StartCoroutine(LipSyncCoroutine(m_mouthRenderer.sprite, m_charParts.EmotionFile));
			}
			else
			{
				// 該当する口パクアニメーションがないなら、判定にもう入らないようにする.
				m_talkAudio = null;
			}
		}
	}

	IEnumerator LipSyncCoroutine(Sprite defaultSprite, string spriteName)
	{
		float talkTimer = 0f;
		if (m_talkingSprites.TryGetValue(spriteName, out LipSyncAnimationData animationData) == false)
		{
			yield break;
		}
		while(m_charParts.EmotionFile == spriteName)
		{
			if(m_talkAudio == null || !m_talkAudio.isPlaying)
			{
				m_mouthRenderer.sprite = defaultSprite;
				break;
			}
			talkTimer += Time.deltaTime;
			int talkIndex = (int)(talkTimer / animationData.changeSpan) % animationData.animationSprites.Length;
			m_mouthRenderer.sprite = animationData.animationSprites[talkIndex];
			yield return null;
		}
		m_lipSyncCoroutine = null;
		m_talkAudio = null;
	}
}
