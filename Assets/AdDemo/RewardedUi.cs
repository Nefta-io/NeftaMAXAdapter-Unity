using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class RewardedUi : MonoBehaviour
    {
#if UNITY_IOS
        public const string AdUnitIdA = "7c6097e4101586b0";
        public const string AdUnitIdB = "08304643cb16df3b";
#else // UNITY_ANDROID
        public const string AdUnitIdA = "c164298ebdd0c008";
        public const string AdUnitIdB = "3082ee9199cf59f0";
#endif
        private IRewarded _logic;
        
        [SerializeField] private Toggle _load;
        [SerializeField] private Text _status;
        [SerializeField] private Button _show;
        
        public bool IsAutoLoad { get; private set; }
        
        public void Init(IRewarded logic)
        {
            _load.onValueChanged.AddListener(OnLoadChanged);
            _show.interactable = false;
            _show.onClick.AddListener(OnShowClick);
            gameObject.SetActive(true);

            _logic = logic;
            _logic.Init(this);
        }
        
        public void OnAdLogicReady()
        {
            _load.interactable = true;
        }
        
        private void Update()
        {
            _logic.OnUpdate(Time.unscaledDeltaTime);
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
            Debug.Log($"[NeftaPluginMAX] Rewarded: {status}");
        }
    }
}