using UnityEditor;

namespace AmagiSakuya.MiniMap.EditorClass
{
    [CustomEditor(typeof(MiniMap))]
    public class MiniMapEditor : Editor
    {
        private MiniMap t;
        void OnEnable()
        {
            t = (MiniMap)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}