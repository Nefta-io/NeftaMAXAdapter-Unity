using System;
using System.Collections.Generic;
using NeftaCustomAdapter;
using UnityEngine;

namespace AdDemo
{
    public class Interstitial
    {
#if UNITY_IOS
        private const string _defaultAdUnitId = "c9acf50602329bfe";
#else // UNITY_ANDROID
        private const string _defaultAdUnitId = "60bbc7cc56dfa329";
#endif
        
        public static readonly string AdUnitId = "recommended_interstitial_ad_unit_id";
        public static readonly string FloorPrice = "calculated_user_floor_price_interstitial";
        
        private Action GetInsights;
        private string _selectedAdUnitId;
        private string _recommendedAdUnitId;
        private double _calculatedBidFloor;
        private int _consecutiveAdFail;
        private bool _isLoadPending;
        
        private readonly Action<string> _setStatus;
        private readonly Action _onLoad;
        private string _loadedAdUnitId;
        private readonly Action<bool> _onFullScreenAdDisplayed;
        
        public void OnUserInsights(Dictionary<string, Insight> insights)
        {
            _recommendedAdUnitId = insights[AdUnitId]._string;
            _calculatedBidFloor = insights[FloorPrice]._float;
            
            Debug.Log($"OnUserInsights for Interstitial recommended AdUnit: {_recommendedAdUnitId}, calculated bid floor: {_calculatedBidFloor}");

            _selectedAdUnitId = _recommendedAdUnitId;
            
            if (_isLoadPending)
            {
                Load();
            }
        }

        public Interstitial(Action requestNewInsight, Action<string> setStatus, Action onLoad, Action<bool> onFullScreenAdDisplayed)
        {
            GetInsights = requestNewInsight;
            _onFullScreenAdDisplayed = onFullScreenAdDisplayed;
            
            _setStatus = setStatus;
            _onLoad = onLoad;
                        
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnShowEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnRevenuePaidEvent;
        }

        public void Load()
        {
            _loadedAdUnitId = _selectedAdUnitId ?? _defaultAdUnitId;
            MaxSdk.LoadInterstitial(_loadedAdUnitId);
        }
        
        public void Show()
        {
            if (MaxSdk.IsInterstitialReady(_loadedAdUnitId))
            { 
                _setStatus("Showing");
                MaxSdk.ShowInterstitial(_loadedAdUnitId);
            }
            else
            {
                _setStatus("Ad not ready");
            }
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Interstitial, _recommendedAdUnitId, _calculatedBidFloor, adInfo);
            
            _setStatus($"Loaded {adInfo.NetworkName} {adInfo.NetworkPlacement}");

            _onLoad();
        }

        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Interstitial, _recommendedAdUnitId, _calculatedBidFloor, adUnitId, error);
            
            if (error.Code == MaxSdkBase.ErrorCode.NoFill)
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
            
            _setStatus($"Load failed: {error.Message}");
        }

        private void OnShowEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Show");
            _onFullScreenAdDisplayed(true);
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Display failed");
        }
        
        private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Hidden");
            _onFullScreenAdDisplayed(false);
        }
        
        private void OnRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus($"Paid {adInfo.Revenue}");
        }
    }
}