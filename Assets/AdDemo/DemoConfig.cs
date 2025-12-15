using UnityEngine;

namespace AdDemo
{
    [CreateAssetMenu(fileName = "DemoConfig", menuName = "Ad Demo/Config")]
    public class DemoConfig : ScriptableObject
    {
        public bool _isSimulator;
    }
}