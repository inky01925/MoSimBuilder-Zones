#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Util;

[CustomEditor(typeof(ZoneManager))]
public class ZoneManagerEditor : UnityEditor.Editor
{
    private SerializedProperty zonePlaneProp;
    private SerializedProperty zonesProp;
    private SerializedProperty targetWhenProp;
    private SerializedProperty controllingMechanismProp;
    private SerializedProperty setpointNameProp;
    private SerializedProperty showDebugProp;
    private SerializedProperty activeZoneNameProp;
    private SerializedProperty currentTargetProp;
    private SerializedProperty currentAngleProp;
    private SerializedProperty targetAngleProp;
    private SerializedProperty steerOutputProp;
    private SerializedProperty conditionMechanismProp;
    private SerializedProperty controllerButtonProp;
    private SerializedProperty keyboardButtonProp;

    private enum ZonePlane
    {
        XZ,
        XY,
        YZ
    }

    private enum ZoneAction
    {
        WorldPosition,
        JointPosition
    }

    private void OnEnable()
    {
        zonePlaneProp = serializedObject.FindProperty("zonePlane");
        zonesProp = serializedObject.FindProperty("zones");
        targetWhenProp = serializedObject.FindProperty("targetWhen");
        controllingMechanismProp = serializedObject.FindProperty("controllingMechanism");
        setpointNameProp = serializedObject.FindProperty("setpointName");
        showDebugProp = serializedObject.FindProperty("showDebug");
        activeZoneNameProp = serializedObject.FindProperty("activeZoneName");
        currentTargetProp = serializedObject.FindProperty("currentTarget");
        currentAngleProp = serializedObject.FindProperty("currentAngle");
        targetAngleProp = serializedObject.FindProperty("targetAngle");
        steerOutputProp = serializedObject.FindProperty("steerOutput");
        conditionMechanismProp = serializedObject.FindProperty("conditionMechanism");
        controllerButtonProp = serializedObject.FindProperty("controllerButton");
        keyboardButtonProp = serializedObject.FindProperty("keyboardButton");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(zonePlaneProp);
        EditorGUILayout.PropertyField(targetWhenProp);
        EditorGUILayout.PropertyField(controllingMechanismProp);

        if (targetWhenProp.enumValueIndex == (int)TargetWhen.AtSetpoint)
        {
            EditorGUILayout.PropertyField(conditionMechanismProp);
            EditorGUILayout.PropertyField(setpointNameProp);
        }

        if (targetWhenProp.enumValueIndex == (int)TargetWhen.WhenPressing)
        {
            EditorGUILayout.PropertyField(controllerButtonProp);
            EditorGUILayout.PropertyField(keyboardButtonProp);
        }

        EditorGUILayout.PropertyField(showDebugProp);

        EditorGUILayout.Space();
        DrawZones();

        if (showDebugProp.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(activeZoneNameProp);
            EditorGUILayout.PropertyField(currentTargetProp);
            EditorGUILayout.PropertyField(currentAngleProp);
            EditorGUILayout.PropertyField(targetAngleProp);
            EditorGUILayout.PropertyField(steerOutputProp);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawZones()
    {
        EditorGUILayout.LabelField("Zones", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        for (int i = 0; i < zonesProp.arraySize; i++)
        {
            var zoneProp = zonesProp.GetArrayElementAtIndex(i);
            zoneProp.isExpanded = EditorGUILayout.Foldout(zoneProp.isExpanded, $"Zone {i + 1}", true);

            if (!zoneProp.isExpanded)
                continue;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("zoneName"));

            var firstAxis = GetFirstAxisName();
            var secondAxis = GetSecondAxisName();

            EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("minX"), new GUIContent($"Min {firstAxis}"));
            EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("maxX"), new GUIContent($"Max {firstAxis}"));
            EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("minY"), new GUIContent($"Min {secondAxis}"));
            EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("maxY"), new GUIContent($"Max {secondAxis}"));

            EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("action"));

            var actionProp = zoneProp.FindPropertyRelative("action");
            if (actionProp.enumValueIndex == (int)ZoneAction.WorldPosition)
            {
                EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("targetPosition"));
            }
            else if (actionProp.enumValueIndex == (int)ZoneAction.JointPosition)
            {
                EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("jointTargetValue"));
            }

            EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("zoneColor"));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Move Up") && i > 0)
            {
                zonesProp.MoveArrayElement(i, i - 1);
            }
            if (GUILayout.Button("Move Down") && i < zonesProp.arraySize - 1)
            {
                zonesProp.MoveArrayElement(i, i + 1);
            }
            if (GUILayout.Button("Remove Zone"))
            {
                zonesProp.DeleteArrayElementAtIndex(i);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndHorizontal();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        if (GUILayout.Button("Add Zone"))
        {
            zonesProp.arraySize++;
        }

        EditorGUI.indentLevel--;
    }

    private string GetFirstAxisName()
    {
        switch ((ZonePlane)zonePlaneProp.enumValueIndex)
        {
            case ZonePlane.YZ:
                return "Y";
            default:
                return "X";
        }
    }

    private string GetSecondAxisName()
    {
        switch ((ZonePlane)zonePlaneProp.enumValueIndex)
        {
            case ZonePlane.XZ:
                return "Z";
            case ZonePlane.YZ:
                return "Z";
            default:
                return "Y";
        }
    }
}
#endif