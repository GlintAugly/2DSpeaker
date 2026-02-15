using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditScriptController : MonoBehaviour, IEditProjectController
{
	public EditProjectController EditProjectController { private get; set; }
	[SerializeField]
	Button m_backButton;

	void Start()
	{
		m_backButton.onClick.AddListener(OnClickBack);
	}

    public void Initialize()
    {
    }

	void OnClickBack()
	{
		EditProjectController.GotoEditProjectMain();
	}
}
