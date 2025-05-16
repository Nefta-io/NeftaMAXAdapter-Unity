namespace NeftaCustomAdapter
{
    public class Insight
    {
        public long _int;
        public double _float;
        public string _string;

        internal Insight(long intValue, double floatValue, string stringValue)
        {
            _int = intValue;
            _float = floatValue;
            _string = stringValue;
        }
    }
}