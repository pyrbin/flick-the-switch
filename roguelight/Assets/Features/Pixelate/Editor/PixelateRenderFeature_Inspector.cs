using Ooze.Runtime.Pixelate.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ooze.Runtime.Pixelate.Editor {
    [CustomEditor(typeof(PixelateRenderFeature))]
    public class PixelateRenderFeature_Inspector : UnityEditor.Editor {
        internal const string ScaleFactorClampedWarning = "Render resolution excedees display resolution. Decerease the Pixels Per Unit value.";
        internal const string CameraNotFoundWarning = "No PixelateCamera found in scene.";
        internal PixelateRenderFeature Target => (PixelateRenderFeature)target;
        public override void OnInspectorGUI()
        {
            DrawCameraInspectorGroup();

            // Draw seperator
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (Target.Inspector_ScaleFactorClamped)
                EditorGUILayout.HelpBox(ScaleFactorClampedWarning, MessageType.Error);

            DrawDefaultInspectorGroup();
        }

        private void DrawDefaultInspectorGroup() {
            serializedObject.Update();
            var prop = serializedObject.GetIterator();
            prop.NextVisible(true);
            while (prop.NextVisible(false))
            {
                EditorGUILayout.PropertyField(prop, true);
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCameraInspectorGroup() {
            if (Target.Camera != null) {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Camera", Target.Camera, typeof(PixelateCamera), true);
                // Draw props of Target.Camera default inspector
                var camProp = new SerializedObject(Target.Camera).GetIterator();
                camProp.NextVisible(true);
                while (camProp.NextVisible(false))
                {
                    EditorGUILayout.PropertyField(camProp, true);
                }
                EditorGUI.EndDisabledGroup();
            } else {
                EditorGUILayout.HelpBox(CameraNotFoundWarning, MessageType.Error);
            }
        }
    }
}
