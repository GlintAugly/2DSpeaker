using UnityEngine;
using UnityEngine.UI;

public class EditInitializeController : MonoBehaviour, IEditProjectController
{
    public EditProjectController EditProjectController { private get; set; }
    [SerializeField]
    Button m_backButton;

    void Start()
    {
        m_backButton.onClick.AddListener(OnClickBack);
    }

    void OnClickBack()
    {
        EditProjectController.GotoEditProjectMain();
    }
}
