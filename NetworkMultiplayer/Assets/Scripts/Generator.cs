using System;
using Unity.Netcode;
using UnityEngine;

public class Generator : Interactable
{
    //public BoatLeakManager boatLeakManager;

    public event EventHandler<OnFuelChangedEventArgs> OnFuelChanged;
    public class OnFuelChangedEventArgs : EventArgs
    {
        public float fuelNormalized;
    }

    [SerializeField] private float fuelMax = 5f;
    //[SerializeField] private float fuelingProgress = 0f;
    [SerializeField] private float fuelDecayProgess = -0.3f;
    [SerializeField] private float fuelRate = 0.5f;
    //private bool isFueling;
    [SerializeField] private GameObject fuelUI;

    public NetworkVariable<float> fuelingProgress = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isFueling = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public void Update()
    {
        if (IsSpawned) { GeneratorServerRpc(); }
    }

    public override void Interact(Player player)
    {

        if (player.HasObjectPickUp())
        {
           
            Debug.Log("Player is holding Something");

        }
        else
        {
            // The player is not holding something
            IsFuelingRpc();
            Debug.Log("Player has no item");
            return;
        }
    }

    public override void Cancel(Player player)
    {
        IsNotFuelingRpc();

    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void GeneratorServerRpc()
    {
        float fuelChange = 0;//The value of the fuel change
        if (isFueling.Value) // The player is fueling the generator
        {
            if (fuelingProgress.Value < fuelMax) { fuelChange = fuelRate; }// if the current fuel level is less than max fuel then the fuel rate will be positive
        }
        else { if (fuelingProgress.Value > 0) { fuelChange = fuelDecayProgess; } }// While player is not fueling generator the fuel level goes down until it reaches 0
        fuelingProgress.Value += fuelChange * Time.deltaTime;// The current fuel level changes over time based on the if-else statement above
        OnFuelChanged?.Invoke(this, new OnFuelChangedEventArgs { fuelNormalized = fuelingProgress.Value / fuelMax });

        if (fuelingProgress.Value >= fuelMax)
        {
            return;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void IsFuelingRpc()
    {
        isFueling.Value = true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void IsNotFuelingRpc()
    {
        isFueling.Value = false;
    }


}
