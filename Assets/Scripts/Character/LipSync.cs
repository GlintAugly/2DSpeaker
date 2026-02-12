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
		PrepareAudioSamples();
		m_lastTimeSample = 0;
	}

	void PrepareAudioSamples()
	{
		var clip = m_talkAudio.clip;
		int samples = clip.samples;
		int channels = clip.channels;
		float[] tempSamples = new float[samples * channels];
		clip.GetData(tempSamples, 0);
		if (m_audioSamples.Length != samples)
		{
			m_audioSamples = new float[samples];
		}
		// マルチチャンネル対応
		if (channels == 1)
		{
			// モノラルはそのままコピー.
			Array.Copy(tempSamples, m_audioSamples, samples);
		}
		else
		{
			// ステレオ以上は平均化してモノラル化.
			for (int i = 0; i < samples; i++)
			{
				float sum = 0f;
				int baseIndex = i * channels;
				for (int c = 0; c < channels; c++)
				{
					sum += tempSamples[baseIndex + c];
				}
				m_audioSamples[i] = sum / channels;
			}
		}
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
        if (m_talkAudio != null)
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
		int startSample = Mathf.Clamp(m_lastTimeSample, 0, m_audioSamples.Length);
		int endSample = Mathf.Clamp(nowTimeSamples, 0, m_audioSamples.Length);
		int windowLength = endSample - startSample;
		if (windowLength <= 0)
		{
			m_lastTimeSample = nowTimeSamples;
			return;
		}
		for (int i = startSample; i < endSample; i++)
		{
			float sample = m_audioSamples[i];
			sum += sample * sample;
		}
		float rms = Mathf.Sqrt(sum / windowLength);
		
		if(rms <= 0f)
		{
			m_mouthRenderer.sprite = defaultSprite;
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
		m_lastTimeSample = nowTimeSamples;
	}
}
