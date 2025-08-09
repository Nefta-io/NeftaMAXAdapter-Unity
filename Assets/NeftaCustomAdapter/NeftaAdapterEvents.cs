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
        public delegate void OnInsightsCallback(Insights insights);
        
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
            public OnInsightsCallback _callback;

            public InsightRequest(int id, OnInsightsCallback callback)
            {
                _id = id;
                _returnContext = SynchronizationContext.Current;
                _callback = callback;
            }
        }

#if UNITY_EDITOR
        private static NeftaPlugin _plugin;
#elif UNITY_IOS
        private delegate void OnInsightsDelegate(int requestId, string insights);

        [MonoPInvokeCallback(typeof(OnInsightsDelegate))] 
        private static void OnInsights(int requestId, string insights) {
            IOnInsights(requestId, insights);
        }

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_EnableLogging(bool enable);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Init(string appId, OnInsightsDelegate onInsights);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Record(int type, int category, int subCategory, string nameValue, long value, string customPayload);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationRequest(string mediationProvider, int adType, string recommendedAdUnitId, double requestedFloorPrice, double calculatedFloorPrice, string adUnitId, double revenue, string precision, int status, string providerStatus, string networkStatus);

        [DllImport ("__Internal")]
        private static extern void NeftaAdapter_OnExternalMediationImpressionAsString(string network, string format, string creativeId, string data, double revenue, string precision);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationImpressionAsString(string mediationProvider, string data, int adType, double revenue, string precision);

        [DllImport ("__Internal")]
        private static extern string NeftaPlugin_GetNuid(bool present);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_SetContentRating(string rating);
        
        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_GetInsights(int requestId, int insights, int timeoutInSeconds);
        
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

        public static void Init(string appId, bool sendAdEvents = true)
        {
#if UNITY_EDITOR
            var pluginGameObject = new GameObject("_NeftaPlugin");
            UnityEngine.Object.DontDestroyOnLoad(pluginGameObject);
            _plugin = NeftaPlugin.Init(pluginGameObject, appId);
            _plugin._adapterListener = new NeftaAdapterListener();
#elif UNITY_IOS
            NeftaPlugin_Init(appId, OnInsights);
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

        /// <summary>
        /// Should be called when MAX loads any ad (MaxSdkCallbacks.[AdType].OnAdLoadedEvent)
        /// </summary>
        /// <param name="adType">Ad format of the loaded ad</param>
        /// <param name="usedInsight">Insights that were used</param>
        /// <param name="adInfo">Loaded MAX Ad instance data</param>
        public static void OnExternalMediationRequestLoaded(AdType adType, AdInsight usedInsight, MaxSdkBase.AdInfo adInfo)
        {
            OnExternalMediationRequest(adType, usedInsight, adInfo.AdUnitIdentifier, adInfo.Revenue, adInfo.RevenuePrecision, 1, null, null);
        }

        /// <summary>
        /// Should be called when MAX loads any ad (MaxSdkCallbacks.[AdType].OnAdLoadedEvent)
        /// </summary>
        /// <param name="adType">Ad format of the loaded ad</param>
        /// <param name="usedInsight">Insights that were used</param>
        /// <param name="adUnitId">Ad unit that selected to load</param>
        /// <param name="errorInfo">Load fail reason</param>
        public static void OnExternalMediationRequestFailed(AdType adType, AdInsight usedInsight, string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            var providerStatus = ((int)errorInfo.Code).ToString(CultureInfo.InvariantCulture);
            var networkStatus = errorInfo.MediatedNetworkErrorCode.ToString(CultureInfo.InvariantCulture);
            OnExternalMediationRequest(adType, usedInsight, adUnitId, -1, null, errorInfo.Code == MaxSdkBase.ErrorCode.NoFill ? 2 : 0, providerStatus, networkStatus);
        }

        private static void OnExternalMediationRequest(AdType adType, AdInsight insight, string adUnitId, double revenue, string precision, int status, string providerStatus, string networkStatus)
        {
            string recommendedAdUnitId = null;
            double calculatedFloorPrice = 0;
            if (insight != null)
            {
                recommendedAdUnitId = insight._adUnit;
                calculatedFloorPrice = insight._floorPrice;
                if (insight._type != adType)
                {
                    Debug.LogWarning($"OnExternalMediationRequest reported adType: {adType} doesn't match insight adType: {insight._type}");
                }
            }
#if UNITY_EDITOR
            _plugin.OnExternalMediationRequest("applovin-max", (int)adType, recommendedAdUnitId, -1.0, calculatedFloorPrice, adUnitId, revenue, precision, status, providerStatus, networkStatus);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationRequest("applovin-max", (int)adType, recommendedAdUnitId, -1.0, calculatedFloorPrice, adUnitId, revenue, precision, status, providerStatus, networkStatus);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationRequest", "applovin-max", (int)adType, recommendedAdUnitId, -1.0, calculatedFloorPrice, adUnitId, revenue, precision, status, providerStatus, networkStatus);
#endif
        }

        public static void OnExternalMediationImpression(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adInfo == null)
            {
                return;
            }

            var network = adInfo.NetworkName;
            var format = adInfo.AdFormat;
            var creativeId = adInfo.CreativeIdentifier;
            var revenue = adInfo.Revenue;
            var precision = adInfo.RevenuePrecision;
            var sb = new StringBuilder();
            sb.Append("{\"ad_unit_id\":\"");
            sb.Append(adUnitId);
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

            sb.Append("\"");
            
            var data = sb.ToString();
#if UNITY_EDITOR
            _plugin.OnExternalMediationImpression(data);
#elif UNITY_IOS
            NeftaAdapter_OnExternalMediationImpressionAsString(network, format, creativeId, data, revenue, precision);
#elif UNITY_ANDROID
            if (_adapter == null) {
                _adapter = new AndroidJavaClass("com.applovin.mediation.adapters.NeftaMediationAdapter");
            }
            _adapter.CallStatic("OnExternalMediationImpressionAsString", network, format, creativeId, data, revenue, precision);
#endif
        }

#if NEFTA_LEVELPLAY || NEFTA_LEVELPLAY_8_0_OR_NEWER
#if NEFTA_LEVELPLAY_8_0_OR_NEWER
        public static void OnLevelPlayRequestLoaded(AdType adType, double requestedFloorPrice, AdInsight usedInsight, Unity.Services.LevelPlay.LevelPlayAdInfo adInfo)
        {
            OnLevelPlayRequest(adType, requestedFloorPrice, usedInsight, adInfo.AdUnitId, adInfo.Revenue ?? 0, adInfo.Precision, 1, null, null);
        }
        
        public static void OnLevelPlayRequestFailed(AdType adType, double requestedFloorPrice, AdInsight usedInsight, Unity.Services.LevelPlay.LevelPlayAdError error)
        {
            var status = 0;
            if (error.ErrorCode == 509 || error.ErrorCode == 606 || error.ErrorCode == 706 || error.ErrorCode == 1058 || error.ErrorCode == 1158)
            {
                status = 2;
            }

            OnLevelPlayRequest(adType, requestedFloorPrice, usedInsight, error.AdUnitId, -1, null, status, error.ErrorCode.ToString(CultureInfo.InvariantCulture), null);
        }
#else
        public static void OnLevelPlayRequestLoaded(AdType adType, double requestedFloorPrice, AdInsight usedInsight, com.unity3d.mediation.LevelPlayAdInfo adInfo)
        {
            OnLevelPlayRequest(adType, requestedFloorPrice, usedInsight, adInfo.AdUnitId, adInfo.Revenue ?? 0, adInfo.Precision, 1, null, null);
        }

        public static void OnLevelPlayRequestFailed(AdType adType, double requestedFloorPrice, AdInsight usedInsight, com.unity3d.mediation.LevelPlayAdError error)
        {
            var status = 0;
            if (error.ErrorCode == 509 || error.ErrorCode == 606 || error.ErrorCode == 706 || error.ErrorCode == 1058 || error.ErrorCode == 1158)
            {
                status = 2;
            }

            OnLevelPlayRequest(adType, requestedFloorPrice, usedInsight, error.AdUnitId, -1, null, status, error.ErrorCode.ToString(CultureInfo.InvariantCulture), null);
        }
#endif
        private static void OnLevelPlayRequest(AdType adType, double requestedFloorPrice, AdInsight insight, string adUnitId, double revenue, string precision, int status, string providerStatus, string networkStatus)
        {
            double calculatedFloorPrice = 0;
            if (insight != null)
            {
                calculatedFloorPrice = insight._floorPrice;
                if (insight._type != adType)
                {
                    Debug.LogWarning($"OnExternalMediationRequest reported adType: {adType} doesn't match insight adType: {insight._type}");
                }
            }
#if UNITY_EDITOR
            _plugin.OnExternalMediationRequest("ironsource-levelplay", (int)adType, null, requestedFloorPrice, calculatedFloorPrice, adUnitId, revenue, precision, status, providerStatus, networkStatus);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationRequest("ironsource-levelplay", (int)adType, null, requestedFloorPrice, calculatedFloorPrice, adUnitId, revenue, precision, status, providerStatus, networkStatus);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationRequest", "ironsource-levelplay", (int)adType, null, requestedFloorPrice, calculatedFloorPrice, adUnitId, revenue, precision, status, providerStatus, networkStatus);
#endif
        }

        public static void OnLevelPlayImpression(IronSourceImpressionData impression)
        {
            var adType = (int)AdType.Other;
            if (impression.adFormat != null)
            {
                var formatLower = impression.adFormat.ToLower();
                if (formatLower == "banner")
                {
                    adType = (int)AdType.Banner;
                }
                else if (formatLower.Contains("inter"))
                {
                    adType = (int)AdType.Interstitial;
                }
                else if (formatLower.Contains("rewarded"))
                {
                    adType = (int)AdType.Rewarded;
                }
            }
            var revenue = impression.revenue ?? 0;
            var precision = impression.precision;
#if UNITY_EDITOR
            _plugin.OnExternalMediationImpression(impression.allData);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationImpressionAsString("ironsource-levelplay", impression.allData, adType, revenue, precision);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationImpressionAsString", "ironsource-levelplay", impression.allData, adType, revenue, precision);
#endif
        }
#endif
        public static void GetInsights(int insights, OnInsightsCallback callback, int timeoutInSeconds=0)
        {
            var id = 0;
            lock (_insightRequests)
            {
                id = _insightId;
                var request = new InsightRequest(id, callback);
                _insightRequests.Add(request);
                _insightId++;
            }
            
#if UNITY_EDITOR
            _plugin.GetInsights(id, insights, timeoutInSeconds);
#elif UNITY_IOS
            NeftaPlugin_GetInsights(id, insights, timeoutInSeconds);
#elif UNITY_ANDROID
            _plugin.Call("GetInsightsBridge", id, insights, timeoutInSeconds);
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
            NeftaPlugin.SetOverride(root);
#elif UNITY_IOS
            NeftaPlugin_SetOverride(root);
#elif UNITY_ANDROID
            _neftaPluginClass.CallStatic("SetOverride", root);
#endif
        }
        
        internal static void IOnInsights(int id, string bi)
        {
            var insights = new Insights(JsonUtility.FromJson<InsightsDto>(bi));
            try
            {
                lock (_insightRequests)
                {
                    for (var i = _insightRequests.Count - 1; i >= 0; i--)
                    {
                        var insightRequest = _insightRequests[i];
                        if (insightRequest._id == id)
                        {
                            insightRequest._returnContext.Post(_ => insightRequest._callback(insights), null);
                            _insightRequests.RemoveAt(i);
                            break;
                        }
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