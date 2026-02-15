using UnityEngine;
using UnityEngine.UI;

public class EditCharacterController : MonoBehaviour, IEditController
{
    public EditorController EditorController { private get; set; }
    [SerializeField]
    Button m_backButton;

    void Start()
    {
        m_backButton.onClick.AddListener(OnClickBack);
    }

    void OnClickBack()
    {
        EditorController.GotoMain();
    }
}
