using System;
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
        private string _androidAdapterVersion;
        private string _androidVersion;
        private string _iosAdapterVersion;
        private string _iosVersion;
        
        private static PluginImporter _debugAdapterImporter;
        private static PluginImporter _debugPluginImporter;
        private static PluginImporter _releaseAdapterImporter;
        private static PluginImporter _releasePluginImporter;
        
        [MenuItem("Window/Nefta/Inspect", false, 200)]
        public static void ShowWindow()
        {
            GetWindow(typeof(NeftaWindow), false, "Nefta");
        }
        
        public void OnEnable()
        {
            TryGetPluginImporters();

            if (_debugPluginImporter != null)
            {
                _isDebugLib = _debugPluginImporter.GetCompatibleWithPlatform(BuildTarget.Android);
            }
            
            _error = null;
#if UNITY_2021_1_OR_NEWER
            GetAndroidVersions();
#endif
            GetIosVersions();
        }

        private void OnGUI()
        {
            if (_error != null)
            {
                EditorGUILayout.LabelField(_error, EditorStyles.helpBox);
                return;
            }
            
#if UNITY_2021_1_OR_NEWER
            if (_androidAdapterVersion != _iosAdapterVersion)
            {
                DrawVersion("Nefta MAX Android Custom Adapter version", _androidAdapterVersion);
                DrawVersion("Nefta SDK Android version", _androidVersion);
                EditorGUILayout.Space(5);
                DrawVersion("Nefta MAX iOS Custom Adapter version", _iosAdapterVersion);
                DrawVersion("Nefta SDK iOS version", _iosVersion);
            }
            else
#endif
            {
                DrawVersion("Nefta MAX Custom Adapter version", _androidAdapterVersion);
                DrawVersion("Nefta SDK version", _androidVersion);
            }
            EditorGUILayout.Space(5);
            
            if (_debugPluginImporter == null || _releasePluginImporter == null ||
                _debugAdapterImporter == null || _releaseAdapterImporter == null)
            {
                EditorGUILayout.HelpBox("This getting Android SDKs", MessageType.Error);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Android Debug libs:");
                var isLoggingEnabled = EditorGUILayout.Toggle(_isDebugLib);
                EditorGUILayout.EndHorizontal();
                if (isLoggingEnabled != _isDebugLib)
                {
                    _isDebugLib = isLoggingEnabled;
                    TogglePlugins(_isDebugLib);
                }
            }
        }
        
        [MenuItem("Window/Nefta/Export Nefta Custom Adapter SDK", false, int.MaxValue)]
        private static void ExportAdSdkPackage()
        {
            var packageName = $"NeftaMAX_SDK_{Application.version}.unitypackage";
            var assetPaths = new string[] { "Assets/NeftaCustomAdapter" };
            
            TryGetPluginImporters();
            TogglePlugins(false);
            
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
        
        public static void TryGetPluginImporters()
        {
            var guid = AssetDatabase.FindAssets("NeftaMaxAdapter-debug")[0];
            var path = AssetDatabase.GUIDToAssetPath(guid);
            _debugAdapterImporter = (PluginImporter) AssetImporter.GetAtPath(path);

            guid = AssetDatabase.FindAssets("NeftaMaxAdapter-release")[0];
            path = AssetDatabase.GUIDToAssetPath(guid);
            _releaseAdapterImporter = (PluginImporter) AssetImporter.GetAtPath(path);
            
            guid = AssetDatabase.FindAssets("NeftaPlugin-debug")[0];
            path = AssetDatabase.GUIDToAssetPath(guid);
            _debugPluginImporter = (PluginImporter) AssetImporter.GetAtPath(path);

            guid = AssetDatabase.FindAssets("NeftaPlugin-release")[0];
            path = AssetDatabase.GUIDToAssetPath(guid);
            _releasePluginImporter = (PluginImporter) AssetImporter.GetAtPath(path);
        }

        public static void TogglePlugins(bool enable)
        {
            _debugAdapterImporter.SetCompatibleWithPlatform(BuildTarget.Android, enable);
            _debugAdapterImporter.SaveAndReimport();
            
            _releaseAdapterImporter.SetCompatibleWithPlatform(BuildTarget.Android, !enable);
            _releaseAdapterImporter.SaveAndReimport();
            
            _debugPluginImporter.SetCompatibleWithPlatform(BuildTarget.Android, enable);
            _debugPluginImporter.SaveAndReimport();
                    
            _releasePluginImporter.SetCompatibleWithPlatform(BuildTarget.Android, !enable);
            _releasePluginImporter.SaveAndReimport();
        }
        
        private static void DrawVersion(string label, string version)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label); 
            EditorGUILayout.LabelField(version, EditorStyles.boldLabel, GUILayout.Width(60)); 
            EditorGUILayout.EndHorizontal();
        }
        
#if UNITY_2021_1_OR_NEWER
        private void GetAndroidVersions()
        {
            _androidAdapterVersion = GetAarVersion("NeftaMaxAdapter");
            _androidVersion = GetAarVersion("NeftaPlugin-");
        }

        private string GetAarVersion(string aarName)
        {
            var guids = AssetDatabase.FindAssets(aarName);
            if (guids.Length == 0)
            {
                _error = $"{aarName} AARs not found in project";
                return null;
            }
            if (guids.Length > 2)
            {
                _error = $"Multiple instances of {aarName} AARs found in project";
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
        
        private void GetIosVersions()
        {
            var guids = AssetDatabase.FindAssets("ALNeftaMediationAdapter");
            if (guids.Length == 0)
            {
                _error = "ALNeftaMediationAdapter not found in project";
                return;
            }
            if (guids.Length > 3)
            {
                _error = "Multiple instances of ALNeftaMediationAdapter found in project";
                return;
            }

            String wrapperPath = null;
            for (var i = 0; i < guids.Length; i++) {
                wrapperPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (wrapperPath.EndsWith(".m"))
                {
                    break;
                }
            }
            using StreamReader reader = new StreamReader(wrapperPath);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("return @\""))
                {
                    var start = line.IndexOf('"') + 1;
                    var end = line.LastIndexOf('"');
                    _iosAdapterVersion = line.Substring(start, end - start);
                    break;
                }
            }
            
            var pluginPath = Path.GetDirectoryName(wrapperPath);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pluginPath + "/NeftaSDK.xcframework/Info.plist");
            var dict = xmlDoc.ChildNodes[2].ChildNodes[0];
            for (var i = 0; i < dict.ChildNodes.Count; i++)
            {
                if (dict.ChildNodes[i].InnerText == "Version")
                {
                    _iosVersion = dict.ChildNodes[i + 1].InnerText;
                    break;
                }
            }
        }
    }
}