using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

using Parameters = System.Collections.Generic.Dictionary<string, object>;
using System.Net.WebSockets;

public class PlayController : MonoBehaviour 
{
	delegate bool CommandFunction(Parameters parameters, CommandSpec spec);
	public enum EHorizontalSlot
	{
	   Right,
	   Left,
	   Center,
	}
	public enum EVerticalSlot
	{
		Center,
		Ground,
	}

	[SerializeField]
	private GameObject m_textFrame;
	[SerializeField]
	private Animator m_blackBoard;
	[SerializeField]
	private Text m_blackBoardText;
	[SerializeField]
	private SpriteRenderer m_backGround;
	[SerializeField]
	private Text m_credit;
	[SerializeField]
	private GameObject m_characterTemplate;
	[SerializeField]
	private Font m_defaultFont;
	[SerializeField]
	private int m_defaultFontSize;

	private IDictionary<string, CharBody> m_characters = new Dictionary<string, CharBody>();
	private IDictionary<string, Text> m_texts = new Dictionary<string, Text>();
	private IDictionary<string, (CommandFunction, CommandSpec)> m_commandMap;
	private JsonScriptReader m_scriptReader;
	private float m_timer;
	private float m_talktimer = 0f;
	private bool m_isEnd = true;
	private int m_stopMute = -1;
	private float m_interval;
	private float m_bgmVolume;
    float m_sideRight;
    float m_sideLeft;
    float m_sideSlideX;
    float m_sideSlideY;
    float m_groundY;
	Vector3 m_bigPosRight;
    Vector3 m_bigPosLeft;
    Vector3 m_bigSize;
	const string TEXT_OBJ_NAME_FORMAT = "{0}Text";
	const float START_FADE_TIME = 0.9f;
	const float END_FADE_TIME = 0.5f;
	const float TEXT_NOISE_SCALE = 5.0f;
    private static readonly WaitForSeconds WAIT_FOR_CREDIT_HIDE = new(5.0f);

	/// <summary>
	/// コマンドマップの初期化
	/// ScriptableObjectからCommandSpecを読み込み、手動で紐付け
	/// </summary>
	void InitializeCommandMap()
	{
		// ScriptableObjectからCommandSpecを読み込む
		var commandSpecs = Resources.LoadAll<CommandSpec>("CommandSpecs/");
		var specDict = new Dictionary<string, CommandSpec>();
		
		foreach (var spec in commandSpecs)
		{
			specDict[spec.CommandName] = spec;
		}
		
		// 手動マッピング
		m_commandMap = new Dictionary<string, (CommandFunction, CommandSpec)>()
		{
			{"entry", (CallEntry, Utilities.GetDictionaryValueOrDefault(specDict, "entry", null))},
			{"talk", (CallTalk, Utilities.GetDictionaryValueOrDefault(specDict, "talk", null))},
			{"hideSubtitle", (CallHideSubtitle, Utilities.GetDictionaryValueOrDefault(specDict, "hideSubtitle", null))},
			{"mute", (CallMute, Utilities.GetDictionaryValueOrDefault(specDict, "mute", null))},
			{"changeEmotion", (CallChangeEmotion, Utilities.GetDictionaryValueOrDefault(specDict, "changeEmotion", null))},
			{"direction", (CallDirection, Utilities.GetDictionaryValueOrDefault(specDict, "direction", null))},
			{"rotation", (CallRotation, Utilities.GetDictionaryValueOrDefault(specDict, "rotation", null))},
			{"anime", (CallAnime, Utilities.GetDictionaryValueOrDefault(specDict, "anime", null))},
			{"flip", (CallFlip, Utilities.GetDictionaryValueOrDefault(specDict, "flip", null))},
			{"particularPosition", (CallParticularPosition, Utilities.GetDictionaryValueOrDefault(specDict, "particularPosition", null))},
			{"position", (CallPosition, Utilities.GetDictionaryValueOrDefault(specDict, "position", null))},
			{"particularMove", (CallParticularMove, Utilities.GetDictionaryValueOrDefault(specDict, "particularMove", null))},
			{"move", (CallMove, Utilities.GetDictionaryValueOrDefault(specDict, "move", null))},
			{"wait", (CallWait, Utilities.GetDictionaryValueOrDefault(specDict, "wait", null))},
			{"waitTalkEnd", (CallWaitTalkEnd, Utilities.GetDictionaryValueOrDefault(specDict, "waitTalkEnd", null))},
			{"Leave", (CallLeave, Utilities.GetDictionaryValueOrDefault(specDict, "Leave", null))},
			{"giant", (CallGiant, Utilities.GetDictionaryValueOrDefault(specDict, "giant", null))},
			{"returnSize", (CallReturnSize, Utilities.GetDictionaryValueOrDefault(specDict, "returnSize", null))},
			{"returnSizeAtPartiularPosition", (CallReturnSizeAtParticularPosition, Utilities.GetDictionaryValueOrDefault(specDict, "returnSizeAtPartiularPosition", null))},
			{"changeBBActive", (CallChangeBBActive, Utilities.GetDictionaryValueOrDefault(specDict, "changeBBActive", null))},
			{"writeBB", (CallWriteBB, Utilities.GetDictionaryValueOrDefault(specDict, "writeBB", null))},
			{"changeBG", (CallChangeBG, Utilities.GetDictionaryValueOrDefault(specDict, "changeBG", null))},
			{"fadeIn", (CallFadeIn, Utilities.GetDictionaryValueOrDefault(specDict, "fadeIn", null))},
			{"fadeOut", (CallFadeOut, Utilities.GetDictionaryValueOrDefault(specDict, "fadeOut", null))},
			{"ending", (CallEnding, Utilities.GetDictionaryValueOrDefault(specDict, "ending", null))},
			{"hide", (CallHide, Utilities.GetDictionaryValueOrDefault(specDict, "hide", null))},
			{"show", (CallShow, Utilities.GetDictionaryValueOrDefault(specDict, "show", null))},
			{"firstPriority", (CallFirstPriority, Utilities.GetDictionaryValueOrDefault(specDict, "firstPriority", null))},
			{"endBGM", (CallEndBGM, Utilities.GetDictionaryValueOrDefault(specDict, "endBGM", null))},
			{"stopTalk", (CallStopTalk, Utilities.GetDictionaryValueOrDefault(specDict, "stopTalk", null))},
			{"changeInterval", (CallChangeInterval, Utilities.GetDictionaryValueOrDefault(specDict, "changeInterval", null))},
			{"changeBGM", (CallChangeBGM, Utilities.GetDictionaryValueOrDefault(specDict, "changeBGM", null))},
		};
	}
	
