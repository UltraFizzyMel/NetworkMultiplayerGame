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

    public void Update()
    {
        if (!IsServer) return;
        if (IsSpawned) { BoatMovementRpc(); }
        

    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void BoatMovementRpc()
    {
        float progress = 0;
        if (generator.FuelCheck()) { progress = boatSpeed;
            Debug.Log("Moving!!!");
        }
        else {  progress = 0;
            Debug.Log("Not Moving");
        }
        boatProgress.Value += progress * Time.deltaTime;
        OnBoatMoved?.Invoke(this, new OnBoatMovedEventArgs { progressNormalized = boatProgress.Value / boatProgressMax });
    }


}
