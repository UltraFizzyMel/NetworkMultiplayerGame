using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class BasicPlayer : NetworkBehaviour
{
    private float speed = 5f;
    private PlayerInput playerInput;
    private InputAction moveAction;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        //Get playerinput on THIS spawned player object
        playerInput = GetComponent<PlayerInput>();

        //Grab the action by name from the controls action asset
        moveAction = playerInput.actions["Move"];

        // Enable the map/actions for the local owner only
        playerInput.enabled = true;
        moveAction.Enable();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        moveAction?.Disable();
    }

    private void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        Vector2 move = moveAction.ReadValue<Vector2>();
        Vector3 move3 = new Vector3(move.x, 0f, move.y) * speed * Time.deltaTime;
        transform.position += move3;
    }
}
