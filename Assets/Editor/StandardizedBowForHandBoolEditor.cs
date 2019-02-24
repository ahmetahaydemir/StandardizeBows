using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(StandardizedBowForHandBool))]
public class StandardizedBowForHandBoolEditor : Editor
{
    StandardizedBowForHandBool standardized;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GUILayout.Space(10f);
        standardized = (StandardizedBowForHandBool)target;

        if (GUILayout.Button("Find Bow Rig Joints"))
        {
            standardized.FindBoneRigs();
        }

        GUILayout.Space(10f);

    }
}
