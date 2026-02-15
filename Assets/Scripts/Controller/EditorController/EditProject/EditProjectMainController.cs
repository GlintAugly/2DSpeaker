using UnityEngine;
using UnityEngine.UI;

public class EditProjectMainController : MonoBehaviour, IEditProjectController
{
    public EditProjectController EditProjectController { private get; set; }
    [SerializeField]
    Button m_editInitializeDataButton;
    [SerializeField]
    Button m_editScriptButton;
    [SerializeField]
    Button m_backButton;

    void Start()
    {
        m_editInitializeDataButton.onClick.AddListener(OnClickEditInitializeData);
        m_editScriptButton.onClick.AddListener(OnClickEditScript);
        m_backButton.onClick.AddListener(OnClickBack);
    }

    void OnClickEditInitializeData()
    {
        EditProjectController.GotoEditInitialize();
    }

    void OnClickEditScript()
    {
        EditProjectController.GotoEditScript();
    }

    void OnClickBack()
    {
        EditProjectController.BackToEditorMain();
    }
}