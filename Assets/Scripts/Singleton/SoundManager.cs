using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviourSingleton<SoundManager>
{
    public const int NO_LOOP = -1;
    private AudioSource bgmSource;
    private AudioSource[] seSource;
    private AudioSource[] voiceSource;
    private int bgmLoopSampleLength = NO_LOOP;
    private int bgmLoopEndSamplePosition = NO_LOOP;
    private float volumeMaster = 1.0f;
    private float volumeBGM = 1.0f;
    private float volumeSE = 1.0f;
    private float volumeVoice = 1.0f;
    private Coroutine m_bgmLoopCoroutine = null;
    const int SE_SOURCE_COUNT = 5;
    const int VOICE_SOURCE_COUNT = 5;
    protected override void Awake()
    {
        base.Awake();
        GameObject go = Instance.gameObject;
        bgmSource = go.AddComponent<AudioSource>();
        seSource = new AudioSource[SE_SOURCE_COUNT];
        for (int i = 0; i < SE_SOURCE_COUNT; i++)
        {
            seSource[i] = go.AddComponent<AudioSource>();
        }
        voiceSource = new AudioSource[VOICE_SOURCE_COUNT];
        for (int i = 0; i < VOICE_SOURCE_COUNT; i++)
        {
            voiceSource[i] = go.AddComponent<AudioSource>();
        }
    }

    public static void SetMasterVolume(float volume)
    {
        Instance.volumeMaster = Mathf.Clamp01(volume);
        Instance.bgmSource.volume = Instance.volumeMaster * Instance.volumeBGM;
        foreach(var source in Instance.seSource)
        {
            source.volume = Instance.volumeMaster * Instance.volumeSE;
        }
        foreach(var source in Instance.voiceSource)
        {
            source.volume = Instance.volumeMaster * Instance.volumeVoice;
        }
    }

    public static void SetBGMVolume(float volume)
    {
        Instance.volumeBGM = Mathf.Clamp01(volume);
        Instance.bgmSource.volume = Instance.volumeMaster * Instance.volumeBGM;
    }

    public static void SetSEVolume(float volume)
    {
        Instance.volumeSE = Mathf.Clamp01(volume);
        foreach(var source in Instance.seSource)
        {
            source.volume = Instance.volumeMaster * Instance.volumeSE;
        }
    }

    public static void SetVoiceVolume(float volume)
    {
        Instance.volumeVoice = Mathf.Clamp01(volume);
        foreach(var source in Instance.voiceSource)
        {
            source.volume = Instance.volumeMaster * Instance.volumeVoice;
        }
    }

    public static void PlayBGM(AudioClip clip, int loopLength = NO_LOOP, int loopEndPos = NO_LOOP)
    {
        Instance.bgmSource.Stop();
        if (Instance.m_bgmLoopCoroutine != null)
        {
            Instance.StopCoroutine(Instance.m_bgmLoopCoroutine);
            Instance.m_bgmLoopCoroutine = null;
        }

        Instance.bgmSource.clip = clip;
        Instance.bgmSource.volume = Instance.volumeMaster * Instance.volumeBGM;
        Instance.bgmLoopSampleLength = loopLength;
        Instance.bgmLoopEndSamplePosition = loopEndPos;
        Instance.bgmSource.Play();
        if (loopLength != NO_LOOP)
        {
            Instance.m_bgmLoopCoroutine = Instance.StartCoroutine(Instance.RoopBGMCoroutine());
        }
    }

    public static void PlaySE(AudioClip clip)
    {
        foreach(var source in Instance.seSource)
        {
            if(!source.isPlaying)
            {
                source.clip = clip;
                source.loop = false;
                source.volume = Instance.volumeMaster * Instance.volumeSE;
                source.Play();
                break;
            }
        }
    }

    public static AudioSource PlayVoice(AudioClip clip)
    {
        for(int i = 0; i < VOICE_SOURCE_COUNT; i++)
        {
            if(!Instance.voiceSource[i].isPlaying)
            {
                Instance.voiceSource[i].clip = clip;
                Instance.voiceSource[i].loop = false;
                Instance.voiceSource[i].volume = Instance.volumeMaster * Instance.volumeVoice;
                Instance.voiceSource[i].Play();
                return Instance.voiceSource[i];
            }
        }
        return null;
    }

    public static void StopBGM()
    {
        Instance.bgmSource.Stop();
        Instance.bgmLoopSampleLength = NO_LOOP;
        Instance.bgmLoopEndSamplePosition = NO_LOOP;
    }
    public static void StopSE()
    {
        foreach(var source in Instance.seSource)
        {
            source.Stop();
        }
    }

    public static void StopVoice()
    {
        foreach(var source in Instance.voiceSource)
        {
            source.Stop();
        }
    }

    private IEnumerator RoopBGMCoroutine()
    {
        while (bgmLoopSampleLength != NO_LOOP && bgmLoopEndSamplePosition != NO_LOOP)
        {
            if (bgmSource.isPlaying)
            {
                if (bgmSource.timeSamples >= bgmLoopEndSamplePosition)
                {
                    bgmSource.timeSamples -= bgmLoopSampleLength;
                }
            }
            yield return null;
        }
    }
}
