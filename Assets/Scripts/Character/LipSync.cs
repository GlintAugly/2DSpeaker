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
	}
	private AudioSource m_talkAudio = null;
	private SpriteRenderer m_mouthRenderer;
	private CharParts m_charParts;
	private Dictionary<string, LipSyncAnimationData> m_talkingSprites = new();
	private readonly float[] m_audioSamples = new float[GET_SAMPLES_LENGTH];
	private int m_lastTalkSpriteIndex = -1;
	private float m_maxRms = MAX_RMS_DEFAULT;
	private const int GET_SAMPLES_LENGTH = 1024;
	private const float RMS_BORDER_MULTIPLIER = 0.1f;
	private const float MAX_RMS_DEFAULT = 0f;

	public void Initialize(LipSyncInitializeDataItem[] lipSyncInitializeData)
	{
		m_talkingSprites.Clear();
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
				};
			}
		}
	}
	public void SetTalkAudio(AudioSource audioSource)
	{
		m_talkAudio = audioSource;
		m_lastTalkSpriteIndex = -1;
		m_maxRms = MAX_RMS_DEFAULT;
		if(m_talkAudio == null || m_talkAudio.clip == null)
		{
			this.enabled = false;
			return;
		}
		this.enabled = true;
	}

	void Awake () 
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
        if (m_talkingSprites.Count > 0 && m_talkAudio != null)
		{
			if(m_talkingSprites.ContainsKey(m_charParts.EmotionFile))
			{
				UpdateLipSyncByVolume(m_charParts.BaseSprite, m_charParts.EmotionFile);
			}
			else
			{
				// 該当する口パクアニメーションがないなら、判定にもう入らないようにする.
				SetTalkAudio(null);
			}
		}
	}

	void UpdateLipSyncByVolume(Sprite defaultSprite, string spriteName)
	{
		if (m_talkingSprites.TryGetValue(spriteName, out LipSyncAnimationData animationData) == false)
		{
			return;
		}
		if (m_talkAudio == null || !m_talkAudio.isPlaying)
		{
			m_mouthRenderer.sprite = defaultSprite;
			SetTalkAudio(null);
			return;
		}
		m_talkAudio.GetOutputData(m_audioSamples, 0);
		float sum = 0f;
		for (int i = 0; i < m_audioSamples.Length; i++)
		{
			float sample = m_audioSamples[i];
			sum += sample * sample;
		}
		float rms = Mathf.Sqrt(sum / m_audioSamples.Length);
		
		if(rms <= 0f)
		{
			m_mouthRenderer.sprite = defaultSprite;
			m_lastTalkSpriteIndex = 0;
		}
		else
		{
			m_maxRms = Mathf.Max(m_maxRms, rms);
			float rmsBorder = RMS_BORDER_MULTIPLIER * m_maxRms;

			int maxIndex = animationData.animationSprites.Length;
			int talkIndex = Mathf.Clamp(Mathf.CeilToInt((rms - rmsBorder) / (m_maxRms - rmsBorder) * maxIndex), 0, maxIndex);
			if (talkIndex != m_lastTalkSpriteIndex)
			{
				m_mouthRenderer.sprite = talkIndex == 0 ? defaultSprite : animationData.animationSprites[talkIndex - 1];
				m_lastTalkSpriteIndex = talkIndex;
			}
		}
	}
}
