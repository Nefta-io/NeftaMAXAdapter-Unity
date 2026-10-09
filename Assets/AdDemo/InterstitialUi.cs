using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class InterstitialUi : MonoBehaviour
    {
#if UNITY_IOS
        public const string DefaultAdUnitId = "0u6jgm23ggqso85n";
        public const string OptimizedAdUnitIdA = "78b66d4cd80ca1e7";
        public const string OptimizedAdUnitIdB = "c9acf50602329bfe";
#else // UNITY_ANDROID
        public const string DefaultAdUnitId = "wrzl86if1sqfxquc";
        public const string OptimizedAdUnitIdA = "850bcc93f949090c";
        public const string OptimizedAdUnitIdB = "60bbc7cc56dfa329";
#endif
        private IInterstitial _logic;
        
        [SerializeField] private Toggle _load;
        [SerializeField] private Text _status;
        [SerializeField] private Button _show;
        
        public bool IsAutoLoad { get; private set; }
        
        public void Init(IInterstitial logic)
        {
            _load.onValueChanged.AddListener(OnLoadChanged);
            _show.interactable = false;
            _show.onClick.AddListener(OnShowClick);
            gameObject.SetActive(true);

            _logic = logic;
            _logic.Init(this);
        }
        
        private void OnLoadChanged(bool isOn)
        {
            IsAutoLoad = isOn;
            if (IsAutoLoad)
            {
                _logic.Load();   
            }
        }

        public void SetAvailability(bool isAvailable)
        {
            _show.interactable = isAvailable;
        }
        
        private void OnShowClick()
        {
            _logic.Show();
        }
        
        public void SetStatus(string status)
        {
            _status.text = status;
            Debug.Log($"[NeftaPluginMAX] Interstitial: {status}");
        }
    }
}