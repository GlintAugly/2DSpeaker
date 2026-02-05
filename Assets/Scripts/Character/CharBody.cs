using UnityEngine;
using System.Collections;
public class CharBody : CharParts
{
	/// <summary>
	/// m_moveVect : 移動先の座標
	/// m_moveTimer : 移動が始まってから何秒経ったか
    /// m_events : キャラクターの状態変化イベント
	/// </summary>
    private Vector3 m_moveVect;
    private float m_moveTimer;
    private CharBodyEvents m_events = new();
    public CharBodyEvents Events => m_events;

    const float MOVE_DURATION = 1f;

    void InitializeEmotion(string dataPath)
    {
        // 感情リストの作成
        string texts = ModifiableAssetsUtils.LoadTextFile(Definition.CHARACTER_FOLDER, dataPath);
        if (texts != null) 
        {
			// JSON形式で読み込み
			var emotionData = EmotionData.LoadFromJson(texts);
			if (emotionData != null)
            {
                m_events.OnLoadEmotionData.Invoke(emotionData);
            }
			else
			{
				AppDebug.LogError("ERR: Failed to parse emotion JSON {0}", dataPath);
			}
        }
        else
        {
            AppDebug.LogError("ERR: text cannot read {0}", dataPath);
        }
    }

    public void Initialize(CharacterDataEntry charData)
    {
        InitializeEmotion(charData.emotionListPath);
        // 初期感情設定
        ChangeEmo(charData.defaultEmotion, false);

        // まばたき設定
        var blink = GetComponentInChildren<Blink>();
        if (blink != null)
        {
            blink.Initialize(charData.blink);
        }

        // 口パク設定
        var lipSync = GetComponentInChildren<LipSync>();
        if (lipSync != null)
        {
            lipSync.Initialize(charData.lipSync);
        }
    }
    public int GetSortingOrder()
    {
        return SpriteRenderer.sortingOrder / Definition.CHARACTER_SORTING_ORDER_MULTIPLIER;
    }

    public void ChangeSort(int sortNum)
    {
        m_events.OnChangeSort.Invoke(sortNum);
    }

    public void ChangeEmo(string emoName, bool keep = true)
    {
		m_events.OnChangeEmo.Invoke(emoName, keep);
    }
    
    public void ChangeFlip()
    {
		m_events.OnChangeFlip.Invoke();
    }

	public void ChangeHide(bool isHide)
	{
        m_events.OnChangeHide.Invoke(isHide);
	}
    
    public void TalkStart(AudioSource audioSource)
    {
        m_events.OnStartTalking.Invoke(audioSource);
    }

    public void ChangeDirection(float direction)
    {
        Quaternion rotation = transform.rotation;
        Vector3 rotationAngles = rotation.eulerAngles;
        rotationAngles.y = direction;
        rotation = Quaternion.Euler(rotationAngles);
        transform.rotation = rotation;
    }

    public void Move(Vector2 pos)
    {
        m_moveVect = (Vector3)pos - transform.position;
        m_moveTimer = 0f;
        StartCoroutine(MovingCoroutine());
    }

	IEnumerator MovingCoroutine () 
    {
        while(m_moveVect != Vector3.zero)
        {
            float dTime = Time.deltaTime;
            if (m_moveTimer >= MOVE_DURATION)
            {
                transform.position += m_moveVect * (MOVE_DURATION - m_moveTimer) / MOVE_DURATION;
                m_moveTimer = 0f;
                m_moveVect = Vector3.zero;
            }
            else
            {
                transform.position += m_moveVect * dTime / MOVE_DURATION;
                m_moveTimer += dTime;
            }
            yield return null;
        }
    }

    public void Leave()
    {
        GetComponent<Animator>().SetTrigger("Leave");
    }

    public void Disappear()
    {
        gameObject.SetActive(false);
    }
    
    public void ChangeRotation(float direction)
    {
        Quaternion rotation = transform.rotation;
        Vector3 rotationAngles = rotation.eulerAngles;
        rotationAngles.z = direction;
        rotation = Quaternion.Euler(rotationAngles);
        transform.rotation = rotation;
	}
}
