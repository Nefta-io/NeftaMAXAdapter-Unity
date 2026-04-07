using System.IO;
using System.IO.Compression;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace NeftaCustomAdapter.Editor
{
    public class NeftaWindow : EditorWindow
    {
        private bool _isDebugLib;
        
        private string _error;
        private string _androidVersion;
        private string _iosVersion;
        
        [MenuItem("Window/Nefta/Inspect", false, 200)]
        public static void ShowWindow()
        {
            GetWindow(typeof(NeftaWindow), false, "Nefta");
        }
        
        public void OnEnable()
        {
            _error = null;
#if UNITY_2021_1_OR_NEWER
            _androidVersion = GetAndroidVersions();
#endif
            _iosVersion = GetIosVersions();
        }

        private void OnGUI()
        {
            if (_error != null)
            {
                EditorGUILayout.LabelField(_error, EditorStyles.helpBox);
                return;
            }
            
#if UNITY_2021_1_OR_NEWER
            if (_androidVersion != _iosVersion)
            {
                DrawVersion("Nefta SDK Android version", _androidVersion);
                EditorGUILayout.Space(5);
                DrawVersion("Nefta SDK iOS version", _iosVersion);
            }
            else
#endif
            {
                DrawVersion("Nefta SDK version", _androidVersion);
            }
            EditorGUILayout.Space(5);
        }

        [MenuItem("Window/Nefta/Delete nuid", false, 300)]
        private static void DeleteNuid()
        {
            PlayerPrefs.DeleteKey("nefta.core.user_id");
            PlayerPrefs.Save();
            Debug.Log("Deleted nuid");
        }
        
        private static void DrawVersion(string label, string version)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label); 
            EditorGUILayout.LabelField(version, EditorStyles.boldLabel, GUILayout.Width(60)); 
            EditorGUILayout.EndHorizontal();
        }
        
#if UNITY_2021_1_OR_NEWER
        private string GetAndroidVersions()
        {
            var aarName = "NeftaPlugin-";
            var guids = AssetDatabase.FindAssets(aarName);
            if (guids.Length == 0)
            {
                _error = $"{aarName} AARs not found in project";
                return null;
            }
            var aarPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            using ZipArchive aar = ZipFile.OpenRead(aarPath);
            ZipArchiveEntry manifestEntry = aar.GetEntry("AndroidManifest.xml");
            if (manifestEntry == null)
            {
                _error = "Nefta SDK AAR seems to be corrupted";
                return null;
            }
            using Stream manifestStream = manifestEntry.Open();
            XmlDocument manifest = new XmlDocument();
            manifest.Load(manifestStream);
            var root = manifest.DocumentElement;
            if (root == null)
            {
                _error = "Nefta SDK AAR seems to be corrupted";
                return null;
            }
            return root.Attributes["android:versionName"].Value;
        }
#endif
        
        private string GetIosVersions()
        {
            var guids = AssetDatabase.FindAssets("NeftaAdapter t:script");
            if (guids.Length == 0)
            {
                _error = "NeftaAdapter.m not found in project";
                return null;
            }
            var wrapperPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var pluginPath = Path.GetDirectoryName(wrapperPath);
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(pluginPath + "/Plugins/iOS/NeftaSDK.xcframework/Info.plist");
            var dict = xmlDoc.ChildNodes[2].ChildNodes[0];
            for (var i = 0; i < dict.ChildNodes.Count; i++)
            {
                if (dict.ChildNodes[i].InnerText == "Version")
                {
                    return dict.ChildNodes[i + 1].InnerText;
                }
            }
            return null;
        }
    }
}