using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class EndingController : MonoBehaviour 
{
    [SerializeField]
    private readonly Text m_upperTitle;
    [SerializeField]
    private readonly Text m_upperBody;
    [SerializeField]
    private readonly Text m_downerTitle;
    [SerializeField]
    private readonly Text m_downerBody;
	[SerializeField]
	private readonly Text m_credit;

    private string[] m_allTexts;
    private float m_stayTimer;
    enum EStatus
    {
        ComeIn,
        Stay,
        GoOut,
        AllOver,
    };
    private EStatus m_state;
    private int m_readCount = 0;
    private readonly StringBuilder m_readBodySB = new(256);
    const int MAX_ONCE_READ_LINES = 3;
	const float CREDIT_VIEW_TIME = 5f;
    const float VIEW_TIME = 3f;
    const float UPPER_TITLE_POS_Y = -45;
    const float DOWNER_TITLE_POS_Y = -200;
    const float NO_DISP_POS_X = -500;
    const float TITLE_START_POS_X = 800;
    readonly static Vector2 UPPER_TITLE_STOP_POS = new(40, UPPER_TITLE_POS_Y);
    readonly static Vector2 DOWNER_TITLE_STOP_POS = new(225, DOWNER_TITLE_POS_Y);
    readonly static Vector2 TITLE_MOVE_SPEED = new(-1200, 0);
    readonly static Vector2 UPPER_TITLE_START_POS = new(TITLE_START_POS_X, UPPER_TITLE_POS_Y);
    readonly static Vector2 DOWNER_TITLE_START_POS = new(TITLE_START_POS_X, DOWNER_TITLE_POS_Y);


    void Start ()
    {
        string endingData = ModifiableAssetsUtils.LoadTextFile(ProjectManager.GetScriptFolder(), Definition.ENDING_CREDIT_FILE);
        var endingDataObj = JsonUtility.FromJson<EndingData>(endingData);
        m_state = EStatus.ComeIn;
        m_upperTitle.rectTransform.anchoredPosition = UPPER_TITLE_START_POS;
        m_downerTitle.rectTransform.anchoredPosition = DOWNER_TITLE_START_POS;
        m_allTexts = endingDataObj.credits;
        ChangeText();
        StartCoroutine(ModifiableAssetsUtils.LoadAudioClipCoroutine(Definition.BGM_FOLDER, endingDataObj.bgmName, clip => SoundManager.PlayBGM(clip)));
        StartCoroutine(ShowCreditCoroutine(endingDataObj.bgmCredit));
        Fade.StartFadeIn(0.2f, Camera.main);
    }

    void Update() 
    {
        switch (m_state)
        {
        case EStatus.ComeIn:
            ComeIn();
            if (IsTitleStopped())
            {
                m_stayTimer = VIEW_TIME;
                m_state = EStatus.Stay;
            }
            break;
        case EStatus.Stay:
            m_stayTimer -= Time.deltaTime;
            if (m_stayTimer < 0)
            {
                if (m_readCount >= m_allTexts.Length)
                {
                    m_state = EStatus.AllOver;
                }
                else
                {
                    m_state = EStatus.GoOut;
                }
            }
            break;
        case EStatus.GoOut:
            GoOut();
            if (IsTitleMoved())
            {
                ChangeText();
                m_upperTitle.rectTransform.anchoredPosition = UPPER_TITLE_START_POS;
                m_downerTitle.rectTransform.anchoredPosition = DOWNER_TITLE_START_POS;

                m_state = EStatus.ComeIn;
            }
            break;
        case EStatus.AllOver:
            break;
        }
	}

    void ReadCredit(Text titleText, Text bodyText)
    {
        if (m_readCount < m_allTexts.Length)
        {
            // タイトル行読み取り時点で空欄ならスキップ.
            if (m_allTexts[m_readCount] == "")
            {
                m_readCount++;
                titleText.text = "";
                bodyText.text = "";
                return;
            } 
            titleText.text = m_allTexts[m_readCount];
            m_readCount++;
        }
        m_readBodySB.Clear();
        for(int i = 0; i < MAX_ONCE_READ_LINES - 1 && m_readCount < m_allTexts.Length; i++)
        {
            if (m_allTexts[m_readCount] == "")
            {
                m_readCount++;
                break;
            }
            if (m_readBodySB.Length != 0)
            {
                m_readBodySB.Append("\n");
            }
            m_readBodySB.Append(m_allTexts[m_readCount]);
            m_readCount++;
        }
        bodyText.text = m_readBodySB.ToString();
        if (m_readCount < m_allTexts.Length)
        {
            // 区切りの空行を飛ばす.
            if (m_allTexts[m_readCount] == "")
            {
                m_readCount++;
            }
        }
    }

    void GoOut()
    {
        m_upperTitle.rectTransform.anchoredPosition += Time.deltaTime * TITLE_MOVE_SPEED;
        m_downerTitle.rectTransform.anchoredPosition += Time.deltaTime * TITLE_MOVE_SPEED;
    }

    bool IsTitleMoved()
    {
        return m_upperTitle.rectTransform.anchoredPosition.x < NO_DISP_POS_X 
                && m_downerTitle.rectTransform.anchoredPosition.x < NO_DISP_POS_X;
    }
    void ComeIn()
    {
        var upperRect = m_upperTitle.rectTransform;
        if (upperRect.anchoredPosition != UPPER_TITLE_STOP_POS)
        {
            upperRect.anchoredPosition += Time.deltaTime * TITLE_MOVE_SPEED;
        }
        if (upperRect.anchoredPosition.x <= UPPER_TITLE_STOP_POS.x)
        {
            upperRect.anchoredPosition = UPPER_TITLE_STOP_POS;
        }

        var downerRect = m_downerTitle.rectTransform;
        if (downerRect.anchoredPosition != DOWNER_TITLE_STOP_POS)
        {
            downerRect.anchoredPosition += Time.deltaTime * TITLE_MOVE_SPEED;
        }
        if (downerRect.anchoredPosition.x <= DOWNER_TITLE_STOP_POS.x)
        {
            downerRect.anchoredPosition = DOWNER_TITLE_STOP_POS;
        }
    }

    bool IsTitleStopped()
    {
        return m_upperTitle.rectTransform.anchoredPosition == UPPER_TITLE_STOP_POS
                && m_downerTitle.rectTransform.anchoredPosition == DOWNER_TITLE_STOP_POS;
    }

    void ChangeText()
    {
        ReadCredit(m_upperTitle, m_upperBody);
        ReadCredit(m_downerTitle, m_downerBody);
    }

    IEnumerator ShowCreditCoroutine(string creditText)
    {
        if (m_credit == null)
        {
            yield break;
        }
        m_credit.text = creditText;
        m_credit.gameObject.SetActive(true);

        yield return new WaitForSeconds(CREDIT_VIEW_TIME);
        m_credit.gameObject.SetActive(false);
    }
}
