using System;
using System.Collections.Generic;
using Nefta.Core.Events;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace AdDemo
{
    public class InterstitialController : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private Button _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;

        private Interstitial _interstitial;
        private Queue<string> _statusQueue;

        public void Init(List<AdConfig> adUnits, Action getInsights)
        {
            _statusQueue = new Queue<string>();
            
            _interstitial = new Interstitial("calculated_user_floor_price_interstitial", adUnits, getInsights, SetStatus, OnLoad);
            
            _title.text = "Interstitial";
            _load.onClick.AddListener(OnLoadClick);
            _show.onClick.AddListener(OnShowClick);
            
            _show.interactable = false;
        }

        private void OnLoadClick()
        {
            var category = (ResourceCategory) Random.Range(0, 9);
            var method = (ReceiveMethod)Random.Range(0, 8);
            var value = Random.Range(0, 101);
            NeftaAdapterEvents.Record(new ReceiveEvent(category) { _method = method, _name = $"receive_{category} {method} {value}", _value = value });

            _interstitial.Load();
            SetStatus("Loading...");
        }

        private void OnLoad()
        {
            _show.interactable = true;
        }
        
        private void OnShowClick()
        {
            _show.interactable = false;
            _interstitial.Show();
        }
        
        private void SetStatus(string status)
        {
            lock(_statusQueue)
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
                    Debug.Log($"Interstitial: {status}");
                }
            }
        }
        
        public void OnBehaviourInsight(Dictionary<string, Insight> behaviourInsight)
        {
            _interstitial.OnBehaviourInsight(behaviourInsight);
        }
    }
}