using System;
using System.Collections;
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
        private const string DefaultAdUnitId = "08304643cb16df3b";
#else // UNITY_ANDROID
        private const string DefaultAdUnitId = "3082ee9199cf59f0";
#endif
        private const int TimeoutInSeconds = 5;
        
        private string _selectedAdUnitId;
        private AdInsight _usedInsight;
        private int _consecutiveAdFails;
        private Queue<string> _statusQueue;
        private Action<bool> _onFullScreenAdDisplayed;
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private void GetInsightsAndLoad()
        {
            NeftaAdapterEvents.GetInsights(Insights.Rewarded, OnInsights, TimeoutInSeconds);
        }
        
        private void OnInsights(Insights insights)
        {
            _selectedAdUnitId = DefaultAdUnitId;
            _usedInsight = insights._rewarded;
            if (_usedInsight != null && _usedInsight._adUnit != null)
            {
                _selectedAdUnitId = _usedInsight._adUnit;
            }
            
            SetStatus($"Loading {_selectedAdUnitId} insights: {_usedInsight}");
            MaxSdk.SetRewardedAdExtraParameter(_selectedAdUnitId, "disable_auto_retries", "true");
            MaxSdk.LoadRewardedAd(_selectedAdUnitId);
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Rewarded, _usedInsight, adUnitId, errorInfo);

            SetStatus($"Load failed {adUnitId}: {errorInfo}");
            
            _consecutiveAdFails++;
            StartCoroutine(RetryLoadWithDelay());
        }
        
        private IEnumerator RetryLoadWithDelay()
        {
            // As per MAX recommendations, retry with exponentially higher delays up to 64s
            // In case you would like to customize fill rate / revenue please contact our customer support
            yield return new WaitForSeconds(new [] { 0, 2, 4, 8, 16, 32, 64 }[Math.Min(_consecutiveAdFails, 6)]);
            GetInsightsAndLoad();
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Rewarded, _usedInsight, adInfo);

            SetStatus($"Loaded {adUnitId} at: {adInfo.Revenue}");
            
            _consecutiveAdFails = 0;
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
            SetStatus("GetInsightsAndLoad...");
            GetInsightsAndLoad();
            AddDemoGameEventExample();
            _load.interactable = false;
        }
        
        private void OnShowClick()
        {
            if (MaxSdk.IsRewardedAdReady(_selectedAdUnitId))
            {
                SetStatus("Showing");
                MaxSdk.ShowRewardedAd(_selectedAdUnitId);
            }
            else
            {
                SetStatus("Ad not ready");
            }
            
            _show.interactable = false;
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
            _load.interactable = true;
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
            new SpendEvent(category) { _method = method, _name = $"spend_{category} {method} {value}", _value = value }.Record();
        }
    }
}