using System;
using System.Collections.Generic;
using System.Text;

namespace NeftaCustomAdapter
{
    public class InitConfiguration
    {
        public bool _skipOptimization;
        public string _nuid;
        public Dictionary<string, string[]> _providerAdUnits;

        public InitConfiguration(bool skipOptimization, string nuid, string providerAdUnits)
        {
            _skipOptimization = skipOptimization;
            _nuid = nuid;
            _providerAdUnits = new Dictionary<string, string[]>();
            var sb = new StringBuilder();
            String provider = null;
            List<String> adUnits = null;
            for (var i = 0; i < providerAdUnits.Length; i++)
            {
                var c = providerAdUnits[i];
                if (c == ':')
                {
                    if (provider != null)
                    {
                        _providerAdUnits.Add(provider, adUnits.ToArray());
                    }
                    provider = sb.ToString();
                    adUnits = new List<String>();
                    sb.Clear();
                }
                else if (c == ',' || i == providerAdUnits.Length - 1)
                {
                    adUnits.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);   
                }
            }
            if (provider != null && adUnits.Count > 0)
            {
                _providerAdUnits.Add(provider, adUnits.ToArray());
            }
        }

        public string[] GetProviderAdUnits()
        {
            if (_providerAdUnits.TryGetValue("applovin-max", out var adUnits))
            {
                return adUnits;
            }
            return null;
        }
    }
}