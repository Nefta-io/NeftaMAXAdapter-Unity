#if UNITY_EDITOR
using Nefta.Editor;
#elif UNITY_IOS
using System.Runtime.InteropServices;
using AOT;
#endif
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Nefta.Core.Events;
using UnityEngine;

namespace NeftaCustomAdapter
{
    public class NeftaAdapterEvents
    {
        public delegate void OnBehaviourInsightCallback(Dictionary<string, Insight> insights);

        public enum AdType
        {
            Other = 0,
            Banner = 1,
            Interstitial = 2,
            Rewarded = 3
        }
        
        public enum ContentRating
        {
            Unspecified = 0,
            General = 1,
            ParentalGuidance = 2,
            Teen = 3,
            MatureAudience = 4
        }
        
        private class InsightRequest
        {
            public int _id;
            public IEnumerable<string> _insights;
            public SynchronizationContext _returnContext;
            public OnBehaviourInsightCallback _callback;

            public InsightRequest(OnBehaviourInsightCallback callback)
            {
                _id = _insightId;
                _insightId++;
                _returnContext = SynchronizationContext.Current;
                _callback = callback;
            }
        }
        
#if UNITY_EDITOR
        private static NeftaPlugin _plugin;
#elif UNITY_IOS
        private delegate void OnBehaviourInsightDelegate(int requestId, string behaviourInsight);

        [MonoPInvokeCallback(typeof(OnBehaviourInsightDelegate))] 
        private static void OnBehaviourInsight(int requestId, string behaviourInsight) {
            IOnBehaviourInsight(requestId, behaviourInsight);
        }

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_EnableLogging(bool enable);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Init(string appId, OnBehaviourInsightDelegate onBehaviourInsight);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Record(int type, int category, int subCategory, string nameValue, long value, string customPayload);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationRequest(int adType, string recommendedAdUnitId, double requestedFloorPrice, double calculatedFloorPrice, string adUnitId, double revenue, string precision, int status);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationImpressionAsString(string network, string format, string creativeId, string data);

        [DllImport ("__Internal")]
        private static extern string NeftaPlugin_GetNuid(bool present);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_SetContentRating(string rating);
        
        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_GetBehaviourInsight(int requestId, string insights);
        
        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_SetOverride(string root);
#elif UNITY_ANDROID
        private static AndroidJavaClass _neftaPluginClass;
        private static AndroidJavaClass NeftaPluginClass {
            get
            {
                if (_neftaPluginClass == null)
                {
                    _neftaPluginClass = new AndroidJavaClass("com.nefta.sdk.NeftaPlugin");
                }
                return _neftaPluginClass;
            }
        }
        private static AndroidJavaObject _plugin;
        private static AndroidJavaClass _adapter;
#endif

        private static List<InsightRequest> _insightRequests;
        private static int _insightId;
        
        public static OnBehaviourInsightCallback BehaviourInsightCallback;
        
        public static void EnableLogging(bool enable)
        {
#if UNITY_EDITOR
            NeftaPlugin.EnableLogging(enable);
#elif UNITY_IOS
            NeftaPlugin_EnableLogging(enable);
#elif UNITY_ANDROID
            NeftaPluginClass.CallStatic("EnableLogging", enable);
#endif
        }
        
        public static void Init(string appId, bool sendAdEvents=true)
        {
#if UNITY_EDITOR
            var pluginGameObject = new GameObject("_NeftaPlugin");
            UnityEngine.Object.DontDestroyOnLoad(pluginGameObject);
            _plugin = NeftaPlugin.Init(pluginGameObject, appId);
            _plugin._adapterListener = new NeftaAdapterListener();
#elif UNITY_IOS
            NeftaPlugin_Init(appId, OnBehaviourInsight);
#elif UNITY_ANDROID
            AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");

            _plugin = NeftaPluginClass.CallStatic<AndroidJavaObject>("Init", unityActivity, appId, new NeftaAdapterListener());
#endif
            if (sendAdEvents)
            {
                MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnExternalMediationImpression;
                MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnExternalMediationImpression;
                MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnExternalMediationImpression;
            }

            _insightRequests = new List<InsightRequest>();
        }

        public static void Record(GameEvent gameEvent)
        {
            var type = gameEvent._eventType;
            var category = gameEvent._category;
            var subCategory = gameEvent._subCategory;
            var name = gameEvent._name;
            if (name != null)
            {
                name = JavaScriptStringEncode(gameEvent._name);
            }
            var value = gameEvent._value;
            var customPayload = gameEvent._customString;
            if (customPayload != null)
            {
                customPayload = JavaScriptStringEncode(gameEvent._customString);
            }
            
            Record(type, category, subCategory, name, value, customPayload);
        }
        
        internal static void Record(int type, int category, int subCategory, string name, long value, string customPayload)
        {
#if UNITY_EDITOR
            _plugin.Record(type, category, subCategory, name, value, customPayload);
#elif UNITY_IOS
            NeftaPlugin_Record(type, category, subCategory, name, value, customPayload);
#elif UNITY_ANDROID
            _plugin.Call("Record", type, category, subCategory, name, value, customPayload);
#endif
        }
        
        public static void OnExternalMediationRequestLoaded(AdType adType, string recommendedAdUnitId, double calculatedFloorPrice, MaxSdkBase.AdInfo adInfo)
        {
            OnExternalMediationRequest((int) adType, recommendedAdUnitId, -1, calculatedFloorPrice, adInfo.AdUnitIdentifier, adInfo.Revenue, adInfo.RevenuePrecision, 1);
        }

        public static void OnExternalMediationRequestFailed(AdType adType, string recommendedAdUnitId, double calculatedFloorPrice, string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            OnExternalMediationRequest((int) adType, recommendedAdUnitId, -1, calculatedFloorPrice, adUnitId, -1, null, errorInfo.Code == MaxSdkBase.ErrorCode.NoFill ? 2 : 0);
        }
        
        /// <summary>
        /// Should be called when MAX loads any ad (MaxSdkCallbacks.[AdType].OnAdLoadedEvent
        /// </summary>
        /// <param name="adType"></param>
        /// <param name="requestedFloorPrice">The price of </param>
        /// <param name="calculatedFloorPrice"></param>
        /// <param name="adInfo"></param>
        public static void OnExternalMediationRequestLoaded(AdType adType, double requestedFloorPrice, double calculatedFloorPrice, MaxSdkBase.AdInfo adInfo)
        {
            OnExternalMediationRequest((int) adType, null, requestedFloorPrice, calculatedFloorPrice, adInfo.AdUnitIdentifier, adInfo.Revenue, adInfo.RevenuePrecision, 1);
        }

        public static void OnExternalMediationRequestFailed(AdType adType, double requestedFloorPrice, double calculatedFloorPrice, string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            OnExternalMediationRequest((int) adType, null, requestedFloorPrice, calculatedFloorPrice, adUnitId, -1, null, errorInfo.Code == MaxSdkBase.ErrorCode.NoFill ? 2 : 0);
        }

        private static void OnExternalMediationRequest(int adType, string recommendedAdUnitId, double requestedFloorPrice, double calculatedFloorPrice, string adUnitId, double revenue, string precision, int status)
        {
#if UNITY_EDITOR
            _plugin.OnExternalMediationRequest("applovin-max", adType, recommendedAdUnitId, requestedFloorPrice, calculatedFloorPrice, adUnitId, revenue, precision, status);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationRequest(adType, recommendedAdUnitId, requestedFloorPrice, calculatedFloorPrice, adUnitId, revenue, precision, status);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationRequest", "applovin-max", adType, recommendedAdUnitId, requestedFloorPrice, calculatedFloorPrice, adUnitId, revenue, precision, status);
#endif
        }

        private static void OnExternalMediationImpression(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adInfo == null)
            {
                return;
            }
            
            var network = adInfo.NetworkName;
            var format = adInfo.AdFormat;
            var creativeId = adInfo.CreativeIdentifier;
            var sb = new StringBuilder();
            sb.Append("{\"mediation_provider\":\"applovin-max\",\"ad_unit_id\":\"");
            sb.Append(adUnitId);
            sb.Append("\",\"revenue_precision\":\"");
            sb.Append(adInfo.RevenuePrecision);
            sb.Append("\",\"placement_name\":\"");
            sb.Append(adInfo.Placement);
            sb.Append("\",\"request_latency\":");
            sb.Append(adInfo.LatencyMillis);
            sb.Append(",\"dsp_name\":\"");
            sb.Append(JavaScriptStringEncode(adInfo.DspName));
            if (adInfo.WaterfallInfo != null)
            {
                sb.Append("\",\"waterfall_name\":\"");
                sb.Append(JavaScriptStringEncode(adInfo.WaterfallInfo.Name));
                var responses = adInfo.WaterfallInfo.NetworkResponses;
                if (responses != null && responses.Count > 0)
                {
                    var isEmpty = true;
                    sb.Append("\",\"waterfall\":[\"");
                    foreach (var response in responses)
                    {
                        if (!isEmpty)
                        {
                            sb.Append("\",\"");
                        }
                        var networkName = response.MediatedNetwork.Name;
                        if (String.IsNullOrEmpty(networkName) && response.Credentials != null &&
                            response.Credentials.TryGetValue("network_name", out var name))
                        {
                            if (name is string nameString)
                            {
                                networkName = nameString;
                            }
                        }
                        
                        if (!String.IsNullOrEmpty(networkName))
                        {
                            sb.Append(networkName);
                            isEmpty = false;
                        }
                    }
                    sb.Append("\"]");
                }
                else
                {
                    sb.Append("\"");
                }
                sb.Append(",\"waterfall_test_name\":\"");
                sb.Append(JavaScriptStringEncode(adInfo.WaterfallInfo.TestName));

            }
            sb.Append("\",\"revenue\":");
            sb.Append(adInfo.Revenue.ToString(CultureInfo.InvariantCulture));
            var data = sb.ToString();
#if UNITY_EDITOR
            _plugin.OnExternalMediationImpression(data);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationImpressionAsString(network, format, creativeId, data);
#elif UNITY_ANDROID
            if (_adapter == null) {
                _adapter = new AndroidJavaClass("com.applovin.mediation.adapters.NeftaMediationAdapter");
            }
            _adapter.CallStatic("OnExternalMediationImpressionAsString", network, format, creativeId, data);
#endif
        }
        
        public static void GetBehaviourInsight(string[] insightList, OnBehaviourInsightCallback callback=null)
        {
            var request = new InsightRequest(callback ?? BehaviourInsightCallback);
            _insightRequests.Add(request);
            
            StringBuilder sb = new StringBuilder();
            bool isFirst = true;
            foreach (var insight in insightList)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    sb.Append(",");
                }
                sb.Append(insight);
            }
            var insights = sb.ToString();
#if UNITY_EDITOR
            _plugin.GetBehaviourInsight(request._id, insights);
#elif UNITY_IOS
            NeftaPlugin_GetBehaviourInsight(request._id, insights);
#elif UNITY_ANDROID
            _plugin.Call("GetBehaviourInsightBridge", request._id, insights);
#endif
        }
        
        public static string GetNuid(bool present)
        {
            string nuid = null;
#if UNITY_EDITOR
            nuid = _plugin.GetNuid(present);
#elif UNITY_IOS
            nuid = NeftaPlugin_GetNuid(present);
#elif UNITY_ANDROID
            nuid = _plugin.Call<string>("GetNuid", present);
#endif
            return nuid;
        }
        
        public static void SetContentRating(ContentRating rating)
        {
            var r = "";
            switch (rating)
            {
                case ContentRating.General:
                    r = "G";
                    break;
                case ContentRating.ParentalGuidance:
                    r = "PG";
                    break;
                case ContentRating.Teen:
                    r = "T";
                    break;
                case ContentRating.MatureAudience:
                    r = "MA";
                    break;
            }
#if UNITY_EDITOR
            _plugin.SetContentRating(r);
#elif UNITY_IOS
            NeftaPlugin_SetContentRating(r);
#elif UNITY_ANDROID
            _plugin.Call("SetContentRating", r);
#endif
        }
        
        public static void SetOverride(string root) 
        {
#if UNITY_EDITOR
            _plugin.Override(root);
#elif UNITY_IOS
            NeftaPlugin_SetOverride(root);
#elif UNITY_ANDROID
            _neftaPluginClass.CallStatic("SetOverride", root);
#endif
        }
        
        internal static void IOnBehaviourInsight(int id, string bi)
        {
            var behaviourInsight = new Dictionary<string, Insight>();
            if (bi != null)
            {
                try
                {
                    var start = bi.IndexOf("s\":", StringComparison.InvariantCulture) + 5;

                    while (start != -1 && start < bi.Length)
                    {
                        var end = bi.IndexOf("\":{", start, StringComparison.InvariantCulture);
                        var key = bi.Substring(start, end - start);
                        long intVal = 0;
                        double floatVal = 0;
                        string stringVal = null;

                        start = end + 4;
                        for (var f = 0; f < 4; f++)
                        {
                            if (bi[start] == 'f')
                            {
                                start += 11;
                                end = start + 1;
                                for (; end < bi.Length; end++)
                                {
                                    if (bi[end] == ',' || bi[end] == '}')
                                    {
                                        break;
                                    }
                                }

                                var doubleString = bi.Substring(start, end - start);
                                floatVal = Double.Parse(doubleString, NumberStyles.Float, CultureInfo.InvariantCulture);
                            }
                            else if (bi[start] == 'i')
                            {
                                start += 9;
                                end = start + 1;
                                for (; end < bi.Length; end++)
                                {
                                    if (bi[end] == ',' || bi[end] == '}')
                                    {
                                        break;
                                    }
                                }

                                var intString = bi.Substring(start, end - start);
                                intVal = long.Parse(intString, NumberStyles.Number, CultureInfo.InvariantCulture);
                            }
                            else if (bi[start] == 's' && bi[start + 2] == 'r')
                            {
                                start += 13;
                                end = bi.IndexOf("\"", start, StringComparison.InvariantCulture);
                                stringVal = bi.Substring(start, end - start);
                                end++;
                            }

                            if (bi[end] == '}')
                            {
                                break;
                            }

                            start = end + 2;
                        }

                        behaviourInsight[key] = new Insight(intVal, floatVal, stringVal);

                        if (bi[end + 1] == '}')
                        {
                            break;
                        }

                        start = end + 3;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            try
            {
                InsightRequest request = null;
                foreach (var iR in _insightRequests)
                {
                    if (iR._id == id)
                    {
                        request = iR;
                        break;
                    }   
                }
                if (request == null)
                {
                    return;
                }
                
                request._returnContext.Post(_ => request._callback(behaviourInsight), null);

                for (var i = _insightRequests.Count - 1; i >= 0; i--)
                {
                    if (_insightRequests[i]._id == id)
                    {
                        _insightRequests.RemoveAt(i);
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
        
        internal static string JavaScriptStringEncode(string value)
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
    }
}