using Unity.VisualScripting;
using UnityEngine;

public class RisingWater : Interactable
{
    [SerializeField] private BucketController bucketController;
    [SerializeField] private BoatLeakManager DeckManager;
    public override void Interact(Player player)
    {
        
        if (player.HasObjectPickUp())
        {
            //The player is holding something
           if(player.GetObjectPickUp().TryGetComponent<BucketController>(out BucketController heldBucket))
           {
                //The player has a bucket
                if (heldBucket.isFull.Value)
                {
                    Debug.Log("Player has full Bucket");
                    return;
                }
                else
                {
                    TryGetComponent<BoatLeakManager>(out BoatLeakManager boatLeakManager);
                    DeckManager.RemoveWaterServerRpc(heldBucket.bucketCapacity);
                    //heldBucket.isFull.Value = true;
                    heldBucket.FillServerRpc();
                    if (MusicManager.Instance != null)
                        MusicManager.Instance.PlaySFX(SFXType.WaterInBucket);
                    Debug.Log($"[RisingWater]: Bucket full state: {heldBucket.isFull.Value}");
                    //Debug.Log("Player bucket has been Filled");
                }
           }
           else
           {
                //The player does not have a bucket
                Debug.Log("Player has no bucket");
                return;
           }
            
        }
        else
        {
            // The player is not holding something
            Debug.Log("Player has no item");
            return;
        }
    }

    public override string GetInteractText(Player player)
    {
        if (BoatLeakManager.Instance == null || !BoatLeakManager.Instance.shouldShowWater)
            return "";

        if (!player.HasObjectPickUp())
        {
            if (BoatLeakManager.Instance.currentWaterLevel.Value > 5f)
                return altText;

            return "";
        }

        if (player.GetObjectPickUp().TryGetComponent<BucketController>(out BucketController bucket))
        {
            if (!bucket.isFull.Value)
                return interactText;
            else
                return "Empty Bucket";
        }

        return "";
    }
}
