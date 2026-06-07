using UnityEngine;

public class BackFogMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Generator generator;

    [Header("Chase / Retreat")]
    [SerializeField] private float chaseSpeed = 1f;  // Units/sec fog advances when no fuel
    [SerializeField] private float retreatSpeed = 0.5f;// Units/sec fog retreats when fueled
    [SerializeField] private float maxChaseDistance = 80f;  // Hard cap on how far fog can advance

    // How far the fog has advanced beyond its natural position.
    // 0 = fog is at its original relative position and cannot retreat further.
    private float _chaseOffset;

    private BoatMovement _boatMovement;
    private BoatSteeringManager _steering;

    private void Update()
    {
        if (_boatMovement == null) _boatMovement = BoatMovement.Instance;
        if (_steering == null) _steering = BoatSteeringManager.Instance;

        if (_boatMovement == null) return;
        if (GameManager.Instance == null || !GameManager.Instance.GameReady()) return;

        //float moveSpeed = _boatMovement.netCurrentMoveSpeed.Value;

        // ── Move with the environment ─────────────────────────────────────────
        // Keeps the fog at a consistent distance behind the boat during normal play.
        // This is identical to EnvironmentMover's behaviour so the fog participates
        // in the same world-movement system as rocks and the lighthouse.
        //transform.position += Vector3.right * moveSpeed * Time.deltaTime;
        //transform.position += Vector3.forward * (steering * steeringInfluence) * Time.deltaTime;

        if (generator == null) return;

        bool stopped = BackFogManager.Instance != null && BackFogManager.Instance.FogStopped;
        bool hasFuel = generator.FuelCheck();

        /*if (!hasFuel && !stopped)
        {
            // ── Chase ─────────────────────────────────────────────────────────
            // Fog advances faster than the environment when there is no fuel.
            /*float advance = Mathf.Min(
                chaseSpeed * Time.deltaTime,
                maxChaseDistance - _chaseOffset);

            if (advance > 0f)
            {
                _chaseOffset += advance;
                transform.position += Vector3.right * advance;
            }/
            transform.position += Vector3.right * moveSpeed * Time.deltaTime;
        }
        else if (hasFuel && _chaseOffset > 0f)
        {
            // ── Retreat ───────────────────────────────────────────────────────
            // When fuel is restored the fog moves back toward its original position.
            // Clamped so it never retreats past the natural starting offset (chaseOffset = 0).
            float retreat = Mathf.Min(retreatSpeed * Time.deltaTime, _chaseOffset);
            _chaseOffset -= retreat;
            transform.position += Vector3.left * retreat;
        }*/
        if (!hasFuel && !stopped)
        {
            // Fog advances toward the boat
            float advance = Mathf.Min(
                chaseSpeed * Time.deltaTime,
                maxChaseDistance - _chaseOffset);

            if (advance > 0f)
            {
                _chaseOffset += advance;

                // Move opposite to the normal world movement
                transform.position += Vector3.right * advance;
            }
        }
        else if (hasFuel && _chaseOffset > 0f)
        {
            // Fog retreats back to original position
            float retreat = Mathf.Min(
                retreatSpeed * Time.deltaTime,
                _chaseOffset);

            _chaseOffset -= retreat;

            transform.position += Vector3.left * retreat;
        }
    }
}