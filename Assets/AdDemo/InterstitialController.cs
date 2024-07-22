using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class InterstitialController : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private Button _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;

        private string _adUnitId;
        private Queue<string> _statusQueue;

        public void Init(string adUnitId)
        {
            _adUnitId = adUnitId;
            _statusQueue = new Queue<string>();
            
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaidEvent;
            
            _title.text = $"Interstitial {_adUnitId}";
            _load.onClick.AddListener(OnLoadClick);
            _show.onClick.AddListener(OnShowClick);
            
            _show.interactable = false;
        }

        private void OnLoadClick()
        {
            SetStatus("Loading...");
            MaxSdk.LoadInterstitial(_adUnitId);
        }
        
        private void OnShowClick()
        {
            _show.interactable = false;
            if (MaxSdk.IsInterstitialReady(_adUnitId))
            {
                SetStatus("Showing");
                MaxSdk.ShowInterstitial(_adUnitId);
            }
            else
            {
                SetStatus("Ad not ready");
            }
        }

        private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"Loaded {adInfo.NetworkName} {adInfo.NetworkPlacement}");
            _show.interactable = true;
        }

        private void OnInterstitialFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            SetStatus("Load failed");
        }
        
        private void InterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("Display failed");
        }
        
        private void OnInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("Hidden");
        }

        private void OnInterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            lock(_statusQueue)
            {
                _statusQueue.Enqueue($"Paid {adInfo.Revenue}");
            }
        }

        private void SetStatus(string status)
        {
            _status.text = status;
            Debug.Log($"AdUnit \"{_adUnitId}\": {status}");
        }
        
        private void Update()
        {
            lock (_statusQueue)
            {
                while (_statusQueue.Count > 0)
                {
                    var status = _statusQueue.Dequeue();
                    SetStatus(status);
                }
            }
        }
    }
}