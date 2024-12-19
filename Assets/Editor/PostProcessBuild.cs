#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Editor
{
    public class PostProcessBuild
    {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            var plist = new UnityEditor.iOS.Xcode.PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("NSUserTrackingUsageDescription", "This allows us to deliver personalized ads and content.");
            plist.WriteToFile(plistPath);
            
            string projectPath = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj/project.pbxproj");
            string pbxprojContent = File.ReadAllText(projectPath);
            
            if (pbxprojContent.Contains("SystemCapabilities"))
            {
                int startIndex = pbxprojContent.IndexOf("SystemCapabilities");
                int endIndex = pbxprojContent.IndexOf(";", startIndex) + 1;
                string systemCapabilities = pbxprojContent.Substring(startIndex, endIndex - startIndex);
                systemCapabilities = systemCapabilities.Replace("\"com.apple.InAppPurchase\" = {enabled = 1;};", "");
                pbxprojContent = pbxprojContent.Remove(startIndex, endIndex - startIndex).Insert(startIndex, systemCapabilities);
            }
        
            File.WriteAllText(projectPath, pbxprojContent);
        }
    }
}
#endif