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
        
        // When integrating Nefta chose either Manual or Wrapper
        private IntegrationType _integrationType;
        
        private void Awake()
        {
            _title.text = $"MAX Integration {MaxSdk.Version}";
            
            _consetCheckBox.onValueChanged.AddListener(OnConsentChange);
            _integrationManualButton.onClick.AddListener(OnManualClick);
            _integrationWrapperButton.onClick.AddListener(OnWrapperClick);
            _integrationSimulatorButton.onClick.AddListener(OnSimulatorClick);
            _optimizationButtonUnknown.onClick.AddListener(OnUnknownClick);
            _optimizationButtonOptimized.onClick.AddListener(OnOptimizedClick);
            _optimizationButtonNotOptimized.onClick.AddListener(OnNotOptimizedClick);
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
    }
}
