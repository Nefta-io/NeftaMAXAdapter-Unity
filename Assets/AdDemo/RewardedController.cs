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
    public class RewardedController : MonoBehaviour
    {
#if UNITY_IOS
        private const string DefaultAdUnitId = "08304643cb16df3b";
        private const string DynamicAdUnitId = "7c6097e4101586b0";
#else // UNITY_ANDROID
        private const string DefaultAdUnitId = "3082ee9199cf59f0";
        private const string DynamicAdUnitId = "c164298ebdd0c008";
#endif
        private const int TimeoutInSeconds = 5;
        
        private enum AdState
        {
            None,
            Loading,
            Ready
        }

        private AdState _dynamicAdState;
        private AdInsight _dynamicAdUnitInsight;
        private int _consecutiveDynamicBidAdFails;
        private AdState _defaultAdState;
        
        private Queue<string> _statusQueue;
        private Action<bool> _onFullScreenAdDisplayed;
        
        [SerializeField] private Text _title;
        [SerializeField] private Toggle _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private void StartLoading()
        {
            if (_dynamicAdState == AdState.None)
            {
                GetInsightsAndLoad();   
            }
            if (_defaultAdState == AdState.None)
            {
                LoadDefault();   
            }
        }

        private void GetInsightsAndLoad()
        {
            _dynamicAdState = AdState.Loading;
            NeftaAdapterEvents.GetInsights(Insights.Rewarded, LoadWithInsights, TimeoutInSeconds);
        }
        
        private void LoadWithInsights(Insights insights)
        {
            if (insights._rewarded != null)
            {
                _dynamicAdUnitInsight = insights._rewarded;
                var bidFloor = _dynamicAdUnitInsight._floorPrice.ToString(CultureInfo.InvariantCulture);
                
                SetStatus($"Loading DynamicBid AdUnit with bid floor: {bidFloor}");
                MaxSdk.SetRewardedAdExtraParameter(DynamicAdUnitId, "disable_auto_retries", "true");
                MaxSdk.SetRewardedAdExtraParameter(DynamicAdUnitId, "jC7Fp", bidFloor);
                MaxSdk.LoadRewardedAd(DynamicAdUnitId);
            }
        }

        private void LoadDefault()
        {
            _defaultAdState = AdState.Loading;
            SetStatus("Loading Default AdUnit");
            MaxSdk.LoadRewardedAd(DefaultAdUnitId);
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Rewarded,
                adUnitId == DynamicAdUnitId ? _dynamicAdUnitInsight : null, adUnitId, errorInfo);

            if (adUnitId == DynamicAdUnitId)
            {
                SetStatus($"Load failed Dynamic {adUnitId}: {errorInfo}");
                
                _consecutiveDynamicBidAdFails++;
                StartCoroutine(RetryGetInsightsAndLoad(true));
            }
            else
            {
                SetStatus($"Load failed Default {adUnitId}: {errorInfo}");
                
                StartCoroutine(RetryGetInsightsAndLoad(false));
            }
        }
        
        private IEnumerator RetryGetInsightsAndLoad(bool dynamicBid)
        {
            if (dynamicBid)
            {
                // As per MAX recommendations;
                // retry with exponentially higher delays up to 64s for ad Units with disabled auto retry.
                // In case you would like to customize fill rate / revenue please contact our customer support.
                yield return new WaitForSeconds(new [] { 0, 2, 4, 8, 16, 32, 64 }[Math.Min(_consecutiveDynamicBidAdFails, 6)]);
                GetInsightsAndLoad();
            }
            else
            {
                LoadDefault();
            }
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Rewarded,
                adUnitId == DynamicAdUnitId ? _dynamicAdUnitInsight : null, adInfo);

            if (adUnitId == DynamicAdUnitId)
            {
                SetStatus($"Loaded Dynamic {adUnitId} at: {adInfo.Revenue}");
                
                _consecutiveDynamicBidAdFails = 0;
                _dynamicAdState = AdState.Ready;
            }
            else
            {
                SetStatus($"Loaded Default {adUnitId} at: {adInfo.Revenue}");
                
                _defaultAdState = AdState.Ready;
            }

            UpdateShowButton();
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
            var wasShown = false;
            
            if (_dynamicAdState == AdState.Ready)
            {
                _dynamicAdState = AdState.None;
                if (MaxSdk.IsRewardedAdReady(DynamicAdUnitId))
                {
                    wasShown = true;
                    SetStatus("Showing DynamicAdUnit");
                    MaxSdk.ShowRewardedAd(DynamicAdUnitId);
                }
            }
            if (!wasShown && _defaultAdState == AdState.Ready)
            {
                _defaultAdState = AdState.None;
                if (MaxSdk.IsRewardedAdReady(DefaultAdUnitId))
                {
                    _defaultAdState = AdState.None;
                    SetStatus("Showing DefaultAdUnit");
                    MaxSdk.ShowRewardedAd(DefaultAdUnitId);
                }
            }

            UpdateShowButton();
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
            
            // restart new load cycle
            if (_load.isOn)
            {
                StartLoading();   
            }
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

        private void UpdateShowButton()
        {
            _show.interactable = _dynamicAdState == AdState.Ready || _defaultAdState == AdState.Ready;
        }

        private void AddDemoGameEventExample()
        {
            var category = (ResourceCategory) Random.Range(0, 9);
            var method = (SpendMethod)Random.Range(0, 8);
            var value = Random.Range(0, 101);
            new SpendEvent(category) { _method = method, _name = $"spend_{category} {method} {value}", _value = value }.Record();
        }
    }
}