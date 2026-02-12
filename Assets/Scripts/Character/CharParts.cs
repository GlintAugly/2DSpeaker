using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class CharParts : MonoBehaviour
{
	public enum EPartName
	{
		Body,
		Face,
		Eye,
		Mouth,
		Eyebrow,
		Backeffect,
		Other
	}

	[SerializeField]
	private EPartName m_partsName;
	public string EmotionFile { get; private set; }
	public Sprite BaseSprite { get; private set; }
	private SpriteRenderer m_spriteRenderer;
	protected SpriteRenderer SpriteRenderer
	{
		get
		{
			if (m_spriteRenderer == null)
			{
				m_spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
			}
			return m_spriteRenderer;
		}
	}
    private Dictionary<string, string> m_emotionMap = new();
	// Use this for initialization
	void Awake ()
    {
		// 親にイベントを登録.
		var parentEvents = GetComponentInParent<CharBody>().Events;

		if (parentEvents == null)
		{
			AppDebug.LogError("ERR: parentEvents is null");
			return;
		}
		parentEvents.OnChangeSort.AddListener(ChangeSort);
		parentEvents.OnChangeEmo.AddListener(ChangeEmo);
		parentEvents.OnChangeFlip.AddListener(ChangeFlip);
		parentEvents.OnChangeHide.AddListener(ChangeHide);
        parentEvents.OnLoadEmotionData.AddListener(SetupEmotionMap);
    }

	void SetupEmotionMap(EmotionData emotionData)
	{
		foreach (var entry in emotionData.emotions)
		{
			string partValue = entry.parts.GetPartValue(m_partsName);
			if (partValue != null && partValue != "none")
			{
				m_emotionMap.Add(entry.name, partValue);
			}
		}
	}
    void ChangeEmo(string emoName_d, bool keep = true)
    {
		string sFileName = null;
        string emoName = emoName_d.Replace(" ","");
		if (m_emotionMap.ContainsKey(emoName))
		{
			// 感情変更.
			sFileName = NormalizeResourcePath(m_emotionMap[emoName]);
		}
		else if (keep == false && m_emotionMap.ContainsKey("default"))
		{
			// 感情を元に戻す.
			sFileName = NormalizeResourcePath(m_emotionMap["default"]);
		}
		if (string.IsNullOrEmpty(sFileName))
		{
			return;
		}

		Sprite Readsprite = ModifiableAssetsUtils.LoadSprite(Definition.CHARACTER_FOLDER, sFileName);
		if (Readsprite == null)
		{
			AppDebug.LogError("ERR: Emo File cannot read EmoName = {0}", sFileName);
			return;
		}
		SpriteRenderer.sprite = Readsprite;
		BaseSprite = Readsprite;
		EmotionFile = sFileName;
    }

    void ChangeSort(int num)
    {
        SpriteRenderer.sortingOrder = num * Definition.CHARACTER_SORTING_ORDER_MULTIPLIER
										+ SpriteRenderer.sortingOrder % Definition.CHARACTER_SORTING_ORDER_MULTIPLIER;
    }

    void ChangeFlip()
    {
        SpriteRenderer.flipX = !SpriteRenderer.flipX;
    }
	void ChangeHide(bool isHide)
	{
		Color col = SpriteRenderer.color;
		col.a = isHide ? 0f : 1f;
		SpriteRenderer.color = col;
	}

	static string NormalizeResourcePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return path;
		}

		var normalized = path.Replace("\\", "/");
		return normalized;
	}
}
