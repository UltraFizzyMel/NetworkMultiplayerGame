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
    [SerializeField] private float fuelingProgress = 0f;
    [SerializeField] private float fuelDecayProgess = -0.3f;
    [SerializeField] private float fuelRate = 0.5f;
    private bool isFueling;
    [SerializeField] private GameObject fuelUI;

    public void Update()
    {
        float fuelChange = 0;
        if (isFueling) { fuelChange = fuelRate; }
        else { if (fuelingProgress > 0) { fuelChange = fuelDecayProgess; } }
        fuelingProgress += fuelChange * Time.deltaTime;
        OnFuelChanged?.Invoke(this, new OnFuelChangedEventArgs { fuelNormalized = fuelingProgress / fuelMax });

        if (fuelingProgress >= fuelMax)
        {
            return;
        }
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
            isFueling = true;
            Debug.Log("Player has no item");
            return;
        }
    }

    public override void Cancel(Player player)
    {
        isFueling = false;

    }

}
