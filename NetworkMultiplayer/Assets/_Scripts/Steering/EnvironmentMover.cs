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
        if (!IsServer)
            return;

        if (boatMovement == null) boatMovement = BoatMovement.Instance;
        if (_steering == null) _steering = BoatSteeringManager.Instance;

        if (boatMovement == null || _steering == null)
            return;

        if (GameManager.Instance == null || !GameManager.Instance.GameReady())
            return;

        float moveSpeed = boatMovement.netCurrentMoveSpeed.Value;
        float steering = _steering.SteeringAmount.Value;
        Vector3 moveDirection = Vector3.zero;

        // FORWARD MOVEMENT (X AXIS)
        // ignoreMovement = true  → fog zone, never moves forward
        // ignoreMovement = false → normal object, but ALSO stops when boat
        // is in the death zone (BlockForwardMovement flag)
        if (!ignoreMovement && !FogZoneManager.BlockForwardMovement)
            moveDirection += Vector3.left * moveSpeed;

        // STEERING MOVEMENT (Z AXIS)
        // Objects with ignoreSteering enabled won't sway left/right with the boat
        if (!ignoreSteering)
            moveDirection += Vector3.forward * (steering * steeringInfluence);

        transform.position += moveDirection * Time.deltaTime;
    }
}