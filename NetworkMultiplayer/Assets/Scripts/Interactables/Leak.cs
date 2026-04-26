using System;
using Unity.Netcode;
using UnityEngine;

public class Leak : Interactable
{
    public BoatLeakManager boatLeakManager;

    public event EventHandler<OnProgressChangedEventArgs> OnProgressChanged;
    public class OnProgressChangedEventArgs : EventArgs
    {
        public float progressNormalized;
    }

    [SerializeField] private float fixingProgressMax = 5f;
    [SerializeField] private float fixingProgress = 0f;
    [SerializeField] private float decayProgess = -0.3f;
    [SerializeField] private float fixingRate = 0.5f;
    private bool isFixing;
    [SerializeField] private GameObject leakUI;

    public void Update()
    {
        float progress = 0;
        if (isFixing) { progress = fixingRate; }
        else { if (fixingProgress > 0) { progress = decayProgess; } }
        fixingProgress += progress * Time.deltaTime;
        OnProgressChanged?.Invoke(this, new OnProgressChangedEventArgs { progressNormalized = fixingProgress/fixingProgressMax });
        
        if (fixingProgress >= fixingProgressMax)
        {
           RequestDestroyServerRpc();
        }
    }

    public override void Interact(Player player)
    {

        if (player.HasObjectPickUp())
        {
            //The player is holding something
            if (player.GetObjectPickUp().TryGetComponent<TapeController>(out TapeController tapeController))
            {
                //The player is holding tape
                isFixing = true;
                Debug.Log("Fixing");
            }
            

        }
        else
        {
            // The player is not holding something
            Debug.Log("Player has no item");
            return;
        }
    }

    public override void Cancel(Player player)
    {
        isFixing = false;
       
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDestroyServerRpc()
    {
        RequestDestroyClientRpc();
    }

    [ClientRpc]
    public void RequestDestroyClientRpc()
    {
        DestroySelf();
    }

    public void DestroySelf()
    {
        if (boatLeakManager == null || gameObject == null) return;
        boatLeakManager.RepairLeak();
        Destroy(gameObject);
    }
}
