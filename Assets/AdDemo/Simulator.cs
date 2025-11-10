using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class Simulator : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        
        [Header("Interstitial")]
        [SerializeField] private GameObject _interstitialController;
        [SerializeField] private PlacementController _interstitialSimulatorController;
        
        [Header("Rewarded")]
        [SerializeField] private GameObject _rewardedController;
        [SerializeField] private PlacementController _rewardedSimulatorController;

        private void Awake()
        {
            _toggle.onValueChanged.AddListener(OnSimulationModeChanged);
            _toggle.isOn = false;
        }

        private void OnSimulationModeChanged(bool isOn)
        {
            _interstitialController.SetActive(!isOn);
            _interstitialSimulatorController.gameObject.SetActive(isOn);
            
            _rewardedController.SetActive(!isOn);
            _rewardedSimulatorController.gameObject.SetActive(isOn);
        }
    }
}