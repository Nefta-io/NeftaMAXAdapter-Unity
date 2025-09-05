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
        private AdInsight _dynamicAdUnitInsight;
        private int _consecutiveDynamicBidAdFails;
        private AdRequest _defaultAdRequest;
        
        private Queue<string> _statusQueue;
        private Action<bool> _onFullScreenAdDisplayed;
        
        [SerializeField] private Text _title;
        [SerializeField] private Toggle _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private void StartLoading()
        {
            if (_dynamicAdRequest == null)
            {
                GetInsightsAndLoad();   
            }
            if (_defaultAdRequest == null)
            {
                LoadDefault();   
            }
        }

        private void GetInsightsAndLoad()
        {
            _dynamicAdRequest = new AdRequest();
            NeftaAdapterEvents.GetInsights(Insights.Interstitial, LoadWithInsights, TimeoutInSeconds);
        }
        
        private void LoadWithInsights(Insights insights)
        {
            if (insights._interstitial != null)
            {
                _dynamicAdUnitInsight = insights._interstitial;
                var bidFloor = _dynamicAdUnitInsight._floorPrice.ToString(CultureInfo.InvariantCulture);
            
                SetStatus($"Loading Dynamic AdUnit with bid floor: {bidFloor}");
                MaxSdk.SetInterstitialExtraParameter(DynamicAdUnitId, "disable_auto_retries", "true");
                MaxSdk.SetInterstitialExtraParameter(DynamicAdUnitId, "jC7Fp", bidFloor);
                MaxSdk.LoadInterstitial(DynamicAdUnitId);
            }
        }
        
        private void LoadDefault()
        {
            _defaultAdRequest = new AdRequest();
            SetStatus("Loading Default AdUnit");
            MaxSdk.LoadInterstitial(DefaultAdUnitId);
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (adUnitId == DynamicAdUnitId)
            {
                NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Interstitial, _dynamicAdUnitInsight, adUnitId, errorInfo);
            
                SetStatus($"Load failed Dynamic {adUnitId}: {errorInfo}");

                _consecutiveDynamicBidAdFails++;
                StartCoroutine(RetryGetInsightsAndLoad());
            }
            else
            {
                NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Interstitial, null, adUnitId, errorInfo);
            
                SetStatus($"Load failed Default {adUnitId}: {errorInfo}");

                if (_load.isOn)
                {
                    LoadDefault();
                }
                else
                {
                    _defaultAdRequest = null;
                }
            }
        }
        
        private IEnumerator RetryGetInsightsAndLoad()
        {
            // As per MAX recommendations, retry with exponentially higher delays up to 64s
            // In case you would like to customize fill rate / revenue please contact our customer support
            yield return new WaitForSeconds(new [] { 0, 2, 4, 8, 16, 32, 64 }[Math.Min(_consecutiveDynamicBidAdFails, 6)]);
            if (_load.isOn)
            {
                GetInsightsAndLoad();
            }
            else
            {
                _dynamicAdRequest = null;
            }
        }

        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId == DynamicAdUnitId)
            {
                NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Interstitial, _dynamicAdUnitInsight, adInfo);

                SetStatus($"Loaded Dynamic {adUnitId}: {adInfo.NetworkName}");
                
                _consecutiveDynamicBidAdFails = 0;
                _dynamicAdRequest.Revenue = adInfo.Revenue;
            }
            else
            {
                NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Interstitial, null, adInfo);

                SetStatus($"Loaded Default {adUnitId}: {adInfo.NetworkName}");
                
                _defaultAdRequest.Revenue = adInfo.Revenue;
            }
            
            UpdateShowButton();
        }
        
        public void Init(Action<bool> onFullScreenAdDisplayed)
        {
            _statusQueue = new Queue<string>();
            _onFullScreenAdDisplayed = onFullScreenAdDisplayed;
            
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnShowEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnRevenuePaidEvent;
            
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
                if (_defaultAdRequest != null && _defaultAdRequest.Revenue > _dynamicAdRequest.Revenue)
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
                SetStatus("Showing Dynamic");
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
                SetStatus("Showing Default");
                MaxSdk.ShowInterstitial(DefaultAdUnitId);
                isShown = true;
            }
            _defaultAdRequest = null;
            return isShown;
        }

        private void OnShowEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnShowEvent");
            _onFullScreenAdDisplayed(true);
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdDisplayFailedEvent");
        }
        
        private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdHiddenEvent");
            _onFullScreenAdDisplayed(false);
            
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
                    Debug.Log($"Integration Interstitial: {status}");
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