using Unity.Netcode;
using UnityEngine;

public class EnvironmentMover : NetworkBehaviour
{
    [SerializeField] private float steeringInfluence = 2f;

    private BoatMovement boatMovement;
    private BoatSteeringManager _steering;

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
        transform.position += Vector3.back * moveSpeed * Time.deltaTime;

        // STEERING OFFSET
        transform.position += Vector3.right * (-steering * steeringInfluence * Time.deltaTime);
    }
}