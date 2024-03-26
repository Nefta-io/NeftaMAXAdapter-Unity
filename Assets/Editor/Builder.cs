using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Editor
{
    public class Builder
    {
        private static void Build(BuildTarget target, string outPath)
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/Scenes/HomeScene.unity"
                },
                locationPathName = outPath,
                target = target,
                options = BuildOptions.Development | BuildOptions.StrictMode
            };
            
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            
            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build successful");
            }
            else if (report.summary.result == BuildResult.Failed)
            {
                Debug.LogError($"Build failed");
            }
        }

        public static void BuildAndroid()
        {
            Build(BuildTarget.Android, "out_Android.apk");
        }
        
        public static void Buildios()
        {
            Build(BuildTarget.iOS, "out_iOS");
        }
    }
}