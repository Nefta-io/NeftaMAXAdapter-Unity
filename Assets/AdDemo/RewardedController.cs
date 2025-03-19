using System;
using System.Collections.Generic;
using Nefta.Core.Events;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace AdDemo
{
    public class RewardedController : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private Button _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;

        private Rewarded _rewarded;
        private Queue<string> _statusQueue;
        
        public void Init(List<AdConfig> adUnits, Action getInsights)
        {
            _statusQueue = new Queue<string>();

            _rewarded = new Rewarded("calculated_user_floor_price_rewarded", adUnits, getInsights, SetStatus, OnLoad);

            _title.text = "Rewarded";
            _load.onClick.AddListener(OnLoadClick);
            _show.onClick.AddListener(OnShowClick);

            _show.interactable = false;
        }
        
        private void OnLoadClick()
        {
            var category = (ResourceCategory) Random.Range(0, 9);
            var method = (SpendMethod)Random.Range(0, 8);
            var value = Random.Range(0, 101);
            NeftaAdapterEvents.Record(new SpendEvent(category) { _method = method, _name = $"spend_{category} {method} {value}", _value = value });
            
            SetStatus("Loading...");
            _rewarded.Load();
        }

        private void OnLoad()
        {
            _show.interactable = true;
        }
        
        private void OnShowClick()
        {
            _show.interactable = false;
            _rewarded.Show();
        }
        
        private void SetStatus(string status)
        {
            lock (_statusQueue)
            {
                _statusQueue.Enqueue(status);
            }
        }
        
        private void Update()
        {
            if (_statusQueue == null)
            {
                return;
            }
            
            lock (_statusQueue)
            {
                while (_statusQueue.Count > 0)
                {
                    var status = _statusQueue.Dequeue();
                    
                    _status.text = status;
                    Debug.Log($"Rewarded: {status}");
                }
            }
        }
        
        public void OnBehaviourInsight(Dictionary<string, Insight> behaviourInsight)
        {
            _rewarded.OnBehaviourInsight(behaviourInsight);
        }
    }
}