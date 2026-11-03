using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SbiSimulator))]
public class SbiSimulatorEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(!Application.isPlaying)) {
            if (GUILayout.Button(
                "Отправить показание",
                GUILayout.Height(36))) {
                SbiSimulator simulator = (SbiSimulator) target;

                simulator.SendMessage(
                    "SendTestMeasurement",
                    SendMessageOptions.RequireReceiver
                );
            }
        }
    }
}
