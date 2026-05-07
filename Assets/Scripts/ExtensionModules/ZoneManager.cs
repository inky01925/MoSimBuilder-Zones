using System;
using MyBox;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Util;

public class ZoneManager : MonoBehaviour
{
    private enum ZonePlane
    {
        XZ,
        XY,
        YZ
    }

    private enum ZoneAction
    {
        WorldPosition,
        JointPosition,
        Deadzone
    }

    [System.Serializable]
    private struct ZoneTarget
    {
        [Tooltip("Optional label for this zone.")]
        public string zoneName;

        [Header("Zone Bounds")]
        public float minX;
        public float maxX;
        public float minY;
        public float maxY;

        [Header("Zone Target")]
        public ZoneAction action;
        public Vector3 targetPosition;
        public float jointTargetValue;

        [Tooltip("Color used to draw this zone in the editor.")]
        public Color zoneColor;

        public bool Contains(Vector3 worldPosition, ZonePlane plane)
        {
            switch (plane)
            {
                case ZonePlane.XY:
                    return worldPosition.x >= minX && worldPosition.x <= maxX &&
                           worldPosition.y >= minY && worldPosition.y <= maxY;
                case ZonePlane.YZ:
                    return worldPosition.y >= minX && worldPosition.y <= maxX &&
                           worldPosition.z >= minY && worldPosition.z <= maxY;
                default:
                    return worldPosition.x >= minX && worldPosition.x <= maxX &&
                           worldPosition.z >= minY && worldPosition.z <= maxY;
            }
        }
    }

    [Header("Zone Settings")]
    [Tooltip("Select the 2D plane used for zone bounds.")]
    [SerializeField] private ZonePlane zonePlane = ZonePlane.XZ;

    [Tooltip("List of rectangular zones. The first matching zone is used.")]
    [SerializeField] private ZoneTarget[] zones = new ZoneTarget[0];

    [Header("Activation")]
    [SerializeField] private TargetWhen targetWhen = TargetWhen.Always;
    [SerializeField] private BuildMechanism controllingMechanism;
    [ConditionalField(true, nameof(WhenAtSetpoint))]
    [SerializeField] private string setpointName;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    [ConditionalField(true, nameof(showDebug))]
    [SerializeField] private string activeZoneName;
    [ConditionalField(true, nameof(showDebug))]
    [SerializeField] private Vector3 currentTarget;
    [ConditionalField(true, nameof(showDebug))]
    [SerializeField] private float currentAngle;
    [ConditionalField(true, nameof(showDebug))]
    [SerializeField] private float targetAngle;
    [ConditionalField(true, nameof(showDebug))]
    [SerializeField] private float steerOutput;

    private SwerveController controller;
    private JointController jointController;
    private PIDController steeringPID;

    private void Start()
    {
        controller = GetComponent<SwerveController>();
        jointController = GetComponent<JointController>() ?? GetComponent<BuildMechanism>()?.GetController();

        if (controllingMechanism == null)
        {
            controllingMechanism = Utils.FindParentObjectComponent<BuildMechanism>(gameObject);
        }

        steeringPID = new PIDController
        {
            proportionalGain = 0.5f,
            integralGain = 0f,
            derivativeGain = 0.05f,
            outputMax = 0.5f,
            outputMin = -0.5f
        };
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying)
            return;

        if (!IsActivationSatisfied())
            return;

        var activeZone = GetActiveZone();
        if (activeZone == null)
            return;

        activeZoneName = activeZone.Value.zoneName;
        currentTarget = activeZone.Value.targetPosition;

        if (activeZone.Value.action == ZoneAction.Deadzone)
            return;

        if (ShouldUseDriveSteering(activeZone.Value))
        {
            targetAngle = CalculateTargetAngle(currentTarget) + 180f;
            currentAngle = transform.localRotation.eulerAngles.y;

            steerOutput = steeringPID.UpdateAngle(Time.fixedDeltaTime, currentAngle, targetAngle);
            controller?.OverideSteer(steerOutput, true);
        }
        else if (ShouldUseJointOverride(activeZone.Value))
        {
            jointController?.OveridePosition(activeZone.Value.jointTargetValue);
            targetAngle = activeZone.Value.jointTargetValue;
            currentAngle = 0f;
            steerOutput = 0f;
        }
    }

    private ZoneTarget? GetActiveZone()
    {
        var position = transform.position;
        foreach (var zone in zones)
        {
            if (zone.Contains(position, zonePlane))
                return zone;
        }

        return null;
    }

    public Vector3? GetActiveZoneTarget()
    {
        var activeZone = GetActiveZone();
        return activeZone?.targetPosition;
    }

    public bool IsActiveZoneDisabled()
    {
        var activeZone = GetActiveZone();
        return activeZone?.action == ZoneAction.Deadzone;
    }

    private bool ShouldUseDriveSteering(ZoneTarget zone)
    {
        return zone.action == ZoneAction.WorldPosition && controller != null;
    }

    private bool ShouldUseJointOverride(ZoneTarget zone)
    {
        return zone.action == ZoneAction.JointPosition && jointController != null;
    }

    private bool WhenAtSetpoint() => targetWhen == TargetWhen.AtSetpoint;

    private bool IsActivationSatisfied()
    {
        if (targetWhen == TargetWhen.Always)
            return true;

        if (targetWhen != TargetWhen.AtSetpoint)
            return false;

        var mechanism = controllingMechanism ?? Utils.FindParentObjectComponent<BuildMechanism>(gameObject);
        if (mechanism == null)
            return false;

        var controller = mechanism.GetController();
        if (controller == null)
            return false;

        return string.Equals(
            (controller.getActiveSetpoint() ?? string.Empty).Trim(),
            (setpointName ?? string.Empty).Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private float CalculateTargetAngle(Vector3 targetPosition)
    {
        Vector3 direction = transform.position - targetPosition;
        float angleInRadians = Mathf.Atan2(direction.x, direction.z);
        return angleInRadians * Mathf.Rad2Deg;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebug || zones == null)
            return;

        foreach (var zone in zones)
        {
            Vector3 center;
            Vector3 size;
            switch (zonePlane)
            {
                case ZonePlane.XY:
                    center = new Vector3((zone.minX + zone.maxX) * 0.5f, (zone.minY + zone.maxY) * 0.5f, 0f);
                    size = new Vector3(Mathf.Abs(zone.maxX - zone.minX), Mathf.Abs(zone.maxY - zone.minY), 0.01f);
                    break;
                case ZonePlane.YZ:
                    center = new Vector3(0f, (zone.minX + zone.maxX) * 0.5f, (zone.minY + zone.maxY) * 0.5f);
                    size = new Vector3(0.01f, Mathf.Abs(zone.maxX - zone.minX), Mathf.Abs(zone.maxY - zone.minY));
                    break;
                default:
                    center = new Vector3((zone.minX + zone.maxX) * 0.5f, 0f, (zone.minY + zone.maxY) * 0.5f);
                    size = new Vector3(Mathf.Abs(zone.maxX - zone.minX), 0.01f, Mathf.Abs(zone.maxY - zone.minY));
                    break;
            }

            var fillColor = new Color(zone.zoneColor.r, zone.zoneColor.g, zone.zoneColor.b, 0.12f);
            Gizmos.color = fillColor;
            Gizmos.DrawCube(center, size);

            var brightness = (zone.zoneColor.r + zone.zoneColor.g + zone.zoneColor.b) / 3f;
            var outlineColor = brightness > 0.1f ? zone.zoneColor : Color.yellow;
            Gizmos.color = outlineColor;
            Gizmos.DrawWireCube(center, size);

            #if UNITY_EDITOR
            var label = string.IsNullOrEmpty(zone.zoneName) ? "Zone" : zone.zoneName;
            Handles.color = outlineColor;
            Handles.Label(center + Vector3.up * 0.05f, label);

            var halfSize = size * 0.5f;
            var corners = new Vector3[4];
            corners[0] = center + new Vector3(-halfSize.x, 0, -halfSize.z);
            corners[1] = center + new Vector3(halfSize.x, 0, -halfSize.z);
            corners[2] = center + new Vector3(halfSize.x, 0, halfSize.z);
            corners[3] = center + new Vector3(-halfSize.x, 0, halfSize.z);

            Handles.DrawAAPolyLine(4f, corners[0], corners[1], corners[2], corners[3], corners[0]);
            #endif
        }

        // Draw current target if debug is enabled
        if (showDebug && currentTarget != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentTarget, 0.1f);
            #if UNITY_EDITOR
            Handles.color = Color.red;
            Handles.Label(currentTarget + Vector3.up * 0.15f, "Current Target");
            #endif
        }
    }
}
