using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class BannerController : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private Button _show;
        [SerializeField] private Button _hide;
        [SerializeField] private Text _status;
        
        private string _adUnitId;

        public void Init(string adUnitId)
        {
            _adUnitId = adUnitId;
            
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;

            MaxSdk.CreateBanner(_adUnitId, MaxSdkBase.BannerPosition.TopCenter);

            _title.text = $"Banner {_adUnitId}";
            _show.onClick.AddListener(OnShowClick);
            _hide.onClick.AddListener(OnHideClick);
            _hide.interactable = false;
        }
        
        private void OnShowClick()
        {
            MaxSdk.ShowBanner(_adUnitId);
            _hide.interactable = true;
        }
        
        private void OnHideClick()
        {
            _hide.interactable = false;
            MaxSdk.HideBanner(_adUnitId);
        }
        
        private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"Loaded {adInfo.NetworkName} {adInfo.NetworkPlacement}");
        }
        
        private void OnBannerAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            SetStatus("Load failed");
        }

        private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("Clicked");
        }

        private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"Paid {adInfo.Revenue}");
        }
        
        private void SetStatus(string status)
        {
            _status.text = status;
            Debug.Log($"AdUnit \"{_adUnitId}\": {status}");
        }
    }
}