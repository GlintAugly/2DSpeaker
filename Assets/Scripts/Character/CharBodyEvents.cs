using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class CharBodyEvents
{
    public UnityEvent<int> OnChangeSort = new();
    public UnityEvent<string, bool> OnChangeEmo = new();
    public UnityEvent OnChangeFlip = new();
    public UnityEvent<bool> OnChangeHide = new();
    public UnityEvent<EmotionData> OnLoadEmotionData = new();
    public UnityEvent<AudioSource> OnStartTalking = new();
}