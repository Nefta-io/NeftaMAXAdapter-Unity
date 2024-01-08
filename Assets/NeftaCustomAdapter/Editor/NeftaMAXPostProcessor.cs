using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace NeftaCustomAdapter.Editor
{
    public class NeftaMAXPostProcessor : MonoBehaviour
    {
        private const string PostInstallMAXLinkage = @"
post_install do |installer|
  installer.pods_project.targets.each do |target|
    if target.name == 'NeftaMAXAdapter'
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
                const string dependency = "pod 'NeftaMAXAdapter', :git => 'https://github.com/Nefta-io/NeftaMAXAdapter.git', :tag => '1.0.7'";
                
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

                text += PostInstallMAXLinkage;
                File.WriteAllText(path, text);
            }
        }
        
        [MenuItem("AppLovin/Export Nefta Custom Adapter SDK", false, int.MaxValue)]
        private static void ExportAdSdkPackage()
        {
            var packageName = $"NeftaMAX_SDK_{Application.version}.unitypackage";
            var assetPaths = new string[] { "Assets/NeftaCustomAdapter" };

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