using System;
using System.Threading.Tasks;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;

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
        private int _consecutiveAdFails;
        private bool _isAutoLoad;
        
        [SerializeField] private Toggle _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private void Start()
        {
            _load.onValueChanged.AddListener(OnLoadChanged);
            
            _show.interactable = false;
            _show.onClick.AddListener(OnShowClick);
            
            //MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnAdLoadedEvent;
            //MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            //MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            //MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnAdHiddenEvent;
            //MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedRewardEvent;
            //MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            //MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnAdClickedEvent;
            
            NeftaSdk.Rewarded.InitializeDualTrack(AdUnitIdA, AdUnitIdB);
            NeftaSdk.Rewarded.OnAdLoadedEvent += OnAdLoadedEvent;
            NeftaSdk.Rewarded.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            NeftaSdk.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            NeftaSdk.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedRewardEvent;
            NeftaSdk.Rewarded.OnAdHiddenEvent += OnAdHiddenEvent;
        }

        private void Load()
        {
            //MaxSdk.LoadRewardedAd(AdUnitIdA);
            NeftaSdk.LoadRewardedAd(AdUnitIdA);
        }
        
        private void OnShowClick()
        {
            _show.interactable = false;
            //if (MaxSdk.IsRewardedAdReady(AdUnitIdA))
            if (NeftaSdk.IsRewardedAdReady(AdUnitIdA))
            {
                //MaxSdk.ShowRewardedAd(AdUnitIdA);
                NeftaSdk.ShowRewardedAd(AdUnitIdA);
            }
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"Loaded {adUnitId} at: {adInfo.Revenue}");

            _consecutiveAdFails = 0;
            
            _show.interactable = true;
        }
        
        private void OnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            SetStatus($"Load failed {adUnitId} with: {errorInfo}");
            
            _consecutiveAdFails++;
            _ = LoadWithDelay();
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            _load.interactable = true;
            
            SetStatus("OnAdDisplayFailedEvent");
            
            if (_isAutoLoad)
            {
                Load();
            }
        }
        
        private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _load.interactable = true;
            
            SetStatus("OnAdHideEvent");

            if (_isAutoLoad)
            {
                Load();
            }
        }
        
        private void OnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdReceivedRewardEvent");
        }
        
        private void OnLoadChanged(bool isOn)
        {
            _isAutoLoad = isOn;
            if (_isAutoLoad)
            {
                Load();   
            }
        }
        
        private void SetStatus(string status)
        {
            _status.text = status;
            Debug.Log($"NeftaPluginMAX Rewarded: {status}");
        }
        
        private async Task LoadWithDelay()
        {
            var delay = new[] { 0, 2, 4, 8, 16, 32, 64 }[Math.Min(_consecutiveAdFails, 6)];
            await Task.Delay(delay * 1000);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            Load();
        }
        
        // when implementing dual track manually (not using NeftaSDK wrapper)
        // you should forward ILRD event to the SDK manually
        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationImpression(adUnitId, adInfo);
            
            Debug.Log($"NeftaPluginMAX Rewarded OnAdRevenuePaidEvent: {adInfo.Revenue}");
        }

        // when implementing dual track manually (not using NeftaSDK wrapper)
        // you should forward ILRD event to the SDK manually
        private void OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationClick(adUnitId, adInfo);
            
            Debug.Log("NeftaPluginMAX Rewarded OnAdClickedEvent");
        }
    }
}