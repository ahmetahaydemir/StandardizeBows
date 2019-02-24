using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StandardizedBow))]
public class StandardizedBowEditor : Editor
{
    StandardizedBow standardized;
    public override void OnInspectorGUI()
    {        
        base.OnInspectorGUI();
        GUILayout.Space(10f);
        standardized = (StandardizedBow)target;

        if (GUILayout.Button("Find Bow Rig Joints"))
        {
            standardized.FindBoneRigs();
        }

        GUILayout.Space(10f);

    }
}
