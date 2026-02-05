using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using UnityEngine;

public class BuildPostprocessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;
    
    public void OnPostprocessBuild(BuildReport report)
    {
        string buildOutputPath = report.summary.outputPath;
        string buildFolder = Path.GetDirectoryName(buildOutputPath);
        
        // ラインセンスファイルをコピー
        string sourcePath = Path.Combine(Application.dataPath, "..", "LICENSES.md");
        string destPath = Path.Combine(buildFolder, "LICENSES.md");
        
        if (File.Exists(sourcePath))
        {
            File.Copy(sourcePath, destPath, true);
            Debug.Log($"ライセンスファイルをコピーしました: {destPath}");
        }
        else
        {
            Debug.LogWarning($"ライセンスファイルが見つかりません: {sourcePath}");
        }
    }
}