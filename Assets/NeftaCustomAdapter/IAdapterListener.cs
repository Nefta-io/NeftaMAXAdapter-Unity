#if !UNITY_EDITOR
namespace NeftaCustomAdapter
{
    public interface IAdapterListener
    {
        void IOnBehaviourInsight(int id, string playerScore);
    }
}
#endif