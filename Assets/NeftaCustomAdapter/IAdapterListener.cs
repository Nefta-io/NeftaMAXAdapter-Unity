#if !UNITY_EDITOR
namespace NeftaCustomAdapter
{
    public interface IAdapterListener
    {
        void IOnInsights(int id, string insights);
    }
}
#endif