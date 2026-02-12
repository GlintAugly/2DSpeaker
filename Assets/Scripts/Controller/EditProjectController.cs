using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;

public class EditProjectController : MonoBehaviour
{
    [NonSerialized]
    public string m_projectName;
    [SerializeField]
    readonly EventSystem m_eventSystemForEditor;
    PlayController m_playController;
    const string PLAY_SCENE_NAME = "Play";
    const float PLAY_CAMERA_CLAMP = 0.7f;
    void OnEnable()
    {
        // EditモードとしてPlayシーンを読み込む.そのために、いったんプロジェクト名をクリアしておく.
        ProjectManager.SetProjectName(string.Empty);
        var loadSceneParameters = new LoadSceneParameters(LoadSceneMode.Additive);
        if(!SceneManager.GetSceneByName(PLAY_SCENE_NAME).isLoaded)
        {
            var playScene = SceneManager.LoadScene(PLAY_SCENE_NAME, loadSceneParameters);
            // LoadSceneは1フレーム後に行われるので、完了を待つようにする.
            StartCoroutine(AfterLoadingPlaySceneCoroutine(playScene));
        }
        if(string.IsNullOrEmpty(m_projectName))
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
            if(m_playController != null)
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
        ProjectManager.SetProjectName(m_projectName);

        // 手動で初期化.
        if(ModifiableAssetsUtils.IsFileExists(ProjectManager.GetScriptFolder(), Definition.PLAY_SCENE_INITIALIZE_DATA_FILE))
        {
            PlaySceneInitializeData initData = PlaySceneInitializeData.LoadFromJson(
                ModifiableAssetsUtils.LoadTextFile(ProjectManager.GetScriptFolder(), Definition.PLAY_SCENE_INITIALIZE_DATA_FILE)
            );
            m_playController.Initialize(initData);
        }
    }

    async void OnDisable()
    {
        ProjectManager.SetProjectName(string.Empty);
        SoundManager.StopVoice();
        SoundManager.StopBGM();
        SoundManager.StopSE();
        await SceneManager.UnloadSceneAsync(PLAY_SCENE_NAME);
        m_eventSystemForEditor.gameObject.SetActive(true);
    }
}
