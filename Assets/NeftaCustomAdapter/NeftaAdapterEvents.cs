#if !UNITY_EDITOR && UNITY_IOS
using System;
using System.Runtime.InteropServices;
using AOT;
#endif
using System.Text;
using Nefta.Core.Events;
using UnityEngine;
using UnityEngine.Assertions;

namespace NeftaCustomAdapter
{
    public class NeftaAdapterEvents
    {
#if UNITY_EDITOR
        private static bool _plugin;
        private static bool _isLoggingEnabled;
#elif UNITY_IOS
        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_EnableLogging(bool enable);

        [DllImport ("__Internal")]
        private static extern IntPtr NeftaPlugin_Init(string appId);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Record(IntPtr instance, string recordedEvent);

        [DllImport ("__Internal")]
        private static extern string NeftaPlugin_GetNuid(IntPtr instance, bool present);

        private static IntPtr _plugin;
#elif UNITY_ANDROID
        private static AndroidJavaObject _plugin;
#endif
        private static StringBuilder _eventBuilder;
        
        public static void EnableLogging(bool enable)
        {
#if UNITY_EDITOR
            _isLoggingEnabled = enable;
#elif UNITY_IOS
            NeftaPlugin_EnableLogging(enable);
#endif
        }
        
        public static void Init(string appId)
        {
            _eventBuilder = new StringBuilder(128);
#if UNITY_EDITOR
            _plugin = true;
            Debug.Log($"NeftaPlugin Init:{appId}");
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChange;
#elif UNITY_IOS
            _plugin = NeftaPlugin_Init(appId);
#elif UNITY_ANDROID
            AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaClass neftaPluginClass = new AndroidJavaClass("com.nefta.sdk.NeftaPlugin");
            _plugin = neftaPluginClass.CallStatic<AndroidJavaObject>("Init", unityActivity, appId);
#endif
        }

        public static void Record(GameEvent gameEvent)
        {
            _eventBuilder.Clear();
            _eventBuilder.Append("{\"event_type\":\"");
            _eventBuilder.Append(gameEvent._eventType);
            _eventBuilder.Append("\",\"event_category\":\"");
            _eventBuilder.Append(gameEvent._category);
            _eventBuilder.Append("\",\"value\":");
            _eventBuilder.Append(gameEvent._value.ToString());
            _eventBuilder.Append(",\"event_sub_category\":\"");
            _eventBuilder.Append(gameEvent._subCategory);
            if (gameEvent._name != null)
            {
                _eventBuilder.Append("\",\"item_name\":\"");
                _eventBuilder.Append(JavaScriptStringEncode(gameEvent._name));
            }
            if (gameEvent._customString != null)
            {
                _eventBuilder.Append("\",\"custom_publisher_payload\":\"");
                _eventBuilder.Append(JavaScriptStringEncode(gameEvent._customString));
            }
            _eventBuilder.Append("\"}");
            var eventString = _eventBuilder.ToString();
#if UNITY_EDITOR
            Assert.IsTrue(_plugin, "Before recording game event Init should be called");
            if (_isLoggingEnabled)
            {
                Debug.Log($"Recording {eventString}");
            }
#elif UNITY_IOS
            NeftaPlugin_Record(_plugin, eventString);
#elif UNITY_ANDROID
            _plugin.Call("Record", eventString);
#endif
        }
        
        public static string GetNuid(bool present)
        {
            string nuid = null;
#if UNITY_EDITOR
#elif UNITY_IOS
            nuid = NeftaPlugin_GetNuid(_plugin, present);
#elif UNITY_ANDROID
            nuid = _plugin.Call<string>("GetNuid", present);
#endif
            return nuid;
        }
        
        private static string JavaScriptStringEncode(string value)
        {
            int len = value.Length;
            bool needEncode = false;
            char c;
            for (int i = 0; i < len; i++)
            {
                c = value [i];

                if (c >= 0 && c <= 31 || c == 34 || c == 39 || c == 60 || c == 62 || c == 92)
                {
                    needEncode = true;
                    break;
                }
            }

            if (!needEncode)
            {
                return value;
            }
            
            var sb = new StringBuilder ();
            for (int i = 0; i < len; i++)
            {
                c = value [i];
                if (c >= 0 && c <= 7 || c == 11 || c >= 14 && c <= 31 || c == 39 || c == 60 || c == 62)
                {
                    sb.AppendFormat ("\\u{0:x4}", (int)c);
                }
                else switch ((int)c)
                {
                    case 8:
                        sb.Append ("\\b");
                        break;

                    case 9:
                        sb.Append ("\\t");
                        break;

                    case 10:
                        sb.Append ("\\n");
                        break;

                    case 12:
                        sb.Append ("\\f");
                        break;

                    case 13:
                        sb.Append ("\\r");
                        break;

                    case 34:
                        sb.Append ("\\\"");
                        break;

                    case 92:
                        sb.Append ("\\\\");
                        break;

                    default:
                        sb.Append (c);
                        break;
                }
            }
            return sb.ToString ();
        }
        
#if UNITY_EDITOR
        private static void OnPlayModeChange(UnityEditor.PlayModeStateChange playMode)
        {
            if (playMode == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                _plugin = false;
            }
        }
#endif
    }
}