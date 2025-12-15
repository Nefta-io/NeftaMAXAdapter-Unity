using System;
using System.Collections.Generic;
using System.Text;

namespace NeftaCustomAdapter
{
    public class InitConfiguration
    {
        public bool _skipOptimization;
        public Dictionary<string, string[]> _providerAdUnits;

        public InitConfiguration(bool skipOptimization, string providerAdUnits)
        {
            _skipOptimization = skipOptimization;
            _providerAdUnits = new Dictionary<string, string[]>();
            StringBuilder sb = new StringBuilder();
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
            return _providerAdUnits["applovin-max"];
        }
    }
}