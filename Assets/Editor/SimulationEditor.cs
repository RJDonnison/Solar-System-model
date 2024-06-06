using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BodySimulation))]
public class SimulationEditor : Editor
{
    private BodySimulation simulation;
    private Editor settingsEditor;

    private bool settingsFoldout = true;
    private bool variablesFoldout = false;

    private void OnEnable()
    {
        simulation = (BodySimulation)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Settings Section
        settingsFoldout = EditorGUILayout.Foldout(settingsFoldout, "Settings", true);
        if (settingsFoldout)
        {
            EditorGUI.indentLevel++;
            DrawSettingsSection();
            EditorGUI.indentLevel--;
        }

        // Variables Section
        variablesFoldout = EditorGUILayout.Foldout(variablesFoldout, "Orbit Debug", true);
        if (variablesFoldout)
        {
            EditorGUI.indentLevel++;
            DrawVariablesSection();
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSettingsSection()
    {
        if (simulation.settings != null)
        {
            CreateCachedEditor(simulation.settings, null, ref settingsEditor);
            settingsEditor.OnInspectorGUI();

            if (GUI.changed)
            {
                simulation.UpdateCelestialBodies();
            }
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("settings"));
    }

    private void DrawVariablesSection()
    {
        SerializedProperty iterator = serializedObject.GetIterator();
        iterator.NextVisible(true); // Skip script reference

        // Skip the settings property
        while (iterator.NextVisible(false))
        {
            if (iterator.name == "settings")
                continue;

            EditorGUILayout.PropertyField(iterator, true);
        }
    }
}