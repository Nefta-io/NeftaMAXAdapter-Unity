using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_IOS
using AOT;
using System.Runtime.InteropServices;
#endif

namespace AdDemo
{
    public class SimulatorController : MonoBehaviour
    {
#if UNITY_IOS
        private delegate void OnCallback();
        
        [MonoPInvokeCallback(typeof(OnCallback))] 
        private static void OnShowBridge() { _actions.Enqueue(OnShow); }

        [MonoPInvokeCallback(typeof(OnCallback))] 
        private static void OnClickBridge() { _actions.Enqueue(OnClick); }

        [MonoPInvokeCallback(typeof(OnCallback))] 
        private static void OnRewardBridge() { _actions.Enqueue(OnReward); }

        [MonoPInvokeCallback(typeof(OnCallback))] 
        private static void OnCloseBridge() { _actions.Enqueue(OnClose); }
        
        [DllImport ("__Internal")]
        private static extern void NDebug_Open(string title, OnCallback onShow, OnCallback onClick, OnCallback onReward, OnCallback onClose);
#elif UNITY_ANDROID
        private class AdCallback : AndroidJavaProxy
        {
            public Action _onShow;
            public Action _onClick;
            public Action _onReward;
            public Action _onClose;
            
            public AdCallback() : base("com.nefta.debug.Callback") { }

            public void onShow() { _actions.Enqueue(_onShow); }
            public void onClick() { _actions.Enqueue(_onClick); }
            public void onReward() { _actions.Enqueue(_onReward); }
            public void onClose() { _actions.Enqueue(_onClose); }
        }
#endif
        
        [Header("Controls")]
        [SerializeField] private RectTransform _rootRect;
        [SerializeField] private bool _isRewarded;
        [SerializeField] private Toggle _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        [Header("Track A")]
        [SerializeField] private Image _aFill2Renderer;
        [SerializeField] private Button _aFill2;
        [SerializeField] private Image _aFill1Renderer;
        [SerializeField] private Button _aFill1;
        [SerializeField] private Image _aNoFillRenderer;
        [SerializeField] private Button _aNoFill;
        [SerializeField] private Image _aOtherRenderer;
        [SerializeField] private Button _aOther;
        [SerializeField] private Text _aStatus;
        
        [Header("Track B")]
        [SerializeField] private Image _bFill2Renderer;
        [SerializeField] private Button _bFill2;
        [SerializeField] private Image _bFill1Renderer;
        [SerializeField] private Button _bFill1;
        [SerializeField] private Image _bNoFillRenderer;
        [SerializeField] private Button _bNoFill;
        [SerializeField] private Image _bOtherRenderer;
        [SerializeField] private Button _bOther;
        [SerializeField] private Text _bStatus;
        
        private bool _isAutoLoad;

        [NonSerialized] public AdLogic AdLogic;
        
        public void Init()
        {
            if (_isRewarded)
            {
                AdLogic = new SimulatorRewardedLogic(
                    _aFill2Renderer, _aFill2, _aFill1Renderer, _aFill1, _aNoFillRenderer, _aNoFill,
                    _aOtherRenderer, _aOther, _aStatus,
                    _bFill2Renderer, _bFill2, _bFill1Renderer, _bFill1, _bNoFillRenderer, _bNoFill,
                    _bOtherRenderer, _bOther, _bStatus);
            }
            else
            {
                AdLogic = new SimulatorInterstitialLogic(
                    _aFill2Renderer, _aFill2, _aFill1Renderer, _aFill1, _aNoFillRenderer, _aNoFill,
                    _aOtherRenderer, _aOther, _aStatus,
                    _bFill2Renderer, _bFill2, _bFill1Renderer, _bFill1, _bNoFillRenderer, _bNoFill,
                    _bOtherRenderer, _bOther, _bStatus);
            }
            AdLogic.OnAdLoadedEvent += OnAdLoadedEvent;
            AdLogic.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            AdLogic.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            AdLogic.OnAdHiddenEvent += OnAdHiddenEvent;
            
            _load.onValueChanged.AddListener(OnLoadChanged);
            _show.onClick.AddListener(OnShowClick);
            UpdateShowButton();
        }
        
        private void Load()
        {
            if (_isRewarded)
            {
                NeftaSdk.LoadRewardedAd("rewarded1");
            }
            else
            {
                NeftaSdk.LoadInterstitial("inter1");
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
        
        private void OnShowClick()
        {
            if (_isRewarded)
            {
                if (NeftaSdk.IsRewardedAdReady("rewarded1"))
                {
                    NeftaSdk.ShowRewardedAd("rewarded1");   
                }
                else
                {
                    Load();
                }
            }
            else
            {
                if (NeftaSdk.IsInterstitialReady("interstitial1"))
                {
                    NeftaSdk.ShowInterstitial("interstitial1");
                }
                else
                {
                    Load();
                }
            }
            UpdateShowButton();
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"Loaded {adUnitId} at: {adInfo.Revenue}");

            UpdateShowButton();
        }
        
        private void OnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            SetStatus($"Load failed {adUnitId} with: {errorInfo}");
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdDisplayFailedEvent");
            
            if (_isAutoLoad)
            {
                Load();
            }
        }
        
        private void OnRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"OnRevenuePaidEvent {adInfo.Revenue}");
        }
        
        private void OnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdReceivedRewardEvent");
        }
        
        private void OnAdClickEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"On Ad clicked {adUnitId}");
        }
        
        private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdHideEvent");

            if (_isAutoLoad)
            {
                Load();
            }
        }

        private void UpdateShowButton()
        {
            _show.interactable = AdLogic.IsAdReady();
        }
        
        private void SetStatus(string status)
        {
            _status.text = status;
            Debug.Log($"NeftaPluginMAX Simulator: {status}");
        }
        
        public static void ShowAd(string title, Action onShow, Action onClick, Action onReward, Action onClose)
        {
            OnShow = onShow;
            OnClick = onClick;
            OnReward = onReward;
            OnClose = onClose;
#if UNITY_EDITOR
            OnShow();
            _ = CloseAfterDelay();
#elif UNITY_IOS
            NDebug_Open(title, OnShowBridge, OnClickBridge, OnRewardBridge, OnCloseBridge);
#elif UNITY_ANDROID
            var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"); 

            var debugClass = new AndroidJavaClass("com.nefta.debug.NDebug");
            debugClass.CallStatic("Open", title, unityActivity, new AdCallback { _onShow = onShow, _onClick = onClick, _onReward = onReward, _onClose = onClose });
#endif
        }

        private static async Task CloseAfterDelay()
        {
            await Task.Delay(100);
            OnClick();
            OnClose();
        }
        
        private static Action OnShow;
        private static Action OnClick;
        private static Action OnReward;
        private static Action OnClose;
        private static readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();

        private void Update()
        {
            while (_actions.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }
    }
}