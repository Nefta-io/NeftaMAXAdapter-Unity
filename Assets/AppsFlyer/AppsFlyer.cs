using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AppsFlyerSDK
{
    public class AppsFlyer
    {
        public static Dictionary<string, object> CallbackStringToDictionary(string str)
        {
            return new Dictionary<string, object>();
        }

        public static async Task<string> getAppsFlyerUID()
        {
            await Task.Delay(1);
            return "abc";
        }
        
        public static async Task enableDebug(bool enabled)
        {

        }

        public static async Task registerConversionListener(Action<string> onConversionDataSuccess, Action<string> onConversionDataFail)
        {
        }
        
        public static async Task init(string devKey, string appID, MonoBehaviour gameObject = null)
        {
        }

        public static void start()
        {
            
        }
    }
}