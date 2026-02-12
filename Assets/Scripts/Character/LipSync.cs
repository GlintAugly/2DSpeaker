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
	private float[] m_audioSamples = new float[0];
	private int m_lastTimeSample = 0;
	private int m_lastTalkSpriteIndex = -1;
	private float m_maxRms = MAX_RMS_DEFAULT;
	private const int MIN_CHANGE_INTERVAL = 100;
	private const float RMS_BORDER_MULTIPLIER = 0.1f;
	private const float MAX_RMS_DEFAULT = 0f;

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
		m_lastTalkSpriteIndex = -1;
		m_maxRms = MAX_RMS_DEFAULT;
		if(m_talkAudio == null || m_talkAudio.clip == null)
		{
			m_audioSamples = new float[0];
			return;
		}
		if (m_audioSamples.Length != m_talkAudio.clip.samples)
		{
			m_audioSamples = new float[m_talkAudio.clip.samples];
		}
		m_talkAudio.clip.GetData(m_audioSamples, 0);
		m_lastTimeSample = 0;
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
		int nowTimeSamples = m_talkAudio.timeSamples;
		if (nowTimeSamples - m_lastTimeSample < MIN_CHANGE_INTERVAL)
		{
			return;
		}
		float sum = 0f;
		for (int i = m_lastTimeSample; i < Math.Min(nowTimeSamples, m_audioSamples.Length); i++)
		{
			float sample = m_audioSamples[i];
			sum += sample * sample;
		}
		float rms = Mathf.Sqrt(sum / m_audioSamples.Length);
		AppDebug.Log("LipSync RMS: {0}", rms);
		float rmsBorder = RMS_BORDER_MULTIPLIER * m_maxRms;
		if(rms < rmsBorder)
		{
			rms = 0f;
		}
		m_maxRms = Mathf.Max(m_maxRms, rms);

		int maxIndex = animationData.animationSprites.Length;
		int talkIndex = Mathf.Clamp(Mathf.FloorToInt((rms - rmsBorder) / (m_maxRms - rmsBorder) * maxIndex) + 1, 0, maxIndex);
		if (talkIndex != m_lastTalkSpriteIndex)
		{
			if(talkIndex == 0)
			{
				m_mouthRenderer.sprite = defaultSprite;
			}
			else
			{
				m_mouthRenderer.sprite = animationData.animationSprites[talkIndex - 1];
			}
			m_lastTalkSpriteIndex = talkIndex;
		}
		m_lastTimeSample = nowTimeSamples;
	}
}
