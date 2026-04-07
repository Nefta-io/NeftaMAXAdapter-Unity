using System;
using System.Threading.Tasks;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class InterstitialController : MonoBehaviour
    {
#if UNITY_IOS
        private const string AdUnitIdA = "78b66d4cd80ca1e7";
        private const string AdUnitIdB = "c9acf50602329bfe";
#else // UNITY_ANDROID
        private const string AdUnitIdA = "850bcc93f949090c";
        private const string AdUnitIdB = "60bbc7cc56dfa329";
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
            
            //MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
            //MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            //MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            //MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHiddenEvent;
            //MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            //MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnAdClickedEvent;

            NeftaSdk.Interstitial.InitializeDualTrack(AdUnitIdA, AdUnitIdB);
            NeftaSdk.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
            NeftaSdk.Interstitial.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            NeftaSdk.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            NeftaSdk.Interstitial.OnAdHiddenEvent += OnAdHiddenEvent;
        }

        private void Load()
        {
            //MaxSdk.LoadInterstitial(AdUnitIdA);
            NeftaSdk.LoadInterstitial(AdUnitIdA);
        }
        
        private void OnShowClick()
        {
            _show.interactable = false;
            
            //if (MaxSdk.IsInterstitialReady(AdUnitIdA))
            if (NeftaSdk.IsInterstitialReady(AdUnitIdA))
            {
                //MaxSdk.ShowInterstitial(AdUnitIdA);
                NeftaSdk.ShowInterstitial(AdUnitIdA);
            }
            else
            {
                Load();
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
            Debug.Log($"NeftaPluginMAX Interstitial: {status}");
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
            
            Debug.Log($"NeftaPluginMAX Interstitial OnAdRevenuePaidEvent: {adInfo.Revenue}");
        }

        // when implementing dual track manually (not using NeftaSDK wrapper)
        // you should forward ILRD event to the SDK manually
        private void OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationClick(adUnitId, adInfo);
            
            Debug.Log("NeftaPluginMAX Interstitial OnAdClickedEvent");
        }
    }
}