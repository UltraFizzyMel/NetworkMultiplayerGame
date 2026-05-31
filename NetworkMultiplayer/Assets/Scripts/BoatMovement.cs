/*using System;
using Unity.Netcode;
using Unity.Services.Qos.V2.Models;
using UnityEngine;

public class BoatMovement : NetworkBehaviour
{
    public event EventHandler<OnBoatMovedEventArgs> OnBoatMoved;
    
    public class OnBoatMovedEventArgs : EventArgs
    {
        public float progressNormalized;
    }

     public float boatProgressMax = 5f;
    //[SerializeField] private float boatProgress = 0f;
    [SerializeField] private float boatSpeed = 0.5f;
    //[SerializeField] private float boatMovementSpeed = 2f;
   // private bool isMoving;
    [SerializeField] private GameObject PlayerUI;
    [SerializeField ] private Generator generator;

    public NetworkVariable<float> boatProgress = new(
       0f,
       NetworkVariableReadPermission.Everyone,
       NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> netCurrentMoveSpeed = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public static BoatMovement Instance { get; private set; }

    //[SerializeField] private float boatDamagePenalty;

    [Header("Damage")]
    [SerializeField] private bool applySlowOnDamage = true;
    [SerializeField] private float maxBoatDamage = 1f;

    private float currentBoatDamage;

    public override void OnNetworkSpawn()
    {
        Instance = this;
        Debug.Log("[BoatMovement] Spawned");
    }

    public void Update()
    {
        if (!IsServer) return;

        if (generator == null)
            generator = FindFirstObjectByType<Generator>();

        if (GameManager.Instance == null ||!GameManager.Instance.GameReady()) return;

        //if (IsSpawned) BoatMovementRpc();
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        float fuelPercent = generator.GetFuelNormalized();
        float damageMultiplier = 1f - currentBoatDamage;
        float speed = boatSpeed * fuelPercent * damageMultiplier;

        netCurrentMoveSpeed.Value = speed;

        //float progress = boatSpeed * fuelPercent * damageMultiplier;
        boatProgress.Value += speed * Time.deltaTime;

        OnBoatMoved?.Invoke(this, new OnBoatMovedEventArgs
        {
            progressNormalized = boatProgress.Value / boatProgressMax
        });
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void BoatMovementRpc()
    {
        float fuelPercent = generator.GetFuelNormalized();
        float damageMultiplier = 1f - currentBoatDamage;

        float progress = 0;
        /*if (generator.FuelCheck()) 
        { 
            progress = boatSpeed;
           // Debug.Log("Moving!!!");
        }
        else 
        {  
            progress = 0;
            //Debug.Log("Not Moving");
        }/
        //progress = boatSpeed * fuelPercent;
        progress = boatSpeed * fuelPercent * damageMultiplier;

        netCurrentMoveSpeed.Value = progress;
        boatProgress.Value += progress * Time.deltaTime;
        OnBoatMoved?.Invoke(this, new OnBoatMovedEventArgs { progressNormalized = boatProgress.Value / boatProgressMax });
    }

    public void ApplyPermanentSlow(float amount)
    {
        if (!IsServer)
            return;

        if (!applySlowOnDamage)
            return;

        currentBoatDamage = Mathf.Clamp(currentBoatDamage + amount, 0f, maxBoatDamage);
        Debug.Log($"[BoatMovement] Damage: {currentBoatDamage:F2} / speed multiplier: {1f - currentBoatDamage:F2}");
    }

    public bool CheckWinCondition()
    {
        if (boatProgress.Value >= boatProgressMax)
            return true;
        else
            return false;
    }
}*/

using System;
using Unity.Netcode;
using UnityEngine;

public class BoatMovement : NetworkBehaviour
{
    public static BoatMovement Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Generator generator;
    [SerializeField] private Transform lighthouse;

    [Header("Movement")]
    [SerializeField] private float boatSpeed = 0.5f;

    [Header("Damage")]
    [SerializeField] private bool applySlowOnDamage = true;
    [SerializeField] private float maxBoatDamage = 1f;
    private float _currentBoatDamage;

    public NetworkVariable<float> distanceToLighthouse = new(
        200f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> netCurrentMoveSpeed = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> netStartDistance = new(
        200f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public event EventHandler<OnBoatMovedEventArgs> OnBoatMoved;
    public class OnBoatMovedEventArgs : EventArgs
    {
        public float progressNormalized;
    }

    // ─── Network spawn ───────────────────────────────────────────────────────
    public override void OnNetworkSpawn()
    {
        Instance = this;

        if (!IsServer) return;

        if (generator == null)
            generator = FindFirstObjectByType<Generator>();

        float startDist = lighthouse != null
            ? Vector3.Distance(transform.position, lighthouse.position)
            : 200f;

        netStartDistance.Value = startDist;
        distanceToLighthouse.Value = startDist;
    }

    // ─── Update (server only) ────────────────────────────────────────────────
    private void Update()
    {
        if (!IsServer) return;
        if (GameManager.Instance == null || !GameManager.Instance.GameReady()) return;
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        float fuelPercent = generator.GetFuelNormalized();
        float damageMultiplier = 1f - _currentBoatDamage;
        float speed = boatSpeed * fuelPercent * damageMultiplier;

        netCurrentMoveSpeed.Value = speed;
        distanceToLighthouse.Value = Mathf.Max(distanceToLighthouse.Value - speed * Time.deltaTime, 0f);

        OnBoatMoved?.Invoke(this, new OnBoatMovedEventArgs
        {
            progressNormalized = GetProgressNormalized()
        });
    }

    // ─── Public API ──────────────────────────────────────────────────────────
    public float GetProgressNormalized()
    {
        // Guard against division by zero before the server has synced netStartDistance
        if (netStartDistance.Value <= 0f) return 0f;
        return 1f - (distanceToLighthouse.Value / netStartDistance.Value);
    }

    public void ApplyPermanentSlow(float amount)
    {
        if (!IsServer || !applySlowOnDamage) return;

        _currentBoatDamage = Mathf.Clamp(_currentBoatDamage + amount, 0f, maxBoatDamage);
        Debug.Log($"[BoatMovement] Damage: {_currentBoatDamage:F2}, multiplier: {1f - _currentBoatDamage:F2}");
    }

    public bool CheckWinCondition() => distanceToLighthouse.Value <= 0f;
}