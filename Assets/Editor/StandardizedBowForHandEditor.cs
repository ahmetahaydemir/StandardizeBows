using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(StandardizedBowForHand))]
public class StandardizedBowForHandEditor : Editor
{
    StandardizedBowForHand standardized;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GUILayout.Space(10f);
        standardized = (StandardizedBowForHand)target;

        if (GUILayout.Button("Find Bow Rig Joints"))
        {
            standardized.FindBoneRigs();
        }

        GUILayout.Space(10f);

    }
}
