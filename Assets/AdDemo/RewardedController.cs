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
        private const string AdUnitIdA = "7c6097e4101586b0";
        private const string AdUnitIdB = "08304643cb16df3b";
#else // UNITY_ANDROID
        private const string AdUnitIdA = "c164298ebdd0c008";
        private const string AdUnitIdB = "3082ee9199cf59f0";
#endif
        private const int TimeoutInSeconds = 5;

        private enum State
        {
            Idle,
            LoadingWithInsights,
            Loading,
            Ready
        }
                
        private class AdRequest
        {
            public readonly string AdUnitId;
            public State State;
            public AdInsight Insight;
            public double Revenue;
            public int ConsecutiveAdFails;

            public AdRequest(string adUnitId)
            {
                AdUnitId = adUnitId;
            }
        }

        private AdRequest _adRequestA;
        private AdRequest _adRequestB;
        private bool _isFirstResponseReceived;

        private Queue<string> _statusQueue;
        
        [SerializeField] private Toggle _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private void StartLoading()
        {
            Load(_adRequestA, _adRequestB.State);
            Load(_adRequestB, _adRequestA.State);
        }
        
        private void Load(AdRequest request, State otherState)
        {
            if (request.State == State.Idle)
            {
                if (otherState != State.LoadingWithInsights)
                {
                    GetInsightsAndLoad(request); 
                }
                else if (_isFirstResponseReceived)
                {
                    LoadDefault(request);
                }
            }
        }

        private void GetInsightsAndLoad(AdRequest adRequest)
        {
            adRequest.State = State.LoadingWithInsights;
               
            NeftaAdapterEvents.GetInsights(Insights.Rewarded, adRequest.Insight, (Insights insights) => {
                var insight = insights._rewarded;
                SetStatus($"Load with Insights: {insight}");
                if (insight != null && _load.isOn)
                {
                    adRequest.Insight = insight;
                    var bidFloor = insight._floorPrice.ToString(CultureInfo.InvariantCulture);
                    MaxSdk.SetRewardedAdExtraParameter(adRequest.AdUnitId, "disable_auto_retries", "true");
                    MaxSdk.SetRewardedAdExtraParameter(adRequest.AdUnitId, "jC7Fp", bidFloor);
                    
                    NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Rewarded, adRequest.AdUnitId, insight);
                    
                    SetStatus($"Loading {adRequest.AdUnitId} as Optimized with floor: {bidFloor}");
                    MaxSdk.LoadRewardedAd(adRequest.AdUnitId);
                }
            }, TimeoutInSeconds);
        }

        private void LoadDefault(AdRequest adRequest)
        {
            adRequest.State = State.Loading;
                        
            SetStatus($"Loading {adRequest.AdUnitId} as Default");
            
            NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Rewarded, adRequest.AdUnitId);
            
            MaxSdk.LoadRewardedAd(adRequest.AdUnitId);
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(adUnitId, errorInfo);
            
            var adRequest = adUnitId == _adRequestA.AdUnitId ? _adRequestA : _adRequestB;
            SetStatus($"Load Failed {adRequest.AdUnitId}: {errorInfo}");
            
            _isFirstResponseReceived = true;
            adRequest.ConsecutiveAdFails++;
            
            StartLoading();
            
            StartCoroutine(RetryGetInsightsAndLoad(adRequest));
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(adInfo);
            
            var adRequest = adUnitId == _adRequestA.AdUnitId ? _adRequestA : _adRequestB;
            SetStatus($"Loaded {adRequest.AdUnitId} at: {adInfo.Revenue}");

            _isFirstResponseReceived = true;
            adRequest.Insight = null;
            adRequest.ConsecutiveAdFails = 0;
            adRequest.Revenue = adInfo.Revenue;
            adRequest.State = State.Ready;

            UpdateShowButton();
            
            StartLoading();
        }
        
        private IEnumerator RetryGetInsightsAndLoad(AdRequest adRequest)
        {
            yield return new WaitForSeconds(GetMinWaitTime(adRequest.ConsecutiveAdFails));

            adRequest.State = State.Idle;
            if (_load.isOn)
            {
                StartLoading();
            }
        }

        private float GetMinWaitTime(int numberOfConsecutiveFails)
        {
            // As per MAX recommendations, retry with exponentially higher delays up to 64s
            // In case you would like to customize fill rate / revenue please contact our customer support
            return new [] { 0, 2, 4, 8, 16, 32, 64 }[Math.Min(numberOfConsecutiveFails, 6)];
        }
        
        private void Awake()
        {
            _adRequestA = new AdRequest(AdUnitIdA);
            _adRequestB = new AdRequest(AdUnitIdB);
                        
            _statusQueue = new Queue<string>();
            
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnAdFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnAdHideEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            
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
            var isShown = false;
            if (_adRequestA.State == State.Ready)
            {
                if (_adRequestB.State == State.Ready && _adRequestB.Revenue > _adRequestA.Revenue)
                {
                    isShown = TryShow(_adRequestB);
                }
                if (!isShown)
                {
                    isShown = TryShow(_adRequestA);
                }
            }
            if (!isShown && _adRequestB.State == State.Ready)
            {
                TryShow(_adRequestB);
            }
            
            UpdateShowButton();
        }

        private bool TryShow(AdRequest adRequest)
        {
            adRequest.State = State.Idle;
            adRequest.Revenue = 0;
            
            if (MaxSdk.IsRewardedAdReady(adRequest.AdUnitId))
            {
                SetStatus($"Showing {adRequest.AdUnitId}");
                MaxSdk.ShowRewardedAd(adRequest.AdUnitId);
                return true;
            }
            return false;
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdDisplayFailedEvent");
        }
        
        private void OnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdDisplayedEvent");
        }
        
        private void OnAdHideEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdHideEvent");
            
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
        
        private void OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdClickedEvent");
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
            _show.interactable = _adRequestA.State == State.Ready || _adRequestB.State == State.Ready;
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