	void Awake()
	{
		// コマンドマップの初期化
		InitializeCommandMap();
	}
	
	/// <summary>
	/// 音楽データ、スクリプトファイルの読み込み
	/// </summary>
	void Start()
	{
		Initialize();
	}

	void Initialize()
	{
		PlaySceneInitializeData initData = PlaySceneInitializeData.LoadFromJson(
			ModifiableAssetsUtils.LoadTextFile(ProjectManager.GetScriptFolder(), Definition.PLAY_SCENE_INITIALIZE_DATA_FILE)
		);

		// キャラクターとテキストオブジェクトの生成・初期化
		InitializeCaharacters(initData);

		// 座標の設定
		m_sideRight = initData.sideRight;
		m_sideLeft = initData.sideLeft;
		m_sideSlideX = initData.sideSlideX;
		m_sideSlideY = initData.sideSlideY;
		m_groundY = initData.groundPos;
		m_bigPosRight = new Vector2(initData.bigPosRight, initData.bigPosY);
		m_bigPosLeft = new Vector2(initData.bigPosLeft, initData.bigPosY);
		m_bigSize = new Vector3(initData.bigSize, initData.bigSize, 1);

		// 背景の設定
		ChangeBG(initData.BGName, initData.BGSize, initData.BGPos);

		// 黒板、メッセージボックスの設定.
		InitializeTextBox(m_blackBoard.GetComponent<Image>(), initData.BBData);
		InitializeTextBox(m_textFrame.GetComponent<Image>(), initData.SubtitleData);

		// BGM再生とクレジット表示
		m_bgmVolume = initData.BGMVolume;
		SetupBGM(initData.BGMName, initData.BGMLoopSampleLength, initData.BGMLoopEndSamplePos);
		if (!string.IsNullOrEmpty(initData.BGMCreditText))
		{
			ShowCredit(initData.BGMCreditText);
		}
		m_interval = initData.talkInterval;
		
		// スクリプトの読み込み・初期設定
		m_scriptReader = new JsonScriptReader(ProjectManager.GetScriptFolder(), Definition.EXEC_SCRIPT_FILE);
		m_isEnd = false;
		Fade.StartFadeIn(START_FADE_TIME);
	}

	void InitializeCaharacters(PlaySceneInitializeData initData)
	{
		foreach(var chara in m_characters)
		{
			Destroy(chara.Value.gameObject);
		}
		foreach(var text in m_texts)
		{
			Destroy(text.Value.gameObject);
		}
		m_characters.Clear();
		m_texts.Clear();

		CharacterData charData = CharacterData.LoadFromJson(
			ModifiableAssetsUtils.LoadTextFile(Definition.CHARACTER_FOLDER, Definition.CHARACTER_DATA_FILE)
		);
		foreach(var charaName in initData.characters)
		{
			var charDataItem = charData.GetCharacterDataItem(charaName);
			if (charDataItem == null)
			{
				AppDebug.LogError("初期化データに存在しないキャラクター名が指定されました: {0}", charaName);
				continue;
			}
			// キャラクター生成・初期化
			var charaInstance = Instantiate(m_characterTemplate, null);
			charaInstance.name = charDataItem.name;
			charaInstance.SetActive(false);
			var charBody = charaInstance.GetComponent<CharBody>();
			charBody.Initialize(charDataItem);
			m_characters[charaName] = charBody;

			// テキスト生成・初期化
			string textObjName = string.Format(TEXT_OBJ_NAME_FORMAT, charaName);
			GameObject textObj = new(textObjName);
			textObj.transform.SetParent(m_textFrame.transform);
            Text text = textObj.AddComponent<Text>();
			text.color = charDataItem.textData.color;
			text.rectTransform.localScale = Vector3.one;
			m_texts[textObjName] = text;
		}
	}

	void InitializeTextBox(Image baseImage, TextBoxDataItem initData)
	{
		baseImage.rectTransform.anchoredPosition = initData.basePosition;
		baseImage.rectTransform.sizeDelta = initData.baseSize;
		if(string.IsNullOrEmpty(initData.baseFileName) == false)
		{
			Sprite baseSprite = ModifiableAssetsUtils.LoadSprite(Definition.IMAGE_FOLDER, initData.baseFileName);
			if (baseSprite != null)
			{
				baseImage.sprite = baseSprite;
			}
			else
			{
				AppDebug.LogError("{0}用背景画像の読み込みに失敗しました: {1}", baseImage.name, initData.baseFileName);
			}
		}
		foreach(var text in baseImage.GetComponentsInChildren<Text>())
		{
			text.font = m_defaultFont;
			text.fontSize = m_defaultFontSize;
			if(string.IsNullOrEmpty(initData.fontName) == false)
			{
				Font loadedFont = ModifiableAssetsUtils.LoadFont(initData.fontName, initData.fontSize);
				if(loadedFont != null)
				{
					text.font = loadedFont;
					if(initData.fontSize > 0)
					{
						text.fontSize = initData.fontSize;
					}
				}
				else
				{
					AppDebug.LogError("テキスト用フォントの読み込みに失敗しました: {0}", initData.fontName);
				}
			}
			Vector2 offset = new(UnityEngine.Random.Range(-TEXT_NOISE_SCALE, TEXT_NOISE_SCALE), UnityEngine.Random.Range(-TEXT_NOISE_SCALE, TEXT_NOISE_SCALE));
			text.rectTransform.pivot = Vector2.up;
			text.rectTransform.anchorMin = Vector2.up;
			text.rectTransform.anchorMax = Vector2.up;
			text.rectTransform.anchoredPosition = initData.textPosition + offset;
			text.rectTransform.sizeDelta = initData.textSize;
		}
	}
	
	/// <summary>
	/// m_timerごとにファイルを一行ずつ読む
	/// ファイルがなくなったら終了処理(次のシーンに)
	/// </summary>
	void Update ()
	{
		m_timer -= Time.deltaTime;
		if (m_talktimer > 0)
		{
			m_talktimer -= Time.deltaTime;
			if (m_talktimer < 0)
			{
				m_talktimer = 0;
			}
		}
		if (m_timer <= 0f)
		{
			// 終了処理.
			if (true == m_isEnd)
			{
				m_timer = float.MaxValue;
				SceneManager.LoadScene("Ending");
				return;
			}
			while (m_timer <= 0f && !m_isEnd)
			{
				// 終了判定.
				if (m_scriptReader.IsAllRead)
				{
					CloseAllText();
					if (!Fade.IsFadeOut)
					{
						Fade.StartFadeOut(END_FADE_TIME);
					}
					m_timer = END_FADE_TIME;
					m_isEnd = true;
					break;
				}
				if (m_scriptReader.ReadNextCommand(out string commandName, out Parameters parameters))
				{
					if (m_commandMap.TryGetValue(commandName, out var comandData))
					{
						var func = comandData.Item1;
						var spec = comandData.Item2;
						if (func == null || spec == null)
						{
							AppDebug.LogWarning("コマンドハンドラーまたは仕様が未定義: {0}", commandName);
							continue;
						}
						if (func(parameters, spec))
						{
							if (SafeCast(parameters, spec, "wait", out float waitTime))
							{
								Wait(waitTime);
							}
						}
					}
					else
					{
						AppDebug.LogError("不明なコマンド:{0}", commandName);
					}
				}
			}
		}
	}

	private bool SafeCast<T>(Parameters parameters, CommandSpec spec, string key, out T value)
	{
		value = default(T);
		if (parameters.TryGetValue(key, out object obj) == false)
		{
			// commandsにキーが存在しないことはあり得る.
			return false;
		}
		if (spec.ParamTypeMap.TryGetValue(key, out Type expectedType) == false)
		{
			//specにキーが存在しないのは異常.
			AppDebug.LogError("SafeCast: キーがSpecに見つかりません。キー: {0}", key);
			return false;
		}
		return SafeCast(obj, expectedType, out value);
	}
	/// <summary>
	/// 型安全なキャストヘルパー
	/// </summary>
	private bool SafeCast<T>(object obj, Type expectedType, out T value)
	{
		value = default(T);
		// nullチェック
		if (obj == null)
		{
			AppDebug.LogWarning("SafeCast<{0}>: オブジェクトがnullです", typeof(T).Name);
			return false;
		}
		
#if UNITY_EDITOR || DEBUG
		// デバッグビルドでは型チェック
		var actualType = obj.GetType();
		var requestedType = typeof(T);
		
		if (expectedType != null && requestedType != expectedType && !expectedType.IsAssignableFrom(requestedType))
		{
			AppDebug.LogError("SafeCast: キャスト先型が仕様と不一致。仕様: {0}, 要求: {1}", expectedType.Name, requestedType.Name);
			return false;
		}
		
		if (!requestedType.IsAssignableFrom(actualType))
		{
			AppDebug.LogError("SafeCast: 型変換不可。実際: {0}, 要求: {1}", actualType.Name, requestedType.Name);
			return false;
		}
#endif
		
		value = (T)obj;
		return true;
	}

	bool CallChangeBGM(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "bgmName", out string bgmName) == false)
		{
			AppDebug.LogError("Command 'setupBGM' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		if(SafeCast(parameters, spec, "loopStart", out int loopStart) == false
		|| SafeCast(parameters, spec, "loopEnd", out int loopEndPos) == false)
		{
			SetupBGM(bgmName);
		}
		else
		{
			int loopLength = loopEndPos - loopStart;
			SetupBGM(bgmName, loopLength, loopEndPos);
		}

		SafeCast(parameters, spec, "credit", out string credit);
		if (!string.IsNullOrEmpty(credit))
		{
			ShowCredit(credit);
		}
		return true;
	}

	void SetupBGM(string bgmName, int loopLength = SoundManager.NO_LOOP, int loopEndPos = SoundManager.NO_LOOP)
	{
		StartCoroutine(ModifiableAssetsUtils.LoadAudioClipCoroutine(Definition.BGM_FOLDER, bgmName, clip => {
			if (loopLength < 0 || loopEndPos < 0)
			{
				SoundManager.PlayBGM(clip);
			}
			else
			{
				SoundManager.PlayBGM(clip, loopLength, loopEndPos);
			}
			SoundManager.SetBGMVolume(m_bgmVolume);
		}));
	}
	
	bool CallWait(Parameters parameters, CommandSpec spec)
	{
		// Waitは共通処理にあるので何もしない
		return true;
	}
	bool CallWaitTalkEnd(Parameters _, CommandSpec __)
	{
		Wait(m_talktimer);
		return true;
	}
	/// <summary>
	/// 待つ
	/// </summary>
	/// <param name="time">待ち時間</param>
	void Wait(float time)
	{
		m_timer = time;
	}

	bool CallAnime(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false
		|| SafeCast(parameters, spec, "animeName", out string animeName) == false)
		{
			AppDebug.LogError("Command 'anime' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		ChangeAnime(targetName, animeName);
		return true;
	}
	/// <summary>
	/// アニメーション変更
	/// </summary>
	/// <param name="targetName">対象オブジェクトの名前</param>
	/// <param name="AnimeName">アニメーション名</param>
	void ChangeAnime(string targetName, string AnimeName)
	{
		if (m_characters.TryGetValue(targetName, out CharBody character))
		{
			Animator animator = character.gameObject.GetComponent<Animator>();
			if (animator != null)
			{
				animator.SetTrigger(AnimeName);
			}
		}
	}

	bool CallEntry(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false
		|| SafeCast(parameters, spec, "horizontalSlot", out EHorizontalSlot horizontalSlot) == false
		|| SafeCast(parameters, spec, "slide", out int slide) == false)
		{
			AppDebug.LogError("Command 'entry' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		Entry(targetName, horizontalSlot, slide);
		return true;
	}
	/// <summary>
	/// 出現
	/// </summary>
	/// <param name="targetName">対象オブジェクトの名前</param>
	/// <param name="horizontalSlot">出現位置(LRC)</param>
	/// <param name="slide">何人目か(登場キャラ分だけずらして表示)</param>
	void Entry(string targetName, EHorizontalSlot horizontalSlot = EHorizontalSlot.Left, int slide = 0)
	{
		if (m_characters.TryGetValue(targetName, out CharBody character) == false)
		{
			AppDebug.LogError("Entry: キャラクターが見つかりません。name={0}", targetName);
			return;
		}
		var sidePosX = horizontalSlot switch
		{
			EHorizontalSlot.Left => m_sideLeft,
			EHorizontalSlot.Right => m_sideRight,
			_ => 0f,
		};
		character.transform.position = new Vector3(sidePosX + m_sideSlideX * slide, m_groundY);
		character.gameObject.SetActive(true);
		ChangeSort(targetName);
		ChangeAnime(targetName, "Entry");
	}

	bool CallParticularMove(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false
		|| SafeCast(parameters, spec, "horizontalSlot", out EHorizontalSlot horizontalSlot) == false
		|| SafeCast(parameters, spec, "horizontalSlide", out int horizontalSlide) == false
		|| SafeCast(parameters, spec, "verticalSlot", out EVerticalSlot verticalSlot) == false
		|| SafeCast(parameters, spec, "verticalSlide", out int verticalSlide) == false)
		{
			AppDebug.LogError("Command 'particularMove' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		var setPosition = CalcParticularPosition(horizontalSlot, horizontalSlide, verticalSlot, verticalSlide);
		Move(targetName, setPosition);
		return true;
	}
	bool CallMove(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false
		|| SafeCast(parameters, spec, "position", out Vector2 position) == false)
		{
			AppDebug.LogError("Command 'move' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		Move(targetName, position);
		return true;
	}
	/// <summary>
	/// 移動指示
	/// </summary>
	/// <param name="targetName">対象オブジェクトの名前</param>
	/// <param name="position">移動位置</param>
	void Move(string targetName, Vector2 position)
	{

		if (m_characters.TryGetValue(targetName, out CharBody character) == false)
		{
			AppDebug.LogError("Move: キャラクターが見つかりません。name={0}", targetName);
			return;
		}
		character.Move(position);
	}

	// テキストフレームの非表示化
	bool CallHideSubtitle(Parameters parameters, CommandSpec spec)
	{
		m_textFrame.SetActive(false);
		return true;
	}
	bool CallTalk(Parameters parameters, CommandSpec spec)
	{
		if (m_scriptReader.CurrentIndex <= m_stopMute)
		{
			// Muteが効いてるので正常終了.
			return true;
		}

		if (SafeCast(parameters, spec, "characterName", out string characterName) == false
		|| SafeCast(parameters, spec, "fileName", out string fileName) == false)
		{
			AppDebug.LogError("Command 'talk' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		if (SafeCast(parameters, spec, "subtitle", out string subtitle) == false)
		{
			subtitle = "";
		}
		if (SafeCast(parameters, spec, "emotion", out string emotion) == false)
		{
			emotion = "";
		}
		if (SafeCast(parameters, spec, "isNewText", out bool isNewText) == false)
		{
			isNewText = true;
		}
		Speak(characterName, fileName, subtitle, emotion, isNewText);
		return true;
	}
	/// <summary>
	/// 話す
	/// 音声とテキストのデータファイル名は一致させる必要がある
	/// 音声データの長さ分だけ待つように設定する.
	/// </summary>
	/// <param name="targetName">話を始めるオブジェクトの名前</param>
	/// <param name="fileName">音声(テキスト)データファイル名</param>
	/// <param name="emotion">表情の変化 空欄だと変化無し</param>
	/// <param name="isNewText">これまで表示していたテキストを消すかどうか</param>
	void Speak(string targetName, string fileName, string subtitle, string emotion, bool isNewText = true)
	{
		AudioClip voice = ModifiableAssetsUtils.LoadAudioClip(ProjectManager.GetVoiceFolder(), fileName);
		if (voice == null)
		{
			AppDebug.LogError("Speak:Cannot read Voice={0}{1}:{4}", ProjectManager.GetVoiceFolder(), fileName, voice);
			return;
		}
		var voiceLength = voice.length + m_interval;
		var voiceSource = SoundManager.PlayVoice(voice);
		if(voiceSource == null)
		{
			AppDebug.LogError("Speak: Cannot play voice file. fileName={0}", fileName);
			return;
		}
		if (m_characters.TryGetValue(targetName, out CharBody character))
		{
			character.TalkStart(voiceSource);
		}
		if (m_texts.TryGetValue(string.Format(TEXT_OBJ_NAME_FORMAT, targetName), out Text textObj))
		{
			ChangeText(textObj, subtitle, isNewText);
		}
		if (emotion != "")
		{
			ChangeEmotion(targetName, emotion);
		}
		m_talktimer = voiceLength;
		Wait(voiceLength);
	}

	bool CallMute(Parameters parameters, CommandSpec spec)
	{
		Mute();
		return true;
	}
	/// <summary>
	/// 話を止める
	/// </summary>
	void Mute()
	{
		SoundManager.StopVoice();
	}

	bool CallFirstPriority(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false)
		{
			AppDebug.LogError("Command 'firstPriority' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		ChangeSort(targetName);
		return true;
	}
	/// <summary>
	/// 渡された名前のオブジェクトの描画順を最後にする。
	/// 他はずれる。
	/// </summary>
	/// <param name="charName">描画順を最後にするオブジェクト名</param>
	void ChangeSort(string charName)
	{
        if (m_characters.TryGetValue(charName, out CharBody target) == false)
        {
            AppDebug.LogError("Error:ChangeSort \ntargetObj == null charname={0}", charName);
            return;
        }
        int targetSortingOrder = target.GetSortingOrder();
		int biggestSortingOrder = m_characters.Count - 1;
		if (targetSortingOrder == biggestSortingOrder)
		{
			// すでに一番前なら何もしない
			return;
		}
		else
		{
			foreach(var character in m_characters.Values)
			{
				int sortingOrder = character.GetSortingOrder();
				if (sortingOrder > targetSortingOrder)
				{
					// 自分より前にいるやつを一つ後ろにずらす
					character.ChangeSort(sortingOrder - 1);
				}
			}
			target.ChangeSort(biggestSortingOrder);
		}
	}

	bool CallChangeEmotion(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false
		|| SafeCast(parameters, spec, "emotion", out string emotion) == false)
		{
			AppDebug.LogError("Command 'changeEmotion' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		if (SafeCast(parameters, spec, "keep", out bool keep) == false)
		{
			keep = true;
		}
		ChangeEmotion(targetName, emotion, keep);
		return true;
	}
	/// <summary>
	/// 渡されたオブジェクトの感情を変える。
	/// keepがfalseになると、その感情を持たないオブジェクトのスプライトは0番になる。
	/// </summary>
	/// <param name="targetName">対象オブジェクトの名前</param>
	/// <param name="emotion">感情の名前</param>
	/// <param name="keep">感情を持たないオブジェクトのスプライトをデフォルトに戻さないかどうか</param>
	void ChangeEmotion(string targetName, string emotion, bool keep = true)
	{
		if (m_characters.TryGetValue(targetName, out CharBody character) == false)
		{
			AppDebug.LogError("ChangeEmotion: キャラクターが見つかりません。name={0}", targetName);
			return;
		}
		character.ChangeEmo(emotion, keep);
	}

	bool CallDirection(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false
		|| SafeCast(parameters, spec, "direction", out float direction) == false)
		{
			AppDebug.LogError("Command 'direction' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		ChangeDirection(targetName, direction);
		return true;
	}
	/// <summary>
	/// 渡された名前のオブジェクトの向きを変える。
	/// アニメーションの回転の方向は変わる
	/// </summary>
	/// <param name="targetName">対象オブジェクトの名前</param>
	/// <param name="direction">変更後のy軸座標</param>
	void ChangeDirection(string targetName, float direction)
	{
		if (m_characters.TryGetValue(targetName, out CharBody character) == false)
		{
			AppDebug.LogError("ChangeDirection: キャラクターが見つかりません。name={0}", targetName);
			return;
		}
		character.ChangeDirection(direction);
	}

	bool CallRotation(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false
		|| SafeCast(parameters, spec, "direction", out float direction) == false)
		{
			AppDebug.LogError("Command 'rotation' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		ChangeRotation(targetName, direction);
		return true;
	}
	/// <summary>
	/// 渡された名前のオブジェクトの回転角を変える。
	/// </summary>
	/// <param name="targetName">対象オブジェクトの名前</param>
	/// <param name="direction">変更後のz軸座標</param>
	void ChangeRotation(string targetName, float direction)
	{
		if (m_characters.TryGetValue(targetName, out CharBody character) == false)
		{
			AppDebug.LogError("ChangeRotation: キャラクターが見つかりません。name={0}", targetName);
			return;
		}
		character.ChangeRotation(direction);
	}

	bool CallFlip(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false)
		{
			AppDebug.LogError("Command 'flip' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		ChangeFlip(targetName);
		return true;
	}
	/// <summary>
	/// 渡された名前のオブジェクトの向きを変える。
	/// アニメーションの回転の方向は変わらない
	/// </summary>
	/// <param name="targetName">対象オブジェクトの名前</param>
	void ChangeFlip(string targetName)
	{
		if (m_characters.TryGetValue(targetName, out CharBody character))
		{
			character.ChangeFlip();
		}
	}

	bool CallParticularPosition(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false
		|| SafeCast(parameters, spec, "horizontalSlot", out EHorizontalSlot horizontalSlot) == false
		|| SafeCast(parameters, spec, "horizontalSlide", out int horizontalSlide) == false
		|| SafeCast(parameters, spec, "verticalSlot", out EVerticalSlot verticalSlot) == false
		|| SafeCast(parameters, spec, "verticalSlide", out int verticalSlide) == false)
		{
			AppDebug.LogError("Command 'particularPosition' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		var position = CalcParticularPosition(horizontalSlot, horizontalSlide, verticalSlot, verticalSlide);
		SetPosition(targetName, position);
		return true;
	}

	Vector2 CalcParticularPosition(EHorizontalSlot horizontalSlot, int slide_x, EVerticalSlot verticalSlot, int slide_y)
	{
        var x = horizontalSlot switch
        {
            EHorizontalSlot.Left => m_sideLeft,
            EHorizontalSlot.Right => m_sideRight,
            EHorizontalSlot.Center => 0f,
            _ => m_sideLeft,
        };
        x += m_sideSlideX * slide_x;
        var y = verticalSlot switch
        {
            EVerticalSlot.Center => 0f,
            EVerticalSlot.Ground => m_groundY,
            _ => m_groundY,
        };
        y += m_sideSlideY * slide_y;
		return new Vector2(x, y);
	}
	bool CallPosition(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false
		|| SafeCast(parameters, spec, "position", out Vector2 position) == false)
		{
			AppDebug.LogError("Command 'position' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		SetPosition(targetName, position);
		return true;
	}
	/// <summary>
	/// 位置の変更(一瞬)
	/// </summary>
	/// <param name="targetName">対象オブジェクトの名前</param>
	/// <param name="position">移動後の位置</param>
	void SetPosition(string targetName, Vector2 position)
	{
		if (m_characters.TryGetValue(targetName, out CharBody character) == false)
		{
			AppDebug.LogError("SetPosition: キャラクターが見つかりません。name={0}", targetName);
			return;
		}
		character.transform.position = new Vector3(position.x, position.y);
	}

	bool CallHide(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false)
		{
			AppDebug.LogError("Command 'hide' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		Hide(targetName, true);
		return true;
	}

	bool CallShow(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false)
		{
			AppDebug.LogError("Command 'show' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		Hide(targetName, false);
		return true;
	}
	/// <summary>
	/// 画像を表示しない
	/// </summary>
	/// <param name="targetName">対象オブジェクトの名前</param>
	/// <param name="isHide">trueだと隠す</param>
	void Hide(string targetName, bool isHide)
	{
		if (m_characters.TryGetValue(targetName, out CharBody character) == false)
		{
			AppDebug.LogError("Hide: キャラクターが見つかりません。name={0}", targetName);
			return;
		}
		character.ChangeHide(isHide);
	}

	bool CallLeave(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false)
		{
			AppDebug.LogError("Command 'leave' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		Leave(targetName);
		return true;
	}
	/// <summary>
	/// 退出
	/// </summary>
	/// <param name="targetName">対象オブジェクトの名前</param>
	void Leave(string targetName)
	{
		if (m_characters.TryGetValue(targetName, out CharBody character) == false)
		{
			AppDebug.LogError("Leave: キャラクターが見つかりません。name={0}", targetName);
			return;
		}
		character.Leave();
	}

	bool CallGiant(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false
		|| SafeCast(parameters, spec, "horizontalSlot", out EHorizontalSlot horizontalSlot) == false)
		{
			AppDebug.LogError("Command 'giant' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		Giant(targetName, horizontalSlot);
		return true;
	}
	/// <summary>
	/// 巨大化(顔だけ出す感じ)
	/// </summary>
	/// <param name="targetName">対象オブジェクトの名前</param>
	/// <param name="horizontalSlot">巨大化したのをどこに出すか(L,R)</param>
	void Giant(string targetName, EHorizontalSlot horizontalSlot)
	{
		if (m_characters.TryGetValue(targetName, out CharBody character) == false)
		{
			AppDebug.LogError("Giant: キャラクターが見つかりません。name={0}", targetName);
			return;
		}
		character.transform.position = horizontalSlot switch
		{
			EHorizontalSlot.Left => m_bigPosLeft,
			EHorizontalSlot.Right => m_bigPosRight,
			_ => m_bigPosLeft,
		};
		character.transform.localScale = m_bigSize;
	}

	bool CallReturnSizeAtParticularPosition(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false
		|| SafeCast(parameters, spec, "horizontalSlot", out EHorizontalSlot horizontalSlot) == false
		|| SafeCast(parameters, spec, "horizontalSlide", out int horizontalIndex) == false
		|| SafeCast(parameters, spec, "verticalSlot", out EVerticalSlot verticalSlot) == false
		|| SafeCast(parameters, spec, "verticalSlide", out int verticalIndex) == false)
		{
			AppDebug.LogError("Command 'returnSizeAtParticcularPosition' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		var position = CalcParticularPosition(horizontalSlot, horizontalIndex, verticalSlot, verticalIndex);
		ReturnSize(targetName, position);
		return true;
	}
	bool CallReturnSize(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "targetName", out string targetName) == false
		|| SafeCast(parameters, spec, "position", out Vector2 position) == false)
		{
			AppDebug.LogError("Command 'returnSize' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		ReturnSize(targetName, position);
		return true;
	}
	/// <summary>
	/// 元に戻る
	/// </summary>
	/// <param name="targetName">対象オブジェクトの名前</param>
	/// <param name="position">元に戻ったときの位置</param>
	void ReturnSize(string targetName, Vector2 position)
	{
		if (m_characters.TryGetValue(targetName, out CharBody character) == false)
		{
			AppDebug.LogError("ReturnSize: キャラクターが見つかりません。name={0}", targetName);
			return;
		}
		character.transform.position = new Vector3(position.x, position.y);
		character.transform.localScale = Vector3.one;
	}

	/// <summary>
	/// テキストの変更と表示。
	/// timeを指定すれば一定時間後に消える。
	/// </summary>
	/// <param name="target">対象オブジェクトの名前</param>
	/// <param name="text">表示するテキスト名</param>
	/// <param name="isNewText">これまで表示していたテキストを消すかどうか</param>
	void ChangeText(Text target, string text, bool isNewText = true)
	{
		m_textFrame.SetActive(true);
		if (isNewText == true)
		{
			foreach (var textObj in m_texts.Values)
			{
				textObj.gameObject.SetActive(false);
			}
		}

		target.gameObject.SetActive(true);
		target.text = text;
	}

	bool CallChangeBBActive(Parameters parameters, CommandSpec spec)
	{
		ChangeBBActive();
		return true;
	}
	/// <summary>
	/// 黒板の入場
	/// </summary>
	void ChangeBBActive()
	{
		if (m_blackBoardText.gameObject.activeSelf)
		{
			m_blackBoardText.gameObject.SetActive(false);
			m_blackBoard.SetTrigger("Down");
		}
		else
		{
			m_blackBoardText.gameObject.SetActive(true);
			m_blackBoard.SetTrigger("Up");
		}
	}

	bool CallWriteBB(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "text", out string text) == false)
		{
			AppDebug.LogError("Command 'writeBB' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		WriteBB(text);
		return true;
	}
	/// <summary>
	/// 黒板に文字を書く
	/// </summary>
	/// <param name="txt">黒板に書く文字</param>
	void WriteBB(string txt)
	{
		m_blackBoardText.text = txt;
	}

	bool CallChangeBG(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "fileName", out string fileName) == false
		|| SafeCast(parameters, spec, "size", out Vector2 size) == false
		|| SafeCast(parameters, spec, "position", out Vector2 position) == false)
		{
			AppDebug.LogError("Command 'changeBG' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		ChangeBG(fileName, size, position);
		return true;
	}
	/// <summary>
	/// 背景画像差し替え
	/// </summary>
	/// <param name="fileName">背景画像ファイル名</param>
	/// <param name="size">背景オブジェクトサイズ</param>
	/// <param name="position">背景オブジェクト位置</param>
	void ChangeBG(string fileName, Vector2 size, Vector2 position)
	{
		Sprite sprite = ModifiableAssetsUtils.LoadSprite(Definition.IMAGE_FOLDER, fileName);
		if (sprite == null)
		{
			AppDebug.LogError("ERR: BG File cannot read BGName = {0}", fileName);
			return;
		}
		m_backGround.sprite = sprite;
		m_backGround.transform.localScale = size;
		m_backGround.transform.position = position;
	}

	void ShowCredit(string creditText)
	{
		m_credit.text = creditText;
		StartCoroutine(HideCreditCoroutine());
	}

	System.Collections.IEnumerator HideCreditCoroutine()
	{
		m_credit.gameObject.SetActive(true);
		yield return WAIT_FOR_CREDIT_HIDE;
		m_credit.gameObject.SetActive(false);
	}

	bool CallFadeIn(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "time", out float time) == false)
		{
			AppDebug.LogError("Command 'fadeIn' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		Fade.StartFadeIn(time);
		return true;
	}

	bool CallFadeOut(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "time", out float time) == false)
		{
			AppDebug.LogError("Command 'fadeOut' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		Fade.StartFadeOut(time);
		return true;
	}

	bool CallEnding(Parameters _, CommandSpec __)
	{
		Fade.StartFadeOut(END_FADE_TIME);
		m_timer = END_FADE_TIME;
		m_isEnd = true;
		return true;
	}

	bool CallEndBGM(Parameters _, CommandSpec __)
	{
		SoundManager.StopBGM();
		return true;
	}

	bool CallStopTalk(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "index", out int index) == false)
		{
			AppDebug.LogError("Command 'stopTalk' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		m_stopMute = index;
		return true;
	}

	bool CallChangeInterval(Parameters parameters, CommandSpec spec)
	{
		if (SafeCast(parameters, spec, "interval", out float interval) == false)
		{
			AppDebug.LogError("Command 'changeInterval' missing required parameters. index:{0}", m_scriptReader.CurrentIndex);
			return false;
		}
		m_interval = interval;
		return true;
	}
	/// <summary>
	/// 出てきている文字列をすべて消す.
	/// </summary>
	void CloseAllText()
	{
		WriteBB("");
		foreach (var textObj in m_texts.Values)
		{
			textObj.gameObject.SetActive(false);
		}
	}
}
