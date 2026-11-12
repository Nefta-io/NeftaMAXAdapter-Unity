using System;
using System.Globalization;
using System.Reflection;
using AppsFlyerSDK;
using com.adjust.sdk;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class AdDemoController : MonoBehaviour
    {
        private const string _clientId = "5734465296924672";
#if UNITY_IOS
        private const string _neftaAppId = "5763106043068416";
        
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void CheckTrackingPermission();
#else // UNITY_ANDROID
        private const string _neftaAppId = "5693275310653440";
#endif
        private readonly string[] _optimizedAdUnits = new string[] {
            InterstitialUi.OptimizedAdUnitIdA, InterstitialUi.OptimizedAdUnitIdB,
            RewardedUi.OptimizedAdUnitIdA, RewardedUi.OptimizedAdUnitIdB,
        };
        
        private enum IntegrationType
        {
            Manual,
            Wrapper,
            Simulator
        }

        [SerializeField] private Text _title;
        [SerializeField] private Toggle _consetCheckBox;
        [SerializeField] private Button _miscButton;
        [SerializeField] private GameObject _integrationSelection;
        [SerializeField] private Button _integrationManualButton;
        [SerializeField] private Button _integrationWrapperButton;
        [SerializeField] private Button _integrationSimulatorButton;
        [SerializeField] private Button _optimizationButtonUnknown;
        [SerializeField] private Button _optimizationButtonOptimized;
        [SerializeField] private Button _optimizationButtonNotOptimized;
        
        [SerializeField] private InterstitialUi _interstitialUi;
        [SerializeField] private RewardedUi _rewardedUi;
        
        [SerializeField] private SimulatorUi _interstitialSimulator;
        [SerializeField] private SimulatorUi _rewardedSimulator;
        
        [SerializeField] private GameObject _miscPanel;
        [SerializeField] private Button _miscPanelButton;
        [SerializeField] private Button[] _miscButtons;
        [SerializeField] private GameObject _reward;
        [SerializeField] private Text _rewardText;
        [SerializeField] private Button _rewardOk;
        
        // When integrating Nefta chose either Manual or Wrapper
        private IntegrationType _integrationType;

        private static AdDemoController _instance;
        
        private void Awake()
        {
            _title.text = $"MAX Integration {MaxSdk.Version}";
            
            _consetCheckBox.onValueChanged.AddListener(OnConsentChange);
            _miscButton.onClick.AddListener(OnMiscClick);
            _integrationManualButton.onClick.AddListener(OnManualClick);
            _integrationWrapperButton.onClick.AddListener(OnWrapperClick);
            _integrationSimulatorButton.onClick.AddListener(OnSimulatorClick);
            _optimizationButtonUnknown.onClick.AddListener(OnUnknownClick);
            _optimizationButtonOptimized.onClick.AddListener(OnOptimizedClick);
            _optimizationButtonNotOptimized.onClick.AddListener(OnNotOptimizedClick);
            
            _miscPanelButton.onClick.AddListener(OnMiscPanelClick);
            for (var i = 0; i < _miscButtons.Length; i++)
            {
                var c = i;
                _miscButtons[i].onClick.AddListener(() => { OnMiscOption(c); });
            }
            
            _instance = this;
            _rewardOk.onClick.AddListener(OnRewardOkClick);
#if !UNITY_EDITOR && UNITY_IOS
            CheckTrackingPermission();
#endif
        }

        private void InitializeNefta(NeftaAdapterEvents.OptimizationType optimizationType)
        {
            NeftaAdapterEvents.EnableLogging(true);
            NeftaAdapterEvents.SetHasUserConsent(_consetCheckBox.isOn);
            NeftaAdapterEvents.InitWithAppId(_neftaAppId, optimizationType, (InitConfiguration config) =>
            {
                Debug.Log($"[NeftaPluginMAX] Nefta Initialized, nuid: {config._nuid} should optimize: {config._isSessionOptimized}");

                if (_integrationType == IntegrationType.Manual)
                {
                    if (config._isSessionOptimized)
                    {
                        var interstitialLogic = new InterstitialNeftaManual();
                        var rewardedLogic = new RewardedNeftaManual();
                        interstitialLogic.RewardedLogic = rewardedLogic;
                        rewardedLogic.InterstitialLogic = interstitialLogic;
                        _interstitialUi.Init(interstitialLogic);
                        _rewardedUi.Init(rewardedLogic);
                    }
                    else
                    {
                        _interstitialUi.Init(new InterstitialDefault());
                        _rewardedUi.Init(new RewardedDefault());
                    }
                }
                else if (_integrationType == IntegrationType.Wrapper)
                {
                    _interstitialUi.Init(new InterstitialNeftaWrapper(config._isSessionOptimized));
                    _rewardedUi.Init(new RewardedNeftaWrapper(config._isSessionOptimized));
                }
                else if (_integrationType == IntegrationType.Simulator)
                {
                    _interstitialSimulator.Init(config._isSessionOptimized);
                    _rewardedSimulator.Init(config._isSessionOptimized);
                }
            });
        }

        private void OnConsentChange(bool consent)
        {
            NeftaAdapterEvents.SetHasUserConsent(consent);
        }
        
        private void OnManualClick()
        {
            _integrationManualButton.interactable = false;
            _integrationWrapperButton.interactable = true;
            _integrationSimulatorButton.interactable = true;
            _integrationType = IntegrationType.Manual;   
        }
        
        private void OnWrapperClick()
        {
            _integrationManualButton.interactable = true;
            _integrationWrapperButton.interactable = false;
            _integrationSimulatorButton.interactable = true;
            _integrationType = IntegrationType.Wrapper;
        }
        
        private void OnSimulatorClick()
        {
            _integrationManualButton.interactable = true;
            _integrationWrapperButton.interactable = true;
            _integrationSimulatorButton.interactable = false;
            _integrationType = IntegrationType.Simulator;
        }
        
        private void OnUnknownClick()
        {
            Initialize(NeftaAdapterEvents.OptimizationType.Unknown);  
        }
        
        private void OnOptimizedClick()
        {
            Initialize(NeftaAdapterEvents.OptimizationType.Optimized);
        }
        
        private void OnNotOptimizedClick()
        {
            Initialize(NeftaAdapterEvents.OptimizationType.NotOptimized);
        }

        private void Initialize(NeftaAdapterEvents.OptimizationType optimizationType)
        {
            _integrationSelection.SetActive(false);
            
            InitializeNefta(optimizationType);
            
            if (_integrationType != IntegrationType.Simulator)
            {
                InitializeMAX();
            }
        }
        
        private void InitializeMAX()
        {
            Debug.Log("[NeftaPluginMAX] Initializing MAX");

            MaxSdk.SetVerboseLogging(true);
            MaxSdk.SetHasUserConsent(_consetCheckBox.isOn);
            MaxSdk.SetTestDeviceAdvertisingIdentifiers(new string[]
            {
                "6AE31431-72EA-44BD-9732-8159D827E21C",
                "B656BE16-9A12-4A0E-B160-DBEDFEC7F4C6",
                "97ec28e2-e65a-4fac-b11e-3975391f7cb7",
                "dca773a6-3445-4776-b361-4d950a0e212f"
            });
            
            MaxSdk.SetExtraParameter("disable_b2b_ad_unit_ids", string.Join(",", _optimizedAdUnits));   
                    
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                Debug.Log("MAX SDK Initialized");
            };
            MaxSdk.InitializeSdk();
        }
        
        private void OnMiscClick()
        {
            _miscPanel.SetActive(true);
        }

        private async void OnMiscOption(int i)
        {
            if (i == 0)
            {
                var a = GetAdjustAttribution();

                NeftaAdapterEvents.OnAttributionChanged(NeftaAdapterEvents.Mmp.Adjust, a.adid, a.network, a.campaign, a.costType, a.costAmount.GetValueOrDefault(), a.costCurrency);
            }
            else if (i == 1 || i == 2)
            {
                var data = AppsFlyer.CallbackStringToDictionary(GetAppsFlyerData(i == 2));

                var id = await AppsFlyer.getAppsFlyerUID();
                data.TryGetValue("media_source", out var network);
                data.TryGetValue("campaign", out var campaign);
                data.TryGetValue("af_cost_model", out var costType);
                var cost = 0.0;
                if (data.TryGetValue("cost_cents_USD", out var costObject))
                {
                    if (costObject is double)
                    {
                        cost = (double)costObject;
                    }
                    else
                    {
                        cost = Double.Parse((string)costObject, CultureInfo.InvariantCulture);   
                    }
                }
                NeftaAdapterEvents.OnAttributionChanged(NeftaAdapterEvents.Mmp.AppsFlyer, id, (string)network, (string)campaign, (string)costType, cost, "USD");

            }
            else if (i == 3)
            {
                NeftaAdapterEvents.OnPurchase("product 1", 2.57, "USD");
            }
            else if (i == 4)
            {
                NeftaAdapterEvents.OnPurchase("product 2", 1.23, "EUR");
            }
            else if (i == 5)
            {
                if (NeftaAdapterEvents.InitConfiguration != null)
                {
                    var type = typeof(NeftaAdapterEvents);
                    var method = type.GetMethod("IOnReady", BindingFlags.Static | BindingFlags.NonPublic);
                    method.Invoke(null, new object[] { "{\"field:3d }" });   
                }
            }
            else if (i == 6)
            {
                if (NeftaAdapterEvents.InitConfiguration != null)
                {
                    var type = typeof(NeftaAdapterEvents);
                    var method = type.GetMethod("OnExternalMediationImpression", BindingFlags.Static | BindingFlags.NonPublic);
                    method.Invoke(null, new object[] { false, "applovin-max", "{\"field\":- }", "id1", "id2" });
                }
            }
        }

        private void OnMiscPanelClick()
        {
            _miscPanel.SetActive(false);
        }

        private AdjustAttribution GetAdjustAttribution()
        {
            var data = new AdjustAttribution("{}");
            data.adid = "3fa82910b82f013b94cd001a5d28b17a";
            data.network = "test_network";
            data.campaign = "test_campaign";
            data.costType = "cpi";
            data.costAmount = 2.21;
            data.costCurrency = "USD";
            return data;
        }

        private string GetAppsFlyerData(bool isOrganic)
        {
            if (isOrganic) {
                return "{\"af_status\":\"Organic\",\"af_message\":\"organicinstall\",\"install_time\":\"2021-08-2306:59:51.194\"}";
            }
            return "{\"adgroup\":null,\"campaign\":\"campaign_name\",\"media_source\":\"applovin_int\",\"retargeting_conversion_type\":\"none\",\"adset_id\":null,\"campaign_id\":null,\"install_time\":\"2018-12-3023:49:54.186\",\"adgroup_id\":null,\"orig_cost\":\"0.0\",\"click_time\":\"2018-12-2414:29:33.738\",\"agency\":null,\"cost_cents_USD\":\"0\",\"af_ad_type\":\"INTER\",\"af_c_id\":\"b8******************************\",\"adset\":null,\"advertising_id\":\"50********************************\",\"af_sub1\":null,\"clickid\":\"10****************\",\"af_click_lookback\":\"7d\",\"af_siteid\":\"123_site_id\",\"af_ad\":\"ad_name\",\"http_referrer\":null,\"af_channel\":\"video\",\"af_adset\":\"_DEFAULT\",\"af_sub5\":null,\"af_sub4\":null,\"af_cpi\":null,\"af_sub3\":null,\"af_sub2\":null,\"af_status\":\"Non-organic\"}";
        }

        public static void ShowReward(string adUnitId)
        {
            _instance._reward.SetActive(true);
            _instance._rewardText.text = "Reward granted \n" + adUnitId;
        }

        public void OnRewardOkClick()
        {
            _reward.SetActive(false);
        }
    }
}
