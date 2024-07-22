using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class RewardedController : MonoBehaviour
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
            
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHideEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;

            _title.text = $"Rewarded {_adUnitId}";
            _load.onClick.AddListener(OnLoadClick);
            _show.onClick.AddListener(OnShowClick);

            _show.interactable = false;
        }
        
        private void OnLoadClick()
        {
            SetStatus("Loading...");
            MaxSdk.LoadRewardedAd(_adUnitId);
        }
        
        private void OnShowClick()
        {
            _show.interactable = false;
            if (MaxSdk.IsRewardedAdReady(_adUnitId))
            {
                SetStatus("Showing");
                MaxSdk.ShowRewardedAd(_adUnitId);
            }
            else
            {
                SetStatus("Ad not ready");
            }
        }
        
        private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"Loaded {adInfo.NetworkName} {adInfo.NetworkPlacement}");
            _show.interactable = true;
        }
        
        private void OnRewardedAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            SetStatus("Load failed");
        }
        
        private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("Display failed");
        }
        
        private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("Displayed");
        }
        
        private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("Rewarded ad clicked");
        }
        
        private void OnRewardedAdHideEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("Hidden");
        }
        
        private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("Rewarded ad received reward");
        }
        
        private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            lock (_statusQueue)
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