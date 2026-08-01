using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class AdDemoController : MonoBehaviour
    {
#if UNITY_IOS
        private const string _neftaAppId = "5763106043068416";
        
        private readonly string[] _adUnits = new string[] {
            // interstitials
            "78b66d4cd80ca1e7",
            "c9acf50602329bfe",
            // rewarded
            "7c6097e4101586b0",
            "08304643cb16df3b"
        };
#else // UNITY_ANDROID
        private const string _neftaAppId = "5693275310653440";

        private readonly string[] _adUnits = new string[] {
            // interstitials
            "850bcc93f949090c",
            "60bbc7cc56dfa329",
            // rewarded
            "c164298ebdd0c008",
            "3082ee9199cf59f0"
        };
#endif
        private enum IntegrationType
        {
            Manual,
            Wrapper,
            Simulator
        }

        [SerializeField] private Text _title;
        [SerializeField] private Toggle _consetCheckBox;
        [SerializeField] private GameObject _integrationSelection;
        [SerializeField] private Button _integrationManualButton;
        [SerializeField] private Button _integrationWrapperButton;
        [SerializeField] private Button _integrationSimulatorButton;
        [SerializeField] private Button _groupDefaultButton;
        [SerializeField] private Button _groupOptimizedButton;
        
        [SerializeField] private InterstitialUi _interstitialUi;
        [SerializeField] private RewardedUi _rewardedUi;
        
        [SerializeField] private SimulatorUi _interstitialSimulator;
        [SerializeField] private SimulatorUi _rewardedSimulator;
        
        // When integrating Nefta chose either Manual or Wrapper
        private IntegrationType _integrationType;
        private bool _isMaxReady;
        private bool _isNeftaReady;
        
        private void Awake()
        {
            _title.text = $"MAX Integration {MaxSdk.Version}";
            
            _consetCheckBox.onValueChanged.AddListener(OnConsentChange);
            _integrationManualButton.onClick.AddListener(OnManualClick);
            _integrationWrapperButton.onClick.AddListener(OnWrapperClick);
            _integrationSimulatorButton.onClick.AddListener(OnSimulatorClick);
            _groupDefaultButton.onClick.AddListener(OnDefaultClick);
            _groupOptimizedButton.onClick.AddListener(OnOptimizedClick);
        }

        private void InitializeNefta()
        {
            NeftaAdapterEvents.EnableLogging(true);
            NeftaAdapterEvents.InitWithAppId(_neftaAppId, (InitConfiguration config) =>
            {
                Debug.Log($"[NeftaPluginMAX] Nefta Initialized, nuid: {config._nuid}");
                _isNeftaReady = true;
                OnAdLogicReady();
            });
        }

        private void OnConsentChange(bool consent)
        {
            NeftaAdapterEvents.SetHasUserConsent(consent);
            _consetCheckBox.interactable = false;
        }

        private void OnAdLogicReady()
        {
            if (_isMaxReady && _isNeftaReady)
            {
                _interstitialUi.OnAdLogicReady();
                _rewardedUi.OnAdLogicReady();
                
                _interstitialSimulator.OnAdLogicReady();
                _rewardedSimulator.OnAdLogicReady();
            }
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
        
        private void OnDefaultClick()
        {
            Initialize(false);
        }
        
        private void OnOptimizedClick()
        {
            Initialize(true);
        }

        private void Initialize(bool isOptimized)
        {
            InitializeNefta();
            _integrationSelection.SetActive(false);
            
            if (_integrationType == IntegrationType.Simulator)
            {
                _isMaxReady = true;
                
                _interstitialSimulator.Init(isOptimized);
                _rewardedSimulator.Init(isOptimized);
            }
            else
            {
                InitializeMAX(true);   
            }
        }
        
        private void InitializeMAX(bool isOptimized)
        {
            MaxSdk.SetVerboseLogging(true);
            MaxSdk.SetTestDeviceAdvertisingIdentifiers(new string[]
            {
                "6AE31431-72EA-44BD-9732-8159D827E21C",
                "B656BE16-9A12-4A0E-B160-DBEDFEC7F4C6",
                "97ec28e2-e65a-4fac-b11e-3975391f7cb7",
                "dca773a6-3445-4776-b361-4d950a0e212f"
            });
            
            if (isOptimized)
            {
                MaxSdk.SetExtraParameter("disable_b2b_ad_unit_ids", string.Join(",", _adUnits));   
            }
                    
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                Debug.Log("MAX SDK Initialized");
                _isMaxReady = true;
                OnAdLogicReady();
            };
            MaxSdk.InitializeSdk();

            if (_integrationType == IntegrationType.Manual)
            {
                NeftaAdapterEvents.SetInterstitialLogic(isOptimized);
                NeftaAdapterEvents.SetRewardedLogic(isOptimized);
                if (isOptimized)
                {
                    _interstitialUi.Init(new InterstitialNeftaManual());
                    _rewardedUi.Init(new RewardedNeftaManual());
                }
                else
                {
                    _interstitialUi.Init(new InterstitialDefault());
                    _rewardedUi.Init(new RewardedDefault());
                }
            }
            else if (_integrationType == IntegrationType.Wrapper)
            {
                _interstitialUi.Init(new InterstitialNeftaWrapper(isOptimized));
                _rewardedUi.Init(new RewardedNeftaWrapper(isOptimized));
            }
        }
    }
}
