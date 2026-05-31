using System;
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
        NetworkVariableWritePermission.Server);

    public static BoatMovement Instance { get; private set; }

    //[SerializeField] private float boatDamagePenalty;

    [Header("Damage")]
    [SerializeField] private float maxBoatDamage = 1f;

    private float currentBoatDamage;

    public override void OnNetworkSpawn()
    {
        Instance = this;
    }

    public void Update()
    {
        if (!IsServer) return;

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
        }*/
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

        currentBoatDamage += Mathf.Clamp(currentBoatDamage + amount, 0f, maxBoatDamage);

        Debug.Log($"Boat damage: {currentBoatDamage}");
    }

    public bool CheckWinCondition()
    {
        if (boatProgress.Value >= boatProgressMax)
            return true;
        else
            return false;
    }
}
