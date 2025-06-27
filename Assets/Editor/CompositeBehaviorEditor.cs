using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CompositeBehavior))]
public class CompositeBehaviorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CompositeBehavior cb = (CompositeBehavior)target;

        EditorGUILayout.Space();

        // If there are no behaviors, show a warning
        if (cb.behaviors == null || cb.behaviors.Length == 0)
        {
            EditorGUILayout.HelpBox("No behaviors in array.", MessageType.Warning);
        }
        else
        {
            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Behaviors", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Weights", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            // List each behavior + weight
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < cb.behaviors.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                cb.behaviors[i] = (FlockBehavior)EditorGUILayout.ObjectField(
                    cb.behaviors[i],
                    typeof(FlockBehavior),
                    false
                );
                cb.weights[i] = EditorGUILayout.FloatField(cb.weights[i], GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(cb);
            }
        }

        EditorGUILayout.Space();

        // Add Behavior button
        if (GUILayout.Button("Add Behavior"))
        {
            AddBehavior(cb);
            EditorUtility.SetDirty(cb);
        }

        // Remove Behavior button (only if there is at least one)
        if (cb.behaviors != null && cb.behaviors.Length > 0)
        {
            if (GUILayout.Button("Remove Behavior"))
            {
                RemoveBehavior(cb);
                EditorUtility.SetDirty(cb);
            }
        }
    }

    void AddBehavior(CompositeBehavior cb)
    {
        int oldCount = (cb.behaviors != null) ? cb.behaviors.Length : 0;
        var newBehaviors = new FlockBehavior[oldCount + 1];
        var newWeights   = new float[oldCount + 1];

        for (int i = 0; i < oldCount; i++)
        {
            newBehaviors[i] = cb.behaviors[i];
            newWeights[i]   = cb.weights[i];
        }

        newWeights[oldCount] = 1f;
        cb.behaviors = newBehaviors;
        cb.weights   = newWeights;
    }

    void RemoveBehavior(CompositeBehavior cb)
    {
        int oldCount = cb.behaviors.Length;
        if (oldCount == 1)
        {
            cb.behaviors = null;
            cb.weights   = null;
            return;
        }

        var newBehaviors = new FlockBehavior[oldCount - 1];
        var newWeights   = new float[oldCount - 1];

        for (int i = 0; i < oldCount - 1; i++)
        {
            newBehaviors[i] = cb.behaviors[i];
            newWeights[i]   = cb.weights[i];
        }

        cb.behaviors = newBehaviors;
        cb.weights   = newWeights;
    }
}
