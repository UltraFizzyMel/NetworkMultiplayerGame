using Unity.Netcode;
using UnityEngine;

public class EnvironmentMover : NetworkBehaviour
{
    [SerializeField] private float steeringInfluence = 2f;

    private BoatMovement boatMovement;
    private BoatSteeringManager _steering;

    [SerializeField] private bool ignoreSteering;
    [SerializeField] private bool ignoreMovement;

    private void Update()
    {
        if (boatMovement == null) boatMovement = BoatMovement.Instance;
        if (_steering == null) _steering = BoatSteeringManager.Instance;

        if (boatMovement == null || _steering == null)
            return;

        if (GameManager.Instance == null || !GameManager.Instance.GameReady())
            return;

        float moveSpeed = boatMovement.netCurrentMoveSpeed.Value;
        float steering = _steering.SteeringAmount.Value;

        // FORWARD MOVEMENT
        // The boat "moves" in the +X direction, so the environment slides in -X.
        //transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        // STEERING OFFSET
        // D (right, positive steering) → environment moves +Z
        // A (left,  negative steering) → environment moves -Z
        //transform.position += Vector3.forward * (steering * steeringInfluence * Time.deltaTime);

        //Combines foward and steering offset into one variable to change the direction of the transform only once
        //Doesn't change steering influence for different environments (rocks vs lighthouse)
        //Vector3 moveDirection = new Vector3(-moveSpeed, 0f, steering * steeringInfluence);

        Vector3 moveDirection = Vector3.zero;

        // FORWARD MOVEMENT (X AXIS)
        // Objects with ignoreMovement enabled will stay near the boat instead of drifting away
        if (!ignoreMovement || !FogZoneManager.BlockForwardMovement)
        {
            moveDirection += Vector3.left * moveSpeed;
        }

        // STEERING MOVEMENT (Z AXIS)
        // Objects with ignoreSteering enabled won't sway left/right with the boat
        if (!ignoreSteering)
        {
            moveDirection += Vector3.forward * (steering * steeringInfluence);
        }

        transform.position += moveDirection * Time.deltaTime;
    }
}