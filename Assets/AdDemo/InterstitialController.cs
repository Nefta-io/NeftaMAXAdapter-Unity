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
        private const string DefaultAdUnitId = "c9acf50602329bfe";
#else // UNITY_ANDROID
        private const string DefaultAdUnitId = "60bbc7cc56dfa329";
#endif
        private const int TimeoutInSeconds = 5;
        
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
            NeftaAdapterEvents.GetInsights(Insights.Interstitial, Load, TimeoutInSeconds);
        }
        
        private void Load(Insights insights)
        {
            var bidFloor = "0";
            _usedInsight = insights._interstitial;
            if (_usedInsight != null)
            {
                bidFloor = _usedInsight._floorPrice.ToString(CultureInfo.InvariantCulture);
            }
            
            SetStatus($"Loading {DefaultAdUnitId} insights: {_usedInsight} with floor: {bidFloor}");
            MaxSdk.SetInterstitialExtraParameter(DefaultAdUnitId, "disable_auto_retries", "true");
            MaxSdk.SetRewardedAdExtraParameter(DefaultAdUnitId, "jC7Fp", bidFloor);
            MaxSdk.LoadInterstitial(DefaultAdUnitId);
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Interstitial, _usedInsight, adUnitId, errorInfo);
            
            SetStatus($"Load failed {adUnitId}: {errorInfo.Message}");

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
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Interstitial, _usedInsight, adInfo);
            
            SetStatus($"Loaded {adUnitId}: {adInfo.NetworkName}");

            _consecutiveAdFails = 0;
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
            SetStatus("GetInsightsAndLoad...");
            GetInsightsAndLoad();
            AddDemoGameEventExample();
            _load.interactable = false;
        }
        
        private void OnShowClick()
        {
            if (MaxSdk.IsInterstitialReady(DefaultAdUnitId))
            {
                SetStatus("Showing");
                MaxSdk.ShowInterstitial(DefaultAdUnitId);
            }
            else
            {
                SetStatus("Ad not ready");
            }
            
            _show.interactable = false;
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
            _load.interactable = true;
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

        private void AddDemoGameEventExample()
        {
            var category = (ResourceCategory) Random.Range(0, 9);
            var method = (ReceiveMethod)Random.Range(0, 8);
            var value = Random.Range(0, 101);
            new ReceiveEvent(category) { _method = method, _name = $"receive_{category} {method} {value}", _value = value }.Record();
        }
    }
}