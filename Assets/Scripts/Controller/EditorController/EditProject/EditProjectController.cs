using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class EditProjectController : MonoBehaviour, IEditController
{
    public EditorController EditorController { private get; set;}
    [NonSerialized]
    public string ProjectName;
    [SerializeField]
    EventSystem m_eventSystemForEditor;

    enum EditProjectMode
    {
        Main,
        InitializeEdit,
        ScriptEdit,
    }
    readonly Dictionary<EditProjectMode, IEditProjectController> m_modeCanvases = new();
    static readonly Dictionary<EditProjectMode, Type> m_modeControllers = new()
    {
        {EditProjectMode.Main, typeof(EditProjectMainController) },
        {EditProjectMode.InitializeEdit, typeof(EditInitializeController) },
        {EditProjectMode.ScriptEdit, typeof(EditScriptController) },
    };
    PlayController m_playController;
    
    const string PLAY_SCENE_NAME = "Play";
    const float PLAY_CAMERA_CLAMP = 0.7f;
    
    public void GotoEditProjectMain()
    {
        SwitchMode(EditProjectMode.Main);
    }

    public void GotoEditInitialize()
    {
        SwitchMode(EditProjectMode.InitializeEdit);
    }

    public void GotoEditScript()
    {
        SwitchMode(EditProjectMode.ScriptEdit);
    }

    public void BackToEditorMain()
    {
        EditorController.GotoMain();
    }

    void Awake()
    {
        foreach (var kvp in m_modeControllers)
        {
            var mode = kvp.Key;
            var type = kvp.Value;
            var canvasObj = GetComponentInChildren(type, true);
            if (canvasObj == null)
            {
                AppDebug.LogError("ERR: EditProjectControllerの子オブジェクトに{0}がありません", type.Name);
                continue;
            }
            if (canvasObj is IEditProjectController controller)
            {
                controller.EditProjectController = this;
                m_modeCanvases[mode] = controller;
            }
            else
            {
                AppDebug.LogError("ERR: {0}がIEditProjectControllerを実装していません", type.Name);
            }
        }
        SwitchMode(EditProjectMode.Main);
    }
    void OnEnable()
    {
        // EditモードとしてPlayシーンを読み込む.そのために、いったんプロジェクト名をクリアしておく.
        ProjectManager.SetProjectName(string.Empty);
        var loadSceneParameters = new LoadSceneParameters(LoadSceneMode.Additive);
        if (!SceneManager.GetSceneByName(PLAY_SCENE_NAME).isLoaded)
        {
            var playScene = SceneManager.LoadScene(PLAY_SCENE_NAME, loadSceneParameters);
            // LoadSceneは1フレーム後に行われるので、完了を待つようにする.
            StartCoroutine(AfterLoadingPlaySceneCoroutine(playScene));
        }
        if (string.IsNullOrEmpty(ProjectName))
        {
            AppDebug.LogError("ERR: Project name is not set in EditProjectController.");
            return;
        }
        m_eventSystemForEditor.gameObject.SetActive(false);
    }

    IEnumerator AfterLoadingPlaySceneCoroutine(Scene playScene)
    {
        while (!playScene.isLoaded)
        {
            yield return null;
        }

        foreach (var rootObj in playScene.GetRootGameObjects())
        {
            m_playController = rootObj.GetComponent<PlayController>();
            if (m_playController != null)
            {
                break;
            }
        }
        if (m_playController == null)
        {
            AppDebug.LogError("ERR: PlayControllerがルートオブジェクトにありません");
            yield break;
        }
        var offset = 1.0f - PLAY_CAMERA_CLAMP;
        m_playController.MainCamera.rect = new Rect(offset, offset, PLAY_CAMERA_CLAMP, PLAY_CAMERA_CLAMP);
        ProjectManager.SetProjectName(ProjectName);

        // 手動で初期化.
        if (ModifiableAssetsUtils.IsFileExists(ProjectManager.GetScriptFolder(), Definition.PLAY_SCENE_INITIALIZE_DATA_FILE))
        {
            PlaySceneInitializeData initData = PlaySceneInitializeData.LoadFromJson(
                ModifiableAssetsUtils.LoadTextFile(ProjectManager.GetScriptFolder(), Definition.PLAY_SCENE_INITIALIZE_DATA_FILE)
            );
            m_playController.Initialize(initData);
        }
    }

    async void OnDisable()
    {
        if (ProjectManager.IsInitialized)
        {
            ProjectManager.SetProjectName(string.Empty);
        }
        if (SoundManager.IsInitialized)
        {
            SoundManager.StopAll();
        }
        if (SceneManager.GetSceneByName(PLAY_SCENE_NAME).isLoaded)
        {
            await SceneManager.UnloadSceneAsync(PLAY_SCENE_NAME);
        }
        if (m_eventSystemForEditor != null)
        {
            m_eventSystemForEditor.gameObject.SetActive(true);
        }
    }

    void SwitchMode(EditProjectMode mode)
    {
        foreach (var kvp in m_modeCanvases)
        {
            kvp.Value.gameObject.SetActive(kvp.Key == mode);
        }
    }
}
