using System;
using Unity.Netcode;
using UnityEngine;

public class Generator : Interactable
{
    //public BoatLeakManager boatLeakManager;

    public GameObject generatorAudio;

    public event EventHandler<OnFuelChangedEventArgs> OnFuelChanged;
    public class OnFuelChangedEventArgs : EventArgs
    {
        public float fuelNormalized;
    }

    public float fuelMax = 5f;// The maxium amount of fuel
    [SerializeField] private float fuelDecayRate = -0.3f;//rate at which fuel bar will decrease
    [SerializeField] private float fuelFillRate = 0.5f;//rate at which fuel bar will increase
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

    [SerializeField] private bool speedChange = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Start with a full tank so the boat moves from the moment the game begins.
        // Previously the default was 0, making GetFuelNormalized() always return 0 and netCurrentMoveSpeed always stay at 0.
        fuelingProgress.Value = fuelMax;
    }

    public void Update()
    {
        if (!IsServer || !IsSpawned) return;
        //if (IsSpawned) { GeneratorServerRpc(); }

        //SendTo.Server RPC  adds unnecessary message-system overhead.
        //We already check to make sure it's the server so it's cheaper to just call the method directly.
        UpdateFuel();
    }

    // ─── Fuel logic (server only) ────────────────────────────────────────────
    private void UpdateFuel()
    {
        float fuelChange = 0f;

        if (isFueling.Value)
        {
            if (fuelingProgress.Value < fuelMax)
                fuelChange = fuelFillRate;
        }
        else
        {
            if (fuelingProgress.Value > 0f)
                fuelChange = fuelDecayRate;
        }

        fuelingProgress.Value = Mathf.Clamp(
            fuelingProgress.Value + fuelChange * Time.deltaTime,
            0f,
            fuelMax);

        // Audio: active only while fuel is actually changing
        if (generatorAudio != null)
            generatorAudio.SetActive(fuelChange != 0f);

        OnFuelChanged?.Invoke(this, new OnFuelChangedEventArgs
        {
            fuelNormalized = fuelingProgress.Value / fuelMax
        });
    }

    // ─── Interactions ────────────────────────────────────────────────────────
    public override void Interact(Player player)
    {
        // Checks to see if the player is holding an object
        if (player.HasObjectPickUp())
        {
           
            Debug.Log("Player is holding Something - cannot fuel");
            return;

        }
        // The player is not holding something - player can fuel generator
        IsFuelingRpc();

        if (MusicManager.Instance != null)
            MusicManager.Instance.PlaySFX(SFXType.GeneratorFixed);
    }

    public override void Cancel(Player player)
    {
        IsNotFuelingRpc();

    }

    /*[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void GeneratorServerRpc()
    {
        float fuelChange = 0;//The value of the fuel change
        if (isFueling.Value) // The player is fueling the generator
        {
            if (fuelingProgress.Value < fuelMax) { fuelChange = fuelFillRate; }// if the current fuel level is less than max fuel then the fuel rate will be positive
        }
        else { if (fuelingProgress.Value > 0) { fuelChange = fuelDecayRate; } }// While player is not fueling generator the fuel level goes down until it reaches 0
        
        fuelingProgress.Value += fuelChange * Time.deltaTime;// The current fuel level changes over time based on the if-else statement above
        // Clamps the fuel level between 0 and the max fuel level
        fuelingProgress.Value = Mathf.Clamp(
            fuelingProgress.Value,
            0f,
            fuelMax
        );
        //OnFuelChanged?.Invoke(this, new OnFuelChangedEventArgs { fuelNormalized = fuelingProgress.Value / fuelMax });

        if (fuelChange == 0 )
        {
            generatorAudio.SetActive(false);
        }
        else
        {
            generatorAudio.SetActive(true);
        }

        if (fuelingProgress.Value >= fuelMax)
        {
            return;
        }
    }*/

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

    public bool FuelCheck()
    {
        if (fuelingProgress.Value > 0f)
            return true;
        else 
            return false;
    }

    public float GetFuelNormalized()
    {
        if (speedChange)
            return Mathf.Clamp01(fuelingProgress.Value / fuelMax);

        return FuelCheck() ? 1f : 0f;
    }
}