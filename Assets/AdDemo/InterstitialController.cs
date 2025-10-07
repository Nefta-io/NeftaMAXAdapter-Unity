using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Nefta.Core.Events;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace AdDemo
{
    public class InterstitialController : MonoBehaviour
    {
#if UNITY_IOS
        private const string DynamicAdUnitId = "78b66d4cd80ca1e7";
        private const string DefaultAdUnitId = "c9acf50602329bfe";
#else // UNITY_ANDROID
        private const string DynamicAdUnitId = "850bcc93f949090c";
        private const string DefaultAdUnitId = "60bbc7cc56dfa329";
#endif
        private const int TimeoutInSeconds = 5;
        
        private class AdRequest
        {
            public double? Revenue;
        }
        
        private AdRequest _dynamicAdRequest;
        private AdInsight _dynamicInsight;
        private int _consecutiveDynamicAdFails;
        private AdRequest _defaultAdRequest;
        
        private Queue<string> _statusQueue;
        
        [SerializeField] private Text _title;
        [SerializeField] private Toggle _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private void StartLoading()
        {
            if (_dynamicAdRequest == null)
            {
                GetInsightsAndLoad(null);   
            }
            if (_defaultAdRequest == null)
            {
                LoadDefault();   
            }
        }

        private void GetInsightsAndLoad(AdInsight previousInsight)
        {
            _dynamicAdRequest = new AdRequest();
            NeftaAdapterEvents.GetInsights(Insights.Interstitial, previousInsight, LoadWithInsights, TimeoutInSeconds);
        }
        
        private void LoadWithInsights(Insights insights)
        {
            _dynamicInsight = insights._interstitial;
            if (_dynamicInsight != null)
            {
                var bidFloor = _dynamicInsight._floorPrice.ToString(CultureInfo.InvariantCulture);
                
                SetStatus($"Loading Dynamic Interstitial with floor: {bidFloor}");
                MaxSdk.SetInterstitialExtraParameter(DynamicAdUnitId, "disable_auto_retries", "true");
                MaxSdk.SetInterstitialExtraParameter(DynamicAdUnitId, "jC7Fp", bidFloor);
                MaxSdk.LoadInterstitial(DynamicAdUnitId);

                NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Interstitial, DynamicAdUnitId, _dynamicInsight);
            }
        }
        
        private void LoadDefault()
        {
            _defaultAdRequest = new AdRequest();
            SetStatus("Loading Default Interstitial");
            MaxSdk.LoadInterstitial(DefaultAdUnitId);
            
            NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Interstitial, DefaultAdUnitId);
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(adUnitId, errorInfo);
            if (adUnitId == DynamicAdUnitId)
            {
                SetStatus($"Load failed Dynamic {adUnitId}: {errorInfo}");
                
                _consecutiveDynamicAdFails++;
                StartCoroutine(RetryGetInsightsAndLoad());
            }
            else
            {
                SetStatus($"Load failed Default {adUnitId}: {errorInfo}");

                _defaultAdRequest = null;
                if (_load.isOn)
                {
                    LoadDefault();
                }
            }
        }
        
        private IEnumerator RetryGetInsightsAndLoad()
        {
            // As per MAX recommendations, retry with exponentially higher delays up to 64s
            // In case you would like to customize fill rate / revenue please contact our customer support
            yield return new WaitForSeconds(new [] { 0, 2, 4, 8, 16, 32, 64 }[Math.Min(_consecutiveDynamicAdFails, 6)]);
            if (_load.isOn)
            {
                GetInsightsAndLoad(_dynamicInsight);
            }
            else
            {
                _dynamicAdRequest = null;
            }
        }

        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(adInfo);
            if (adUnitId == DynamicAdUnitId)
            {
                SetStatus($"Loaded Dynamic {adUnitId} at: {adInfo.Revenue}");

                _consecutiveDynamicAdFails = 0;
                _dynamicAdRequest.Revenue = adInfo.Revenue;
            }
            else
            {
                SetStatus($"Loaded Default {adUnitId} at: {adInfo.Revenue}");
                
                _defaultAdRequest.Revenue = adInfo.Revenue;
            }

            UpdateShowButton();
        }
        
        public void Init()
        {
            _statusQueue = new Queue<string>();
            
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnShowEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnRevenuePaidEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnAdClickEvent;
            
            _title.text = "Interstitial";
            _load.onValueChanged.AddListener(OnLoadChanged);
            _show.onClick.AddListener(OnShowClick);
            
            _show.interactable = false;
        }
        
        private void OnLoadChanged(bool isOn)
        {
            if (isOn)
            {
                StartLoading();   
            }
            
            AddDemoGameEventExample();
        }
        
        private void OnShowClick()
        {
            bool isShown = false;
            if (_dynamicAdRequest != null && _dynamicAdRequest.Revenue.HasValue)
            {
                if (_defaultAdRequest != null && _defaultAdRequest.Revenue.HasValue &&
                    _defaultAdRequest.Revenue > _dynamicAdRequest.Revenue)
                {
                    isShown = TryShowDefault();
                }
                if (!isShown)
                {
                    isShown = TryShowDynamic();
                }
            }
            if (!isShown && _defaultAdRequest != null && _defaultAdRequest.Revenue.HasValue)
            {
                TryShowDefault();
            }
            
            UpdateShowButton();
        }
        
        private bool TryShowDynamic()
        {
            var isShown = false;
            if (MaxSdk.IsInterstitialReady(DynamicAdUnitId))
            {
                SetStatus("Showing Dynamic Interstitial");
                MaxSdk.ShowInterstitial(DynamicAdUnitId);
                isShown = true;
            }
            _dynamicAdRequest = null;
            return isShown;
        }
        
        private bool TryShowDefault()
        {
            var isShown = false;
            if (MaxSdk.IsInterstitialReady(DefaultAdUnitId))
            {
                SetStatus("Showing Default Interstitial");
                MaxSdk.ShowInterstitial(DefaultAdUnitId);
                isShown = true;
            }
            _defaultAdRequest = null;
            return isShown;
        }

        private void OnShowEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnShowEvent");
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdDisplayFailedEvent");
        }
        
        private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdHiddenEvent");
            
            // start new load cycle
            if (_load.isOn)
            {
                StartLoading();   
            }
        }
        
        private void OnRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"OnRevenuePaidEvent {adInfo.Revenue}");
        }
        
        private void OnAdClickEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"On Ad clicked {adUnitId}");
        }
        
        private void SetStatus(string status)
        {
            lock(_statusQueue)
            {
                _statusQueue.Enqueue(status);
            }
        }
        
        private void Update()
        {
            if (_statusQueue == null)
            {
                return;
            }
            
            lock (_statusQueue)
            {
                while (_statusQueue.Count > 0)
                {
                    var status = _statusQueue.Dequeue();
                    
                    _status.text = status;
                    Debug.Log($"NeftaPluginMAX Interstitial: {status}");
                }
            }
        }
        
        private void UpdateShowButton()
        {
            _show.interactable = _dynamicAdRequest != null && _dynamicAdRequest.Revenue.HasValue ||
                                 _defaultAdRequest != null && _defaultAdRequest.Revenue.HasValue;
        }

        private void AddDemoGameEventExample()
        {
            var category = (ResourceCategory) Random.Range(0, 9);
            var method = (ReceiveMethod)Random.Range(0, 8);
            var value = Random.Range(0, 101);
            new ReceiveEvent(category) { _method = method, _name = $"receive_{category} {method} {value}", _value = value }.Record();
        }
    }
}