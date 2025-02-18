namespace NeftaCustomAdapter
{
    public class Insight
    {
        public string _status;
        public long _int;
        public double _float;
        public string _string;

        internal Insight(string status, long intValue, double floatValue, string stringValue)
        {
            _status = status;
            _int = intValue;
            _float = floatValue;
            _string = stringValue;
        }
    }
}