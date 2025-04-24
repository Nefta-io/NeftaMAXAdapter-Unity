using System;
using System.Collections.Generic;
using NeftaCustomAdapter;
using UnityEngine;

namespace AdDemo
{
    public class Rewarded
    {
#if UNITY_IOS
        private const string _defaultAdUnitId = "08304643cb16df3b";
#else // UNITY_ANDROID
        private const string _defaultAdUnitId = "3082ee9199cf59f0";
#endif
        
        public static readonly string AdUnitId = "recommended_rewarded_ad_unit_id";
        public static readonly string FloorPrice = "calculated_user_floor_price_rewarded";

        private Action GetInsights;
        private string _selectedAdUnitId;
        private string _recommendedAdUnitId;
        private double _calculatedBidFloor;
        private int _consecutiveAdFail;
        private bool _isLoadPending;
        private string _loadedAdUnitId;
        
        private readonly Action<string> _setStatus;
        private readonly Action _onLoad;
        private readonly Action<bool> _onFullScreenAdDisplayed;
        
        public void OnUserInsights(Dictionary<string, Insight> insights)
        {
            _recommendedAdUnitId = insights[AdUnitId]._string;
            _calculatedBidFloor = insights[FloorPrice]._float;
            
            Debug.Log($"OnUserInsights for Rewarded recommended AdUnit: {_recommendedAdUnitId}, calculated bid floor: {_calculatedBidFloor}");

            _selectedAdUnitId = _recommendedAdUnitId;
            
            if (_isLoadPending)
            {
                Load();
            }
        }

        public Rewarded(Action requestNewInsight, Action<string> setStatus, Action onLoad, Action<bool> onFullScreenAdDispalyed)
        {
            GetInsights = requestNewInsight;
            _onFullScreenAdDisplayed = onFullScreenAdDispalyed;
            
            _setStatus = setStatus;
            _onLoad = onLoad;
            
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnAdFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnAdHideEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        }

        public void Load()
        {
            _isLoadPending = false;
            _loadedAdUnitId = _selectedAdUnitId ?? _defaultAdUnitId;
            MaxSdk.LoadRewardedAd(_loadedAdUnitId);
        }
        
        public void Show()
        {
            if (MaxSdk.IsRewardedAdReady(_loadedAdUnitId))
            {
                _setStatus("Showing");
                MaxSdk.ShowRewardedAd(_loadedAdUnitId);
            }
            else
            {
                _setStatus("Ad not ready");
            }
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Rewarded, _recommendedAdUnitId, _calculatedBidFloor, adInfo);
            
            _setStatus($"Loaded {adInfo.NetworkName} {adInfo.NetworkPlacement}");

            _onLoad();
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            _consecutiveAdFail = 0;
            
            NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Rewarded, _recommendedAdUnitId, _calculatedBidFloor, adUnitId, errorInfo);
            
            if (errorInfo.Code == MaxSdkBase.ErrorCode.NoFill)
            {
                _consecutiveAdFail++;
                if (_consecutiveAdFail == 1) // in case of first no fill, try to get new insight (will probably return adUnit with lower bid floor
                {
                    _isLoadPending = true;
                    GetInsights();
                }
                else // for consequential no fills go with default (no bid floor) ad unit
                {
                    _selectedAdUnitId = null;
                    Load();
                }
            }
            
            _setStatus("Load failed");
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Display failed");
        }
        
        private void OnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Displayed");
            _onFullScreenAdDisplayed(true);
        }
        
        private void OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Rewarded ad clicked");
        }
        
        private void OnAdHideEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Hidden");
            _onFullScreenAdDisplayed(false);
        }
        
        private void OnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Rewarded ad received reward");
        }
        
        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Revenue paid");
        }
    }
}