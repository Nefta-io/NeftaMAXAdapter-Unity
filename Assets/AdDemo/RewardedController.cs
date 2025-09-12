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
        private const string DynamicAdUnitId = "7c6097e4101586b0";
        private const string DefaultAdUnitId = "08304643cb16df3b";
#else // UNITY_ANDROID
        private const string DynamicAdUnitId = "c164298ebdd0c008";
        private const string DefaultAdUnitId = "3082ee9199cf59f0";
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
        private Action<bool> _onFullScreenAdDisplayed;
        
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
            NeftaAdapterEvents.GetInsights(Insights.Rewarded, previousInsight, LoadWithInsights, TimeoutInSeconds);
        }
        
        private void LoadWithInsights(Insights insights)
        {
            _dynamicInsight = insights._rewarded;
            if (_dynamicInsight != null)
            {
                var bidFloor = _dynamicInsight._floorPrice.ToString(CultureInfo.InvariantCulture);
                
                SetStatus($"Loading Dynamic Rewarded with floor: {bidFloor}");
                MaxSdk.SetRewardedAdExtraParameter(DynamicAdUnitId, "disable_auto_retries", "true");
                MaxSdk.SetRewardedAdExtraParameter(DynamicAdUnitId, "jC7Fp", bidFloor);
                MaxSdk.LoadRewardedAd(DynamicAdUnitId);
                
                NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Rewarded, DynamicAdUnitId, _dynamicInsight);
            }
        }

        private void LoadDefault()
        {
            _defaultAdRequest = new AdRequest();
            SetStatus("Loading Default Rewarded");
            MaxSdk.LoadRewardedAd(DefaultAdUnitId);

            NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Rewarded, DefaultAdUnitId);
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
            // As per MAX recommendations;
            // retry with exponentially higher delays up to 64s for ad Units with disabled auto retry.
            // In case you would like to customize fill rate / revenue please contact our customer support.
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
        
        private void OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationClick(adUnitId, adInfo);
            
            SetStatus("OnAdClickedEvent");
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
            bool isShown = false;
            if (_dynamicAdRequest.Revenue >= 0)
            {
                if (_defaultAdRequest.Revenue > _dynamicAdRequest.Revenue)
                {
                    isShown = TryShowDefault();
                }
                if (!isShown)
                {
                    isShown = TryShowDynamic();
                }
            }
            if (!isShown && _defaultAdRequest.Revenue >= 0)
            {
                TryShowDefault();
            }
            
            UpdateShowButton();
        }

        private bool TryShowDynamic()
        {
            var isShown = false;
            if (MaxSdk.IsRewardedAdReady(DynamicAdUnitId))
            {
                SetStatus("Showing Dynamic Rewarded");
                MaxSdk.ShowRewardedAd(DynamicAdUnitId);
                isShown = true;
            }
            _dynamicAdRequest = null;
            return isShown;
        }
        
        private bool TryShowDefault()
        {
            var isShown = false;
            if (MaxSdk.IsRewardedAdReady(DefaultAdUnitId))
            {
                SetStatus("Showing Default Rewarded");
                MaxSdk.ShowRewardedAd(DefaultAdUnitId);
                isShown = true;
            }
            _defaultAdRequest = null;
            return isShown;
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
        
        private void OnAdHideEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdHideEvent");
            _onFullScreenAdDisplayed(false);
            
            // start new load cycle
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
                    Debug.Log($"NeftaPluginMAX Rewarded: {status}");
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
            var method = (SpendMethod)Random.Range(0, 8);
            var value = Random.Range(0, 101);
            new SpendEvent(category) { _method = method, _name = $"spend_{category} {method} {value}", _value = value }.Record();
        }
    }
}