/*using Unity.Netcode;
using UnityEngine;

public class BoatSteeringManager : NetworkBehaviour
{
    public static BoatSteeringManager Instance;

    [SerializeField] private float maxSteeringOffset = 8f;
    [SerializeField] private float steeringSmoothness = 3f;

    public NetworkVariable<float> SteeringAmount = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float targetSteering;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        SteeringAmount.Value = Mathf.Lerp(
            SteeringAmount.Value,
            targetSteering,
            Time.deltaTime * steeringSmoothness
        );
    }

    public void SetSteering(float input)
    {
        if (!IsOwner && !IsServer)
            return;

        SetSteeringServerRpc(input);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetSteeringServerRpc(float input)
    {
        targetSteering = Mathf.Clamp(input, -1f, 1f);
    }

    public void ApplySteeringKnockback(float amount)
    {
        if (!IsServer)
            return;

        targetSteering += Random.Range(-amount, amount);

        targetSteering = Mathf.Clamp(targetSteering, -maxSteeringOffset, maxSteeringOffset);
    }
}*/

// ─── BoatSteeringManager.cs ──────────────────────────────────────────────────
using Unity.Netcode;
using UnityEngine;

public class BoatSteeringManager : NetworkBehaviour
{
    public static BoatSteeringManager Instance { get; private set; }

    [SerializeField] private float maxSteeringOffset = 8f;
    [SerializeField] private float steeringSmoothness = 3f;

    public NetworkVariable<float> SteeringAmount = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private float _targetSteering;

    public bool blockLeftSteering;
    public bool blockRightSteering;

    private void Awake() => Instance = this;

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsServer) return;

        SteeringAmount.Value = Mathf.Lerp(
            SteeringAmount.Value,
            _targetSteering,
            Time.deltaTime * steeringSmoothness);
    }

    // Call from any client or the server. Input is clamped to -1..1 for normal steering; ApplySteeringKnockback can push it beyond that temporarily
    public void SetSteering(float input)
    {
        //SetSteeringServerRpc(Mathf.Clamp(input, -1f, 1f));
        input = Mathf.Clamp(input, -1f, 1f);

        // Trying to steer LEFT while blocked
        if (blockLeftSteering && input < 0f)
        {
            input = 0f;
        }

        // Trying to steer RIGHT while blocked
        if (blockRightSteering && input > 0f)
        {
            input = 0f;
        }

        SetSteeringServerRpc(input);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetSteeringServerRpc(float input)
    {
        _targetSteering = input;
    }

    // Server-only. Called by BoatCollisionDetector on rock impact
    public void ApplySteeringKnockback(float amount)
    {
        if (!IsServer) return;

        _targetSteering += Random.Range(-amount, amount);
        _targetSteering = Mathf.Clamp(_targetSteering, -maxSteeringOffset, maxSteeringOffset);
    }
}