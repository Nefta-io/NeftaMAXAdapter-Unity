using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class InfoController : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private Button _titleButton;
        
        [SerializeField] private SimulatorController _interstitialSim;
        [SerializeField] private SimulatorController _rewardedSim;
        
        [SerializeField] private InterstitialController _interstitial;
        [SerializeField] private RewardedController _rewarded;
        
        private bool _isSimulator;
        private InterstitialLogic _defaultInterstitialLogic;
        private RewardedLogic _defaultRewardedLogic;

        private void Awake()
        {
            _title.text = $"MAX Integration {MaxSdk.Version}";
        }

        private void Start()
        {
            _interstitialSim.Init();
            _rewardedSim.Init();
            
            _defaultInterstitialLogic = NeftaSdk.Interstitial;
            _defaultRewardedLogic = NeftaSdk.Rewarded;
            
            var demoConfig = Resources.Load<DemoConfig>("DemoConfig");
            if (demoConfig != null)
            {
                ToggleUI(demoConfig._isSimulator);
                _titleButton.onClick.AddListener(OnTitleClick);
            }
        }

        private void OnTitleClick()
        {
            ToggleUI(!_isSimulator);
        }
        
        private void ToggleUI(bool isSimulator)
        {
            _isSimulator = isSimulator;
            NeftaSdk.Interstitial = isSimulator ? (InterstitialLogic)_interstitialSim.AdLogic : _defaultInterstitialLogic;
            NeftaSdk.Rewarded = isSimulator ? (RewardedLogic)_rewardedSim.AdLogic : _defaultRewardedLogic;
            
            _interstitialSim.gameObject.SetActive(isSimulator);
            _rewardedSim.gameObject.SetActive(isSimulator);
            
            _interstitial.gameObject.SetActive(!isSimulator);
            _rewarded.gameObject.SetActive(!isSimulator);
        }
    }
}