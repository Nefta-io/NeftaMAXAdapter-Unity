#if !UNITY_EDITOR
namespace NeftaCustomAdapter
{
    public interface IPluginListener
    {
        void IOnReady(string initConfiguration);

        void IOnInsights(int id, int adapterResponseType, string adapterRerponse);

        void IOnNewSessionCallback();
    }
}
#endif