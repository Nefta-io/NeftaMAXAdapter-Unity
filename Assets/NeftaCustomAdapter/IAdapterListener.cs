#if !UNITY_EDITOR
namespace NeftaCustomAdapter
{
    public interface IAdapterListener
    {
        void IOnBehaviourInsight(string playerScore);
    }
}
#endif