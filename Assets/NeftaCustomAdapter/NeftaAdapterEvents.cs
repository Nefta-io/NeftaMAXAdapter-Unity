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
        
        [Serializable]
        internal class InitConfigurationDto
        {
            public bool skipOptimization;
            public string providerAdUnits;
            public int disabledFeatures;
        }
        
        [Flags]
        private enum Feature {
            Insights = 1,
            Exchange = 1 << 1,
            GameEvents = 1 << 2,
            SessionEvents = 1 << 3,
            ExternalMediationResponse = 1 << 4,
            ExternalMediationRequest = 1 << 5,
            ExternalMediationImpression = 1 << 6,
            ExternalMediationClick = 1 << 7,
        }
        
        public struct ExtParams
        {
            public const string TestGroup = "test_group";
            public const string AttributionSource = "attribution_source";
            public const string AttributionCampaign = "attribution_campaign";
            public const string AttributionAdset = "attribution_adset";
            public const string AttributionCreative = "attribution_creative";
            public const string AttributionIncentivized = "attribution_incentivized";
        }

        private const string _mediationProvider = "applovin-max";

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
        private delegate void OnReadyDelegate(string initConfig);
        private delegate void OnInsightsDelegate(int requestId, int adapterResponseType, string adapterResponse);

        [MonoPInvokeCallback(typeof(OnReadyDelegate))] 
        private static void OnReadyBridge(string initConfig) {
            IOnReady(initConfig);
        }

        [MonoPInvokeCallback(typeof(OnInsightsDelegate))] 
        private static void OnInsightsBridge(int requestId, int adapterResponseType, string adapterResponse) {
            IOnInsights(requestId, adapterResponseType, adapterResponse);
        }

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_EnableLogging(bool enable);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_SetExtraParameter(string key, string value);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Init(string appId, OnReadyDelegate onReady, OnInsightsDelegate onInsights);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Record(int type, int category, int subCategory, string nameValue, long value, string customPayload);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationRequest(string provider, int adType, string id, string requestedAdUnitId, double requestedFloorPrice, int requestId);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationResponseAsString(string provider, string id, string id2, double revenue, string precision, int status, string providerStatus, string networkStatus, string baseData);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationImpressionAsString(bool isClick, string provider, string data, string id, string id2);

        [DllImport ("__Internal")]
        private static extern string NeftaPlugin_GetNuid(bool present);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_SetTracking(bool isAuthorized);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_SetContentRating(string rating);
        
        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_GetInsights(int requestId, int insights, int previousRequestId, int timeoutInSeconds);
        
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
        private static Feature _disabledFeatures;

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
        
        public static Action<InitConfiguration> OnReady;

        public static void Init(string appId, bool sendAdEvents=true, bool simulateAdsInEditor=false)
        {
#if UNITY_EDITOR
            _plugin = NeftaPlugin.Init(appId, simulateAdsInEditor);
            _plugin._adapterListener = new NeftaAdapterListener();
#elif UNITY_IOS
            NeftaPlugin_Init(appId, OnReadyBridge, OnInsightsBridge);
#elif UNITY_ANDROID
            AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            _plugin = NeftaPluginClass.CallStatic<AndroidJavaObject>("Init", unityActivity, appId, new NeftaAdapterListener(), "unity-applovin-max");
#endif
            if (sendAdEvents)
            {
                MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnExternalMediationImpression;
                MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnExternalMediationClick;
                MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnExternalMediationImpression;
                MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnExternalMediationClick;
            }

            _insightRequests = new List<InsightRequest>();
        }

        public static void Record(GameEvent gameEvent)
        {
            if ((_disabledFeatures & Feature.GameEvents) != 0)
            {
                return;
            }
            
            var type = gameEvent._eventType;
            var category = gameEvent._category;
            var subCategory = gameEvent._subCategory;
            var name = gameEvent._name;
            var value = gameEvent._value;
            var customPayload = gameEvent._customString;
            Record(type, category, subCategory, name, value, customPayload);
        }

        internal static void Record(int type, int category, int subCategory, string name, long value, string customPayload)
        {
            if ((_disabledFeatures & Feature.GameEvents) != 0)
            {
                return;
            }
            
#if UNITY_EDITOR
            _plugin.Record(type, category, subCategory, name, value, customPayload);
#elif UNITY_IOS
            NeftaPlugin_Record(type, category, subCategory, name, value, customPayload);
#elif UNITY_ANDROID
            _plugin.Call("Record", type, category, subCategory, name, value, customPayload);
#endif
        }
        
        public static void OnExternalMediationRequest(AdType adType, string requestedAdUnitId, AdInsight usedInsight, double customBidFloor=-1)
        {
            var requestId = -1;
            if (usedInsight != null)
            {
                requestId = usedInsight._requestId;
                if (customBidFloor < 0)
                {
                    customBidFloor = usedInsight._floorPrice;
                }
            }
            OnExternalMediationRequest(_mediationProvider, adType, requestedAdUnitId, requestedAdUnitId, customBidFloor, requestId);
        }
        
        public static void OnExternalMediationRequest(AdType adType, string requestedAdUnitId, double requestedFloorPrice=-1)
        {
            OnExternalMediationRequest(_mediationProvider, adType, requestedAdUnitId, requestedAdUnitId, requestedFloorPrice, -1);
        }

        /// <summary>
        /// Should be called when MAX loads any ad (MaxSdkCallbacks.[AdType].OnAdLoadedEvent)
        /// </summary>
        /// <param name="requestedFloorPrice">When requesting an ad with bid floor, provide requested floor here or -1 otherwise</param>
        /// <param name="adInfo">Loaded MAX Ad instance data</param>
        public static void OnExternalMediationRequestLoaded(MaxSdkBase.AdInfo adInfo)
        {
            string baseString = null;
            if (adInfo.WaterfallInfo != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append('{');
                SerializeWaterfall(sb, adInfo.WaterfallInfo);
                sb.Append('}');
                baseString = sb.ToString();
            }
            OnExternalMediationResponse(_mediationProvider, adInfo.AdUnitIdentifier, null, adInfo.Revenue, adInfo.RevenuePrecision, 1, null, null, baseString);
        }

        /// <summary>
        /// Should be called when MAX loads any ad (MaxSdkCallbacks.[AdType].OnAdLoadedEvent)
        /// </summary>
        /// <param name="adUnitId">Ad unit that selected to load</param>
        /// <param name="requestedFloorPrice">When requesting an ad with bid floor, provide requested floor here or -1 otherwise</param>
        /// <param name="errorInfo">Load fail reason</param>
        public static void OnExternalMediationRequestFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if ((_disabledFeatures & Feature.ExternalMediationRequest) != 0)
            {
                return;
            }
            
            var providerStatus = ((int)errorInfo.Code).ToString(CultureInfo.InvariantCulture);
            var networkStatus = errorInfo.MediatedNetworkErrorCode.ToString(CultureInfo.InvariantCulture);
            string baseString = null;
            if (errorInfo.WaterfallInfo != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append('{');
                SerializeWaterfall(sb, errorInfo.WaterfallInfo);
                sb.Append("}");
                baseString = sb.ToString();
            }
            OnExternalMediationResponse(_mediationProvider, adUnitId, null, -1, null, errorInfo.Code == MaxSdkBase.ErrorCode.NoFill ? 2 : 0, providerStatus, networkStatus, baseString);
        }
        
        private static void OnExternalMediationRequest(string provider, AdType adType, string id, string requestedAdUnitId, double requestedFloorPrice, int requestId)
        {
#if UNITY_EDITOR
            _plugin.OnExternalMediationRequest(provider, (int)adType, id, requestedAdUnitId, requestedFloorPrice, requestId);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationRequest(provider, (int)adType, id, requestedAdUnitId, requestedFloorPrice, requestId);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationRequest", provider, (int)adType, id, requestedAdUnitId, requestedFloorPrice, requestId);
#endif
        }

        private static void OnExternalMediationResponse(string provider, string id, string id2, double revenue, string precision, int status, string providerStatus, string networkStatus, string baseString)
        {
#if UNITY_EDITOR
            _plugin.OnExternalMediationResponseAsString(provider, id, id2, revenue, precision, status, providerStatus, networkStatus, baseString);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationResponseAsString(provider, id, id2, revenue, precision, status, providerStatus, networkStatus, baseString);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationResponseAsString", provider, id, id2, revenue, precision, status, providerStatus, networkStatus, baseString);
#endif
        }

        public static void OnExternalMediationImpression(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if ((_disabledFeatures & Feature.ExternalMediationImpression) != 0)
            {
                return;
            }
            
            OnMAXImpression(false, adUnitId, adInfo);
        }

        public static void OnExternalMediationClick(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if ((_disabledFeatures & Feature.ExternalMediationClick) != 0)
            {
                return;
            }
            
            OnMAXImpression(true, adUnitId, adInfo);
        }

        private static void OnMAXImpression(bool isClick, string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adInfo == null)
            {
                return;
            }
            
            var sb = new StringBuilder();
            sb.Append("{\"ad_unit_id\":\"");
            sb.Append(adUnitId);
            sb.Append("\",\"placement_name\":\"");
            sb.Append(JavaScriptStringEncode(adInfo.Placement));
            sb.Append("\",\"request_latency\":");
            sb.Append(adInfo.LatencyMillis);
            sb.Append(",\"dsp_name\":\"");
            sb.Append(JavaScriptStringEncode(adInfo.DspName));
            sb.Append("\",\"network_name\":\"");
            sb.Append(JavaScriptStringEncode(adInfo.NetworkName));
            sb.Append("\",\"creative_id\":\"");
            sb.Append(JavaScriptStringEncode(adInfo.CreativeIdentifier));
            sb.Append("\",\"format\":\"");
            sb.Append(JavaScriptStringEncode(adInfo.AdFormat));
            sb.Append("\",\"revenue_precision\":\"");
            sb.Append(adInfo.RevenuePrecision);
            sb.Append("\",\"revenue\":");
            sb.Append(adInfo.Revenue.ToString(CultureInfo.InvariantCulture));
            if (adInfo.WaterfallInfo != null)
            {
                sb.Append(',');
                SerializeWaterfall(sb, adInfo.WaterfallInfo);
            }
            sb.Append("}");

            var data = sb.ToString();
            OnExternalMediationImpression(isClick, _mediationProvider, data, adUnitId, null);
        }

        private static void SerializeWaterfall(StringBuilder sb, MaxSdkBase.WaterfallInfo waterfallInfo)
        {
            sb.Append("\"waterfall_name\":\"");
            sb.Append(JavaScriptStringEncode(waterfallInfo.Name));
            var responses = waterfallInfo.NetworkResponses;
            StringBuilder sb2 = null;
            if (responses != null && responses.Count > 0)
            {
                sb2 = new StringBuilder();
                var isFirst = true;
                sb.Append("\",\"waterfall\":[\"");
                foreach (var response in responses)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        sb.Append("\",\"");
                        sb2.Append(",");
                    }

                    string networkName = null;
                    if (response.MediatedNetwork != null)
                    {
                        networkName = response.MediatedNetwork.Name;   
                    }
                    if (String.IsNullOrEmpty(networkName) && response.Credentials != null &&
                        response.Credentials.TryGetValue("network_name", out var name))
                    {
                        if (name is string nameString)
                        {
                            networkName = nameString;
                        }
                    }

                    sb2.Append("{\"name\":\"");
                    if (networkName != null)
                    {
                        networkName = JavaScriptStringEncode(networkName);
                        sb.Append(networkName);
                        sb2.Append(networkName);
                    }
                    sb2.Append("\",\"ad_load_state\":\"");
                    var loadState = "FAILED_TO_LOAD";
                    if (response.AdLoadState == MaxSdkBase.MaxAdLoadState.AdLoadNotAttempted) {
                        loadState = "AD_LOAD_NOT_ATTEMPTED";
                    } else if (response.AdLoadState == MaxSdkBase.MaxAdLoadState.AdLoaded) {
                        loadState = "AD_LOADED";
                    }
                    sb2.Append(loadState);
                    sb2.Append("\",\"is_bidding\":");
                    sb2.Append(response.IsBidding ? "true" : "false");
                    sb2.Append(",\"latency_millis\":");
                    sb2.Append(response.LatencyMillis);
                    var error = response.Error;
                    if (error != null)
                    {
                        sb2.Append(",\"error\":{\"code\":");
                        sb2.Append((int)error.Code);
                        sb2.Append(",\"name\":\"");
                        sb2.Append(JavaScriptStringEncode(error.Message));   
                        sb2.Append("\"}");
                    }
                    sb2.Append("}");
                }
                sb.Append("\"]");
            }
            else
            {
                sb.Append("\"");
            }

            sb.Append(",\"waterfall_test_name\":\"");
            sb.Append(JavaScriptStringEncode(waterfallInfo.TestName));   
            if (sb2 != null)
            {
                sb.Append("\",\"waterfall_responses\":[");
                sb.Append(sb2.ToString());
                sb.Append("]");
            }
            else
            {
                sb.Append('"');
            }
        }

        private static void OnExternalMediationImpression(bool isClick, string provider, string data, string id, string id2)
        {
#if UNITY_EDITOR
            _plugin.OnExternalMediationImpressionAsString(isClick, provider, data, id, id2);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationImpressionAsString(isClick, provider, data, id, id2);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationImpressionAsString", isClick, provider, data, id, id2);
#endif
        }
        
        public static void GetInsights(int insights, AdInsight previousInsight, OnInsightsCallback callback, int timeoutInSeconds=0)
        {
            if ((_disabledFeatures & Feature.Insights) != 0)
            {
                callback(new Insights());
                return;
            }
            
            var id = 0;
            var previousRequestId = -1;
            if (previousInsight != null)
            {
                previousRequestId = previousInsight._requestId;
            }
            lock (_insightRequests)
            {
                id = _insightId;
                var request = new InsightRequest(id, callback);
                _insightRequests.Add(request);
                _insightId++;
            }
            
#if UNITY_EDITOR
            _plugin.GetInsights(id, insights, previousRequestId, timeoutInSeconds);
#elif UNITY_IOS
            NeftaPlugin_GetInsights(id, insights, previousRequestId, timeoutInSeconds);
#elif UNITY_ANDROID
            _plugin.Call("GetInsightsBridge", id, insights, previousRequestId, timeoutInSeconds);
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

        public static void SetTracking(bool isAuthorized)
        {
#if UNITY_EDITOR
            _plugin.SetTracking(isAuthorized);
#elif UNITY_IOS
            NeftaPlugin_SetTracking(isAuthorized);
#elif UNITY_ANDROID
            _plugin.Call("SetTracking", isAuthorized);
#endif
        }
        
        public static void SetExtraParameter(string key, string value)
        {
#if UNITY_EDITOR
            NeftaPlugin.SetExtraParameter(key, value);
#elif UNITY_IOS
            NeftaPlugin_SetExtraParameter(key, value);
#elif UNITY_ANDROID
            NeftaPluginClass.CallStatic("SetExtraParameter", key, value);
#endif
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
        
        internal static void IOnReady(string initConfig)
        {
            var initDto = JsonUtility.FromJson<InitConfigurationDto>(initConfig);
            _disabledFeatures = (Feature)initDto.disabledFeatures;
            if (OnReady != null)
            {
                var initConfiguration = new InitConfiguration(initDto.skipOptimization, initDto.providerAdUnits);
                OnReady.Invoke(initConfiguration);
            }
        }
        
        internal static void IOnInsights(int id, int adapterResponseType, string adapterResponse)
        {
            var insights = new Insights();
            if (adapterResponseType == Insights.Churn)
            {
                insights._churn = new Churn(JsonUtility.FromJson<ChurnDto>(adapterResponse));
            }
            else if (adapterResponseType == Insights.Banner)
            {
                insights._banner = new AdInsight(AdType.Banner, JsonUtility.FromJson<AdConfigurationDto>(adapterResponse));
            }
            else if (adapterResponseType == Insights.Interstitial)
            {
                insights._interstitial = new AdInsight(AdType.Interstitial, JsonUtility.FromJson<AdConfigurationDto>(adapterResponse));
            }
            else if (adapterResponseType == Insights.Rewarded)
            {
                insights._rewarded = new AdInsight(AdType.Rewarded, JsonUtility.FromJson<AdConfigurationDto>(adapterResponse));
            }
            
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
            if (value == null)
            {
                return null;
            }
            var len = value.Length;
            var needEncode = false;
            char c;
            for (var i = 0; i < len; i++)
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
            
            var sb = new StringBuilder();
            for (var i = 0; i < len; i++)
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