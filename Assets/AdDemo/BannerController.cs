using System;
using System.Collections.Generic;
using Nefta.Core.Events;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Type = Nefta.Core.Events.Type;

namespace AdDemo
{
    public class BannerController : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private Button _show;
        [SerializeField] private Button _hide;
        [SerializeField] private Text _status;
        
        private Banner _banner;

        public void Init(Action getInsights)
        {
            _banner = new Banner(getInsights, SetStatus);

            _title.text = "Banner";
            _show.onClick.AddListener(OnShowClick);
            _hide.onClick.AddListener(OnHideClick);
            _hide.interactable = false;
        }
        
        private void OnShowClick()
        {
            var type = (Type) Random.Range(0, 7);
            var status = (Status)Random.Range(0, 3);
            var source = (Source)Random.Range(0, 7);
            var value = Random.Range(0, 101);
            NeftaAdapterEvents.Record(new ProgressionEvent(type, status)
                { _source = source, _name = $"progression_{type}_{status} {source} {value}", _value = value });
            
            _banner.Load();
            _show.interactable = true;
            
            _hide.interactable = true;
        }

        public void SetAutoRefresh(bool refresh)
        {
            if (_banner != null)
            {
                _banner.SetAutoRefresh(refresh);
            }
        }
        
        private void OnHideClick()
        {
            _banner.Hide();
            _hide.interactable = false;
        }
        
        private void SetStatus(string status)
        {
            _status.text = status;
            Debug.Log($"Banner: {status}");
        }

        public void OnBehaviourInsight(Dictionary<string, Insight> behaviourInsight)
        {
            _banner.OnBehaviourInsight(behaviourInsight);
        }
    }
}