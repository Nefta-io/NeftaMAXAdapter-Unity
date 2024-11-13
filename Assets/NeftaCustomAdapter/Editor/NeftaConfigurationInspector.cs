using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace NeftaCustomAdapter.Editor
{
    [CustomEditor(typeof(NeftaConfiguration), false)]
    public class NeftaConfigurationInspector : UnityEditor.Editor
    {
        private NeftaConfiguration _configuration;
        private bool _isLoggingEnabled;

        private string _error;
        private string _androidAdapterVersion;
        private string _androidVersion;
        private string _iosAdapterVersion;
        private string _iosVersion;
        
        private static PluginImporter GetImporter(bool debug)
        {
            var guid = AssetDatabase.FindAssets(debug ? "NeftaMaxAdapter-debug" : "NeftaMaxAdapter-release")[0];
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return (PluginImporter) AssetImporter.GetAtPath(path);
        }
        
#if UNITY_IOS
        [PostProcessBuild(0)]
        public static void NeftaPostProcessPlist(BuildTarget buildTarget, string path)
        {
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new UnityEditor.iOS.Xcode.PlistDocument();
            plist.ReadFromFile(plistPath);

            plist.root.values.TryGetValue("SKAdNetworkItems", out var skAdNetworkItems);
            var existingSkAdNetworkIds = new HashSet<string>();

            if (skAdNetworkItems != null && skAdNetworkItems.GetType() == typeof(UnityEditor.iOS.Xcode.PlistElementArray))
            {
                var plistElementDictionaries = skAdNetworkItems.AsArray().values.Where(plistElement => plistElement.GetType() == typeof(UnityEditor.iOS.Xcode.PlistElementDict));
                foreach (var plistElement in plistElementDictionaries)
                {
                    UnityEditor.iOS.Xcode.PlistElement existingId;
                    plistElement.AsDict().values.TryGetValue("SKAdNetworkIdentifier", out existingId);
                    if (existingId == null || existingId.GetType() != typeof(UnityEditor.iOS.Xcode.PlistElementString) || string.IsNullOrEmpty(existingId.AsString())) continue;

                    existingSkAdNetworkIds.Add(existingId.AsString());
                }
            }
            else
            {
                skAdNetworkItems = plist.root.CreateArray("SKAdNetworkItems");
            }

            const string neftaSkAdNetworkId = "2lj985962l.adattributionkit";
            if (!existingSkAdNetworkIds.Contains(neftaSkAdNetworkId)) {
                var skAdNetworkItemDict = skAdNetworkItems.AsArray().AddDict();
                skAdNetworkItemDict.SetString("SKAdNetworkIdentifier", neftaSkAdNetworkId);
            }

            plist.WriteToFile(plistPath);
        }
#endif
        
        public void OnEnable()
        {
            _configuration = (NeftaConfiguration)target;
            
            var importer = GetImporter(true);
            var isDebugPluginEnabled = importer.GetCompatibleWithPlatform(BuildTarget.Android);
            if (isDebugPluginEnabled != _configuration._isLoggingEnabled)
            {
                _configuration._isLoggingEnabled = isDebugPluginEnabled;
                EditorUtility.SetDirty(_configuration);
                AssetDatabase.SaveAssetIfDirty(_configuration);
            }
            
            _error = null;
#if UNITY_2021_1_OR_NEWER
            GetAndroidVersions();
#endif
            GetIosVersions();
        }

        public void OnDisable()
        {
            _configuration = null;
        }
        
        public override void OnInspectorGUI()
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
            
            base.OnInspectorGUI();
            if (_isLoggingEnabled != _configuration._isLoggingEnabled)
            {
                _isLoggingEnabled = _configuration._isLoggingEnabled;
                
                var importer = GetImporter(true);
                importer.SetCompatibleWithPlatform(BuildTarget.Android, _isLoggingEnabled);
                importer.SaveAndReimport();
                
                importer = GetImporter(false);
                importer.SetCompatibleWithPlatform(BuildTarget.Android, !_isLoggingEnabled);
                importer.SaveAndReimport();
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
            EditorUtility.FocusProjectWindow();
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
            var guids = AssetDatabase.FindAssets("NeftaMaxAdapter");
            if (guids.Length == 0)
            {
                _error = "NeftaMaxAdapter AARs not found in project";
                return;
            }
            if (guids.Length > 2)
            {
                _error = "Multiple instances of NeftaMaxAdapter AARs found in project";
                return;
            }

            var aarPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            using ZipArchive aar = ZipFile.OpenRead(aarPath);
            ZipArchiveEntry manifestEntry = aar.GetEntry("AndroidManifest.xml");
            if (manifestEntry == null)
            {
                _error = "Nefta SDK AAR seems to be corrupted";
                return;
            }
            using Stream manifestStream = manifestEntry.Open();
            XmlDocument manifest = new XmlDocument();
            manifest.Load(manifestStream);
            var root = manifest.DocumentElement;
            if (root == null)
            {
                _error = "Nefta SDK AAR seems to be corrupted";
                return;
            }
            _androidAdapterVersion = root.Attributes["android:versionName"].Value;
            var metaNodes = root.SelectNodes("/manifest/application/meta-data");
            foreach (XmlNode metaNode in metaNodes)
            {
                var name = metaNode.Attributes["android:name"];
                if (name.Value == "NeftaSDKVersion")
                {
                    _androidVersion = metaNode.Attributes["android:value"].Value;
                    break;
                }
            }
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
            if (guids.Length > 2)
            {
                _error = "Multiple instances of ALNeftaMediationAdapter found in project";
                return;
            }
            var wrapperPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (wrapperPath.EndsWith(".h"))
            {
                wrapperPath = AssetDatabase.GUIDToAssetPath(guids[1]);
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