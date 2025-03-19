namespace AdDemo
{
    public class AdConfig
    {
        private string _iosId;
        private string _androidId;
        
        public double _cpm;

        public string Id
        {
            get {
#if UNITY_IOS
                return _iosId;
#else // UNITY_ANDROID
                return _androidId;
#endif
        }
    }

        public AdConfig(string iosId, string androidId, double cpm)
        {
            _iosId = iosId;
            _androidId = androidId;
            _cpm = cpm;
        }
    }
}