using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EventBehavior))]
public class EventBehaviorEditor : Editor
{
    SerializedProperty spawnPositiveBoids, spawnNegativeBoids, spawnBomb, spawnMultiplier, spawnPortal;
    SerializedProperty activationRadius;
    SerializedProperty positiveBoidCount, negativeBoidCount, bombTimer, multiplierTimer, portalTag;

    void OnEnable()
    {
        spawnPositiveBoids = serializedObject.FindProperty("spawnPositiveBoids");
        spawnNegativeBoids = serializedObject.FindProperty("spawnNegativeBoids");
        spawnBomb          = serializedObject.FindProperty("spawnBomb");
        spawnMultiplier    = serializedObject.FindProperty("spawnMultiplier");
        spawnPortal        = serializedObject.FindProperty("spawnPortal");

        activationRadius   = serializedObject.FindProperty("activationRadius");
        positiveBoidCount  = serializedObject.FindProperty("positiveBoidCount");
        negativeBoidCount  = serializedObject.FindProperty("negativeBoidCount");
        bombTimer          = serializedObject.FindProperty("bombTimer");
        multiplierTimer    = serializedObject.FindProperty("multiplierTimer");
        portalTag          = serializedObject.FindProperty("portalTag");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

         
        EditorGUILayout.LabelField("Activation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(activationRadius, new GUIContent("Activation Radius"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Event Types", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spawnPositiveBoids, new GUIContent("Positive Boids"));
        EditorGUILayout.PropertyField(spawnNegativeBoids, new GUIContent("Negative Boids"));
        EditorGUILayout.PropertyField(spawnBomb,           new GUIContent("Bomb"));
        EditorGUILayout.PropertyField(spawnMultiplier,     new GUIContent("Multiplier"));
        EditorGUILayout.PropertyField(spawnPortal,         new GUIContent("Portal"));

       

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
        if (spawnPositiveBoids.boolValue)
            EditorGUILayout.PropertyField(positiveBoidCount, new GUIContent("Positive Boid Count"));
        if (spawnNegativeBoids.boolValue)
            EditorGUILayout.PropertyField(negativeBoidCount, new GUIContent("Negative Boid Count"));
        if (spawnBomb.boolValue)
            EditorGUILayout.PropertyField(bombTimer, new GUIContent("Bomb Delay"));
        if (spawnMultiplier.boolValue)
            EditorGUILayout.PropertyField(multiplierTimer, new GUIContent("Multiplier Delay"));
        if (spawnPortal.boolValue)
            EditorGUILayout.PropertyField(portalTag, new GUIContent("Portal Tag"));

        serializedObject.ApplyModifiedProperties();
    }
}
