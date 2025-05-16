using System;
using System.Collections.Generic;
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
        private const string _defaultAdUnitId = "c9acf50602329bfe";
#else // UNITY_ANDROID
        private const string _defaultAdUnitId = "60bbc7cc56dfa329";
#endif
        
        private const string AdUnitIdInsightName = "recommended_interstitial_ad_unit_id";
        private const string FloorPriceInsightName = "calculated_user_floor_price_interstitial";

        private string _defaultLoadingAdUnitId;
        private MaxSdkBase.AdInfo _defaultAd;
        private string _recommendedAdUnitId;
        private MaxSdkBase.AdInfo _recommendedAd;
        
        private double _calculatedBidFloor;
        private bool _isRecommendedLoadPending;
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private Queue<string> _statusQueue;
        private Action<bool> _onFullScreenAdDisplayed;

        private void Load()
        {
            SetStatus($"Load default: {_defaultAd} recommended: {_recommendedAd}");
            
            if (_defaultAd == null)
            {
                MaxSdk.LoadInterstitial(_defaultAdUnitId);
            }

            if (_recommendedAd == null)
            {
                NeftaAdapterEvents.GetBehaviourInsight(new string[] { AdUnitIdInsightName, FloorPriceInsightName }, OnBehaviourInsight);
            }
        }
        
        private void OnBehaviourInsight(Dictionary<string, Insight> insights)
        {
            _recommendedAdUnitId = null;
            _calculatedBidFloor = 0;
            if (insights.TryGetValue(AdUnitIdInsightName, out var insight))
            {
                _recommendedAdUnitId = insight._string;
            }
            if (insights.TryGetValue(FloorPriceInsightName, out insight))
            {
                _calculatedBidFloor = insight._float;
            }
            
            Debug.Log($"OnBehaviourInsight for Interstitial recommended AdUnit: {_recommendedAdUnitId}, calculated bid floor: {_calculatedBidFloor}");
            
            if (!String.IsNullOrEmpty(_recommendedAdUnitId) && _defaultAdUnitId != _recommendedAdUnitId)
            {
                MaxSdk.LoadInterstitial(_recommendedAdUnitId);
            }
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            if (adUnitId == _recommendedAdUnitId)
            {
                NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Interstitial, _recommendedAdUnitId, _calculatedBidFloor, adUnitId, error);

                _recommendedAdUnitId = null;
                _calculatedBidFloor = 0;
            }
            else
            {
                NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Interstitial, null, 0, adUnitId, error);
                
                _defaultLoadingAdUnitId = null;
            }
            
            SetStatus($"Load failed: {error.Message}");
            
            // or automatically retry
            //if (_defaultLoadingAdUnitId == null && _recommendedAdUnitId == null)
            //{
            //    Load();
            //}
        }

        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId == _recommendedAdUnitId)
            {
                NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Interstitial, _recommendedAdUnitId, _calculatedBidFloor, adInfo);
                
                _recommendedAd = adInfo;
            }
            else
            {
                NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Interstitial, null, 0, adInfo);

                _defaultAd = adInfo;
            }
            
            SetStatus($"Loaded {adUnitId} at: {adInfo.Revenue}");
            _show.interactable = true;
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
            _load.onClick.AddListener(OnLoadClick);
            _show.onClick.AddListener(OnShowClick);
            
            _show.interactable = false;
        }
        
        private void OnLoadClick()
        {
            Load();

            AddDemoGameEventExample();
        }
        
        private void OnShowClick()
        {
            SetStatus($"Show default: {_defaultAd} recommended: {_recommendedAd}");
            _show.interactable = false;

            if (_recommendedAd != null)
            {
                if (_defaultAd != null && _defaultAd.Revenue > _recommendedAd.Revenue)
                {
                    MaxSdk.ShowInterstitial(_defaultAdUnitId);
                    _defaultAd = null;
                }
                else
                {
                    MaxSdk.ShowInterstitial(_recommendedAdUnitId);
                    _recommendedAd = null;
                }
            }
            else if (_defaultAd != null)
            {
                MaxSdk.ShowInterstitial(_defaultAdUnitId);
                _defaultAd = null;
            }
        }

        private void OnShowEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("Show");
            _onFullScreenAdDisplayed(true);
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("Display failed");
        }
        
        private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("Hidden");
            _onFullScreenAdDisplayed(false);
        }
        
        private void OnRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"Paid {adInfo.Revenue}");
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

        private void AddDemoGameEventExample()
        {
            var category = (ResourceCategory) Random.Range(0, 9);
            var method = (ReceiveMethod)Random.Range(0, 8);
            var value = Random.Range(0, 101);
            NeftaAdapterEvents.Record(new ReceiveEvent(category) { _method = method, _name = $"receive_{category} {method} {value}", _value = value });
        }
    }
}