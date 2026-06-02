// ??? FogZone.cs ??????????????????????????????????????????????????????????????
using UnityEngine;

public enum FogZoneType { Warning, Death, IgnoreSteering }

public class FogZone : MonoBehaviour
{
    [SerializeField] private FogZoneType zoneType;

    // True = this zone is on the right side of the boat.
    // A right zone only reacts to the RightFogDetector tag, and vice versa,
    // so the wrong-side detector can never trigger the wrong zone.
    [SerializeField] private bool isRightZone;

    private string ExpectedTag => isRightZone ? "RightFogDetector" : "LeftFogDetector";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(ExpectedTag)) return;
        FogZoneManager.Instance?.OnEnterZone(zoneType, isRightZone);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(ExpectedTag)) return;
        FogZoneManager.Instance?.OnExitZone(zoneType, isRightZone);
    }
}