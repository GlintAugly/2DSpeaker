
using System.IO;

public class ProjectManager : MonoBehaviourSingleton<ProjectManager>
{
    string projectName = "test";
    
    public static string[] GetAllProjects()
    {
        return ModifiableAssetsUtils.GetFolders(Definition.SCRIPT_FOLDER);
    }

    public static bool IsProjectSelected()
    {
        return !string.IsNullOrEmpty(Instance.projectName);
    }

    public static void CreateNewProject(string name)
    {
        ModifiableAssetsUtils.CreateFolderIfNotExists(Definition.SCRIPT_FOLDER, name);
        ModifiableAssetsUtils.CreateFolderIfNotExists(Definition.VOICE_FOLDER, name);
    }
    public static string GetScriptFolder()
    {
        return Path.Combine(Definition.SCRIPT_FOLDER, Instance.projectName);
    }
    public static void SetProjectName(string name)
    {
        CreateNewProject(name);
        Instance.projectName = name;
    }
    public static string GetVoiceFolder()
    {
        return Path.Combine(Definition.VOICE_FOLDER, Instance.projectName);
    }
}
