using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class TitleController : MonoBehaviour
{
    [SerializeField]
    private Dropdown projectDropdown;
    [SerializeField]
    private Button playButton;
    [SerializeField]
    private Button editButton;
    [SerializeField]
    private Text noProjectsText;

    private void Start()
    {
        playButton.onClick.AddListener(OnPlayButtonClicked);
        editButton.onClick.AddListener(OnEditButtonClicked);

        projectDropdown.ClearOptions();
        var options = ModifiableAssetsUtils.GetProjects().Select(p => new Dropdown.OptionData(p)).ToList();
        if (options.Count == 0)
        {
            noProjectsText.gameObject.SetActive(true);
            playButton.gameObject.SetActive(false);
            projectDropdown.gameObject.SetActive(false);
        }
        else
        {
            noProjectsText.gameObject.SetActive(false);
            playButton.gameObject.SetActive(true);
            projectDropdown.gameObject.SetActive(true);
            projectDropdown.AddOptions(options);
        }
    }

    private void OnPlayButtonClicked()
    {
        int selectedIndex = projectDropdown.value;
        string selectedProject = projectDropdown.options[selectedIndex].text;
        ProjectManager.SetProjectName(selectedProject);
        
        SceneManager.LoadScene("Play");

    }

    private void OnEditButtonClicked()
    {
        Debug.Log("Edit button clicked.");
        // Add logic to open the editor for the selected project
    }
}
