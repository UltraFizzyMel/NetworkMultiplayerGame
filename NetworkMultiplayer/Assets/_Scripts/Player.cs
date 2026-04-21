using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : NetworkBehaviour
{
    [Header("Player Components")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera playerCamera;

    [Header("Player Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float maxPitch = 80f;

    private PlayerInput pi;
    private InputAction moveAction;
    private InputAction lookAction;
    private CharacterController cc;

    private float pitch;

    public override void OnNetworkSpawn()
    {
        cc = GetComponent<CharacterController>();
        pi = GetComponent<PlayerInput>();

        PlayerRegistry.Register(this);

        if (!IsOwner)
        {
            if (playerCamera)
                playerCamera.enabled = false;
            if (pi)
                pi.enabled = false;

            enabled = false;
            return;
        }

        /*if (!IsServer)
        {
            cc.enabled = false;
        }*/
        
        moveAction = pi.actions["Move"];
        lookAction = pi.actions["Look"];
        moveAction.Enable();
        lookAction.Enable();

        if (playerCamera)
            playerCamera.enabled = true; //start off

        if (pi)
            pi.enabled = true;

        //controllingClientId.OnValueChanged += OnControlChanged;

        //OnControlChanged(0, controllingClientId.Value);
    }

    private void Update()
    {
        if (!IsOwner || !IsSpawned)
            return;

        //if (NetworkManager.Singleton.LocalClientId != controllingClientId.Value)
        //  return;

        //if (controllingClientId.Value == ulong.MaxValue)
        //  return;

        Vector2 m = moveAction.ReadValue<Vector2>();
        //Vector3 move = transform.right * m.x + transform.forward * m.y;
        //if (cc.enabled)
            //cc.Move(move * moveSpeed * Time.deltaTime);

        MoveServerRpc(m);

        Vector2 look = lookAction.ReadValue<Vector2>() * lookSensitivity;    
        //transform.Rotate(0f, look.x, 0f);
        LookServerRpc(look.x);

        pitch -= look.y;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 pos, Quaternion rot, Vector3 scale)
    {
        if (IsOwner)
        {
            var cnt = GetComponent<ClientNetworkTransform>();

            if (cnt)
            {
                cnt.Teleport(pos, rot, scale);
            }
        }
    }

    /*private void LateUpdate()
    {
        if (!IsOwner || !IsSpawned)
            return;

        Vector2 look = lookAction.ReadValue<Vector2>();

        pitch -= look.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }*/

    [ServerRpc]
    private void MoveServerRpc(Vector2 input)
    {
        Vector3 move =
            transform.right * input.x +
            transform.forward * input.y;

        move.y = 0f;
        move.Normalize();

        cc.Move(move * moveSpeed * Time.deltaTime);
    }

    [ServerRpc]
    private void LookServerRpc(float yawInput)
    {
        transform.Rotate(0f, yawInput, 0f);
    }

    public override void OnNetworkDespawn()
    {
        PlayerRegistry.Unregister(this);
    }
}
