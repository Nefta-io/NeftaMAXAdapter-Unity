using System;
using System.Collections.Generic;
using Nefta.Core.Events;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace AdDemo
{
    public class RewardedController : MonoBehaviour
    {
#if UNITY_IOS
        private const string _defaultAdUnitId = "08304643cb16df3b";
#else // UNITY_ANDROID
        private const string _defaultAdUnitId = "3082ee9199cf59f0";
#endif
        
        private const string AdUnitIdInsightName = "recommended_rewarded_ad_unit_id";
        private const string FloorPriceInsightName = "calculated_user_floor_price_rewarded";
        
        private MaxSdkBase.AdInfo _defaultAd;
        private string _recommendedAdUnitId;
        private MaxSdkBase.AdInfo _recommendedAd;
        private bool _isRecommendedLoadPending;
        private double _calculatedBidFloor;
        
        private Queue<string> _statusQueue;
        private Action<bool> _onFullScreenAdDisplayed;
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private void Load()
        {
            SetStatus($"Load default: {_defaultAd} recommended: {_recommendedAd}");
            
            if (_defaultAd == null)
            {
                MaxSdk.LoadRewardedAd(_defaultAdUnitId);
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
            
            Debug.Log($"OnBehaviourInsight for Rewarded recommended AdUnit: {_recommendedAdUnitId}, calculated bid floor: {_calculatedBidFloor}");
            
            if (!String.IsNullOrEmpty(_recommendedAdUnitId) && _defaultAdUnitId != _recommendedAdUnitId)
            {
                MaxSdk.LoadRewardedAd(_recommendedAdUnitId);
            }
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (adUnitId == _recommendedAdUnitId)
            {
                NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Rewarded, _recommendedAdUnitId, _calculatedBidFloor, adUnitId, errorInfo);

                _recommendedAdUnitId = null;
                _calculatedBidFloor = 0;
            }
            else
            {
                NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Rewarded, null, 0, adUnitId, errorInfo);
            }
            
            SetStatus($"Load failed {adUnitId}: {errorInfo}");

            // or automatically retry
            //if (_defaultAd == null && _recommendedAd == null)
            //{
            //    Load();
            //}
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId == _recommendedAdUnitId)
            {
                NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Rewarded, _recommendedAdUnitId, _calculatedBidFloor, adInfo);
                
                _recommendedAd = adInfo;
            }
            else
            {
                NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Rewarded, null, 0, adInfo);

                _defaultAd = adInfo;
            }
            
            SetStatus($"Loaded {adUnitId} at: {adInfo.Revenue}");
            _show.interactable = true;
        }
        
        public void Init(Action<bool> onFullScreenAdDisplayed)
        {
            _statusQueue = new Queue<string>();
            _onFullScreenAdDisplayed = onFullScreenAdDisplayed;
            
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnAdFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnAdHideEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;

            _title.text = "Rewarded";
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
                    MaxSdk.ShowRewardedAd(_defaultAdUnitId);
                    _defaultAd = null;
                }
                else
                {
                    MaxSdk.ShowRewardedAd(_recommendedAdUnitId);
                    _recommendedAd = null;
                }
            }
            else if (_defaultAd != null)
            {
                MaxSdk.ShowRewardedAd(_defaultAdUnitId);
                _defaultAd = null;
            }
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdDisplayFailedEvent");
        }
        
        private void OnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdDisplayedEvent");
            _onFullScreenAdDisplayed(true);
        }
        
        private void OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdClickedEvent");
        }
        
        private void OnAdHideEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdHideEvent");
            _onFullScreenAdDisplayed(false);
        }
        
        private void OnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdReceivedRewardEvent");
        }
        
        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"OnAdRevenuePaidEvent {adInfo.Revenue}");
        }
        
        private void SetStatus(string status)
        {
            lock (_statusQueue)
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
                    Debug.Log($"Integration Rewarded: {status}");
                }
            }
        }

        private void AddDemoGameEventExample()
        {
            var category = (ResourceCategory) Random.Range(0, 9);
            var method = (SpendMethod)Random.Range(0, 8);
            var value = Random.Range(0, 101);
            NeftaAdapterEvents.Record(new SpendEvent(category) { _method = method, _name = $"spend_{category} {method} {value}", _value = value });
        }
    }
}