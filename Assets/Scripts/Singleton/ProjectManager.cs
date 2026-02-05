
using System.IO;

public class ProjectManager : MonoBehaviourSingleton<ProjectManager>
{
    string projectName = "test";
    
    public static string GetScriptFolder()
    {
        return Path.Combine(Definition.SCRIPT_FOLDER, Instance.projectName);
    }
    public static void SetProjectName(string name)
    {
        Instance.projectName = name;
    }
    public static string GetVoiceFolder()
    {
        return Path.Combine(Definition.VOICE_FOLDER, Instance.projectName);
    }
}
