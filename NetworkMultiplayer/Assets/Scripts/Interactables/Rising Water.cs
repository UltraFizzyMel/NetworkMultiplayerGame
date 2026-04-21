using Unity.VisualScripting;
using UnityEngine;

public class RisingWater : Interactable
{
    public override void Interact(Player player)
    {
        if (player.HasObjectPickUp())
        {
            //The player is holding something
           if(GetObjectPickUp().TryGetComponent<BucketController>(out BucketController bucketController))
           {
                //The player has a bucket
                if (bucketController.isFull)
                {
                    Debug.Log("Player has full Bucket");
                    return;
                }
                else
                {
                    this.TryGetComponent<BoatLeakManager>(out BoatLeakManager boatLeakManager);
                    bucketController.isFull = true;
                    Debug.Log("Player has empty Bucket");
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
}
