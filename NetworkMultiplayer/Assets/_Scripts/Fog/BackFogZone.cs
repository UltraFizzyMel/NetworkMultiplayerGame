using UnityEngine;

public class BackFogZone : MonoBehaviour
{
    [SerializeField] private BackFogZoneType zoneType;

    // Tag this on a trigger collider at the BACK of the boat (the side the fog approaches from)
    private const string DetectorTag = "BackFogDetector";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(DetectorTag)) return;
        BackFogManager.Instance?.OnEnterZone(zoneType);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(DetectorTag)) return;
        BackFogManager.Instance?.OnExitZone(zoneType);
    }
}