using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NeftaCustomAdapter.Editor
{
    public class NeftaMAXPostProcessor : MonoBehaviour
    {
        private const string PostInstallMAXLinkage = @"
post_install do |installer|
  installer.pods_project.targets.each do |target|
    if target.name == 'NeftaMAXAdapter'
      target.build_configurations.each do |config|
        config.build_settings['MACH_O_TYPE'] = 'staticlib'
      end
      framework_ref = installer.pods_project.reference_for_path(File.dirname(__FILE__) + '/Pods/AppLovinSDK/applovin-ios-sdk-12.1.0/AppLovinSDK.xcframework')
      target.frameworks_build_phase.add_file_reference(framework_ref, true)
    end
  end
end";
        
        [PostProcessBuild(45)]
        private static void PostProcessBuild(BuildTarget target, string buildPath)
        {
            if (target == BuildTarget.iOS)
            {
                var neftaSDKDependencyInRoot = false;
                var configuration = GetNeftaConfiguration();
                if (configuration != null)
                {
                    neftaSDKDependencyInRoot = configuration._neftaSDKDependencyInRoot;
                }
                
                AddNeftaPodDependency(buildPath, neftaSDKDependencyInRoot);
                
                if (neftaSDKDependencyInRoot)
                {
                    AddNeftaXcodeDependency(buildPath);
                }
            }
        }

        private static void AddNeftaPodDependency(string buildPath, bool neftaSDKDependencyInRoot)
        {
            string dependency = "pod 'NeftaMAXAdapter', :git => 'https://github.com/Nefta-io/NeftaMAXAdapter.git', :tag => '1.0.8'";
            if (neftaSDKDependencyInRoot)
            {
                dependency += "\n  pod 'NeftaSDK'";
            }
                
            var path = buildPath + "/Podfile";
            var text = File.ReadAllText(path);
            var podIndex = text.IndexOf("pod 'NeftaMAXAdapter'", StringComparison.InvariantCulture);
            if (podIndex >= 0)
            {
                var dependencyEnd = text.IndexOf('\n', podIndex);
                text = text.Substring(0, podIndex) + dependency + text.Substring(dependencyEnd);
            }
            else
            {
                podIndex = text.IndexOf("pod 'AppLovinSDK'", StringComparison.InvariantCulture);
                var index = text.IndexOf('\n', podIndex);
                text = text.Insert(index + 1, $"  {dependency}\n");
            }

            if (!neftaSDKDependencyInRoot)
            {
                var iphoneTargetIndex = text.IndexOf("target 'Unity-iPhone' do", StringComparison.InvariantCulture);
                if (iphoneTargetIndex < 0)
                {
                    text += "\ntarget 'Unity-iPhone' do\n  pod 'NeftaSDK'\nend\n";
                }
                else
                {
                    var index = text.IndexOf('\n', iphoneTargetIndex);
                    text = text.Insert(index + 1, "  pod 'NeftaSDK'\n");
                }
            }

            text += PostInstallMAXLinkage;
            File.WriteAllText(path, text);
        }

        private static void AddNeftaXcodeDependency(string buildPath)
        {
            var scriptName = "NeftaDependencyInstaller";
            var scriptGuid = AssetDatabase.FindAssets(scriptName)[0];
            var scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuid);

            File.Copy(scriptPath, buildPath + $"/{scriptName}.rb", true);

            var startInfo = new ProcessStartInfo {
                FileName = "ruby",
                Arguments = $"{scriptName}.rb",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = buildPath
            };

            using Process process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit();
            var exitCode = process.ExitCode;
            if (exitCode == 0)
            {
                Debug.Log("NeftaSDK dependency installed successfully");
            }
            else
            {
                Debug.LogWarning($"Error installing NeftaSDK dependency: {process.StandardOutput}, {process.StandardError}");
            }
        }
        
        private static NeftaConfiguration GetNeftaConfiguration()
        {
            NeftaConfiguration configuration = null;
            
            string[] guids = AssetDatabase.FindAssets("t:NeftaConfiguration");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                configuration = AssetDatabase.LoadAssetAtPath<NeftaConfiguration>(path);
            }

            return configuration;
        }
        
        [MenuItem("AppLovin/Select Nefta Configuration", false, int.MaxValue)]
        private static void SelectNeftaConfiguration()
        {
            var configuration = GetNeftaConfiguration();
            if (configuration == null)
            {
                const string scriptName = "NeftaMAXPostProcessor";
                string[] scriptGuid = AssetDatabase.FindAssets(scriptName);
                var scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuid[0]);
                
                configuration = ScriptableObject.CreateInstance<NeftaConfiguration>();
                AssetDatabase.CreateAsset(configuration, scriptPath.Replace(scriptName + ".cs", "NeftaConfiguration.asset"));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            Selection.objects = new UnityEngine.Object[] { configuration };
        }
        
        public static PluginImporter GetImporter(bool debug)
        {
            var guid = AssetDatabase.FindAssets(debug ? "NeftaMaxAdapter-debug" : "NeftaMaxAdapter-release")[0];
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return (PluginImporter) AssetImporter.GetAtPath(path);
        }
        
        [MenuItem("AppLovin/Export Nefta Custom Adapter SDK", false, int.MaxValue)]
        private static void ExportAdSdkPackage()
        {
            var packageName = $"NeftaMAX_SDK_{Application.version}.unitypackage";
            var assetPaths = new string[] { "Assets/NeftaCustomAdapter" };
            
            var importer = GetImporter(true);
            importer.SetCompatibleWithPlatform(BuildTarget.Android, true);
            importer.SaveAndReimport();
            
            importer = GetImporter(false);
            importer.SetCompatibleWithPlatform(BuildTarget.Android, false);
            importer.SaveAndReimport();
            
            try
            {
                AssetDatabase.ExportPackage(assetPaths, packageName, ExportPackageOptions.Recurse);
                Debug.Log($"Finished exporting {packageName}");   
            }
            catch (Exception e)
            {
                Debug.LogError($"Error exporting {packageName}: {e.Message}");   
            }
        }
    }
}