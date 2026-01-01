using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class BuildScript
{
    [MenuItem("Build/Build macOS")]
    public static void BuildMacOS()
    {
        string[] scenes = { "Assets/Scenes/Boot.unity", "Assets/Scenes/MythicPlayfield.unity" };
        string buildPath = "mythic.app";
        
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };
        
        BuildPipeline.BuildPlayer(options);
        Debug.Log("Build completed!");
    }
}
