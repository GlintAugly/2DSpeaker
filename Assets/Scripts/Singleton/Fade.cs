using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Image))]
public class Fade : MonoBehaviourSingleton<Fade> {
    private Image m_render;
    private Canvas m_canvas;
    private EFadeStatus m_fade;
    public static bool IsFadeOut => Instance.m_fade == EFadeStatus.FadeOut;
    Coroutine m_coroutine;
    private Sprite m_blackFadeSprite;
    private Sprite m_whiteFadeSprite;
    const string FADE_LAYER_NAME = "Fade";
    const string BLACK_FADE_SPRITE_PATH = "Image/makkuro";
    const string WHITE_FADE_SPRITE_PATH = "Image/white";
    public enum EFadeType
    {
        Black,
        White,
    }
    enum EFadeStatus{
        FadeStop,
        FadeIn,
        FadeOut,
    };
    private static int FadeMultiplier(EFadeStatus fade) => fade switch
    {
        EFadeStatus.FadeIn => -1,
        EFadeStatus.FadeOut => 1,
        _ => 0,
    };
    // Use this for initialization
	protected override void Awake ()
    {
        base.Awake();
        
        // 親がアタッチされていなければCanvasオブジェクトを作成
        if (transform.parent == null)
        {
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
            GameObject canvasObject = new("Canvas");
            m_canvas = canvasObject.AddComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceCamera;
            m_canvas.sortingOrder = Definition.MAX_SORTING_ORDER;
            transform.SetParent(canvasObject.transform);
            DontDestroyOnLoad(canvasObject);
        }
        
        transform.position = Vector3.zero;
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        m_render = GetComponent<Image>();
        m_blackFadeSprite = Resources.Load<Sprite>(BLACK_FADE_SPRITE_PATH);
        m_whiteFadeSprite = Resources.Load<Sprite>(WHITE_FADE_SPRITE_PATH);
    }

    public static void StartFadeIn(float setTime, Camera targetCamera, EFadeType fadeType = EFadeType.Black)
    {
        Sprite sprite = FetchFadeSprite(fadeType);
        StartFadeIn(setTime, targetCamera, sprite);
    }
    public static void StartFadeIn(float setTime, Camera targetCamera, Sprite sprite)
    {
        if (Instance.m_coroutine != null)
        {
            Instance.StopCoroutine(Instance.m_coroutine);
        }
        if (sprite)
        {
            Instance.m_render.sprite = sprite;
        }
        Instance.m_canvas.worldCamera = targetCamera;
        if(targetCamera != null)
        {
            Instance.m_canvas.sortingLayerName = FADE_LAYER_NAME;
        }
        Instance.m_fade = EFadeStatus.FadeIn;
        Instance.m_coroutine = Instance.StartCoroutine(Instance.FadeCoroutine(EFadeStatus.FadeIn, setTime));
    }

    public static void StartFadeOut(float setTime, Camera targetCamera, EFadeType fadeType = EFadeType.Black)
    {
        Sprite sprite = FetchFadeSprite(fadeType);
        StartFadeOut(setTime, targetCamera, sprite);
    }
    
    public static void StartFadeOut(float setTime, Camera targetCamera, Sprite sprite)
    {
        if (Instance.m_coroutine != null)
        {
            Instance.StopCoroutine(Instance.m_coroutine);
        }
        if (sprite)
        {
            Instance.m_render.sprite = sprite;
        }
        Instance.m_fade = EFadeStatus.FadeOut;
        Instance.m_canvas.worldCamera = targetCamera;
        if(targetCamera != null)
        {
            Instance.m_canvas.sortingLayerName = FADE_LAYER_NAME;
        }
        Instance.m_coroutine = Instance.StartCoroutine(Instance.FadeCoroutine(EFadeStatus.FadeOut, setTime));
    }

    private IEnumerator FadeCoroutine(EFadeStatus fadeType, float setTime)
    {
        var leftTime = setTime;
        var alpha = fadeType == EFadeStatus.FadeIn ? 1f : 0f;
        m_fade = fadeType;
        while (m_fade != EFadeStatus.FadeStop)
        {
            leftTime -= Time.deltaTime;
            if (0 > leftTime)
            {
                alpha = fadeType == EFadeStatus.FadeIn ? 0f : 1f;
                m_fade = EFadeStatus.FadeStop;
            }
            else
            {
                alpha += FadeMultiplier(fadeType) * (Time.deltaTime / setTime);
            }
            Color col = m_render.color;
            col.a = alpha;
            m_render.color = col;
            yield return null;
        }
    }
    static Sprite FetchFadeSprite(EFadeType fadeType) => fadeType switch
    {
        EFadeType.Black => Instance.m_blackFadeSprite,
        EFadeType.White => Instance.m_whiteFadeSprite,
        _ => null,
    };
}
