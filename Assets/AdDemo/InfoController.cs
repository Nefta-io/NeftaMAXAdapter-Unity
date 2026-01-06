using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class InfoController : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private Button _titleButton;
        
        [SerializeField] private PlacementController _interstitialSim;
        [SerializeField] private PlacementController _rewardedSim;
        
        [SerializeField] private InterstitialController _interstitial;
        [SerializeField] private RewardedController _rewarded;
        
        private bool _isSimulator;

        private void Awake()
        {
            var demoConfig = Resources.Load<DemoConfig>("DemoConfig");
            if (demoConfig != null)
            {
                ToggleUI(demoConfig._isSimulator);
                _titleButton.onClick.AddListener(OnTitleClick);
            }
            
            _title.text = $"MAX Integration {MaxSdk.Version}";
        }

        private void OnTitleClick()
        {
            ToggleUI(!_isSimulator);
        }
        
        private void ToggleUI(bool isSimulator)
        {
            _isSimulator = isSimulator;
            
            _interstitialSim.gameObject.SetActive(isSimulator);
            _rewardedSim.gameObject.SetActive(isSimulator);
            
            _interstitial.gameObject.SetActive(!isSimulator);
            _rewarded.gameObject.SetActive(!isSimulator);
        }
    }
}