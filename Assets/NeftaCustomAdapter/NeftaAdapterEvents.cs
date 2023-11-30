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
#elif UNITY_IOS
        [DllImport ("__Internal")]
        private static extern IntPtr NeftaPlugin_Init(string appId);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Record(IntPtr instance, string recordedEvent);

        private static IntPtr _plugin;
#elif UNITY_ANDROID
        private static AndroidJavaObject _plugin;
#endif
        private static StringBuilder _eventBuilder;
        
        public static void Init(string appId)
        {
            _eventBuilder = new StringBuilder(128);
#if UNITY_EDITOR
            _plugin = true;
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
            var recordedEvent = gameEvent.GetRecordedEvent();
            _eventBuilder.Clear();
            _eventBuilder.Append("{");
            _eventBuilder.Append("\"event_type\":\"");
            _eventBuilder.Append(recordedEvent._type);
            _eventBuilder.Append("\",\"event_category\":\"");
            _eventBuilder.Append(recordedEvent._category);
            _eventBuilder.Append("\",\"value\":");
            _eventBuilder.Append(recordedEvent._value.ToString());
            _eventBuilder.Append(",\"event_sub_category\":\"");
            _eventBuilder.Append(recordedEvent._subCategory);
            if (recordedEvent._itemName != null)
            {
                _eventBuilder.Append("\",\"item_name\":\"");
                _eventBuilder.Append(recordedEvent._itemName);
            }
            if (recordedEvent._customPayload != null)
            {
                _eventBuilder.Append("\",\"custom_publisher_payload\":\"");
                _eventBuilder.Append(recordedEvent._customPayload);
            }
            _eventBuilder.Append("\"}");
            var eventString = _eventBuilder.ToString();
#if UNITY_EDITOR
            Assert.IsTrue(_plugin, "Before recording game event Init should be called");
            Debug.Log($"Recording {eventString}");
#elif UNITY_IOS
            NeftaPlugin_Record(_plugin, eventString);
#elif UNITY_ANDROID
            _plugin.Call("Record", eventString);
#endif
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