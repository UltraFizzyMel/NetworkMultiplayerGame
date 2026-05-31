/*using Unity.Netcode;
using UnityEngine;

public class BoatTiltVisual : MonoBehaviour
{
    [SerializeField] private Transform boatVisual;
    [SerializeField] private float tiltAmount = 10f;
    [SerializeField] private float tiltSpeed = 5f;

    private void Update()
    {
        if (!NetworkManager.Singleton.IsClient)
            return;

        float steering =
            BoatSteeringManager.Instance.SteeringAmount.Value;

        Quaternion target =
            Quaternion.Euler(
                0,
                0,
                steering * -tiltAmount
            );

        boatVisual.localRotation =
            Quaternion.Lerp(
                boatVisual.localRotation,
                target,
                Time.deltaTime * tiltSpeed
            );
    }
}*/

// ─── BoatTiltVisual.cs ───────────────────────────────────────────────────────
using Unity.Netcode;
using UnityEngine;

public class BoatTiltVisual : MonoBehaviour
{
    [SerializeField] private Transform boatVisual;
    [SerializeField] private float tiltAmount = 10f;
    [SerializeField] private float tiltSpeed = 5f;

    private Quaternion _baseRotation;
    private bool _baseRotationCaptured;

    private void LateUpdate()
    {
        // LateUpdate ensures all animation/physics has settled for this frame
        // before we read the steering value and modify the rotation.
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsClient) return;
        if (BoatSteeringManager.Instance == null) return;
        if (boatVisual == null) return;

        // Capture once, on the first frame everything is ready, so we get the
        // mesh's true resting pose rather than whatever Unity default-initialises.
        if (!_baseRotationCaptured)
        {
            _baseRotation = boatVisual.localRotation;
            _baseRotationCaptured = true;
        }

        float steering = BoatSteeringManager.Instance.SteeringAmount.Value;
        // Tilt offset only on Z. Positive steering → tilt right (negative Z).
        Quaternion tiltOffset = Quaternion.Euler(0f, 0f, steering * -tiltAmount);
        // Compose: base pose first, then tilt on top.
        Quaternion target = _baseRotation * tiltOffset;

        boatVisual.localRotation = Quaternion.Lerp(
            boatVisual.localRotation,
            target,
            Time.deltaTime * tiltSpeed);
    }
}