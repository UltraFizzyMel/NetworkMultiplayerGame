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
           if(player.GetObjectPickUp().TryGetComponent<BucketController>(out BucketController bucketController))
           {
                //The player has a bucket
                if (bucketController.isFull)
                {
                    Debug.Log("Player has full Bucket");
                    return;
                }
                else
                {
                    TryGetComponent<BoatLeakManager>(out BoatLeakManager boatLeakManager);
                    DeckManager.RemoveWater(bucketController.bucketCapacity);
                    bucketController.isFull = true;
                    Debug.Log("Player bucket has been Filled");
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
