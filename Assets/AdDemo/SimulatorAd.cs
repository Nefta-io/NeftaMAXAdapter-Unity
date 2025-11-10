using System;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class SimulatorAd : MonoBehaviour
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private Text _title;
        [SerializeField] private Button _close;
        
        [SerializeField] private Button _ad;

        private Action _onShow;
        private Action _onReward;
        private float _time;
        
        public void Init(string title, Action onShow, Action onClick, Action onReward, Action onClose)
        {
            _title.text = title;

            _onShow = onShow;
            _onReward = onReward;
                
            _ad.onClick.AddListener(() =>
            {
                onClick();
            });
            _close.onClick.AddListener(() =>
            {
                Destroy(gameObject);
                onClose();
            });

            _time = 0f;
        }

        private void Start()
        {
            _onShow();
        }

        private void Update()
        {
            _time += Time.deltaTime;
            if (_time > 3f && _onReward != null)
            {
                _onReward();
                _onReward = null;
            }
        }
    }
}