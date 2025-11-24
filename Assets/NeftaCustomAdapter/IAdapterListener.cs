#if !UNITY_EDITOR
namespace NeftaCustomAdapter
{
    public interface IAdapterListener
    {
        void IOnReady(string initConfiguration);

        void IOnInsights(int id, int adapterResponseType, string adapterRerponse);
    }
}
#endif