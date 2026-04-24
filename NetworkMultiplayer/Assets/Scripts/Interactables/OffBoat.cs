using UnityEngine;

public class OffBoat : Interactable
{
    [SerializeField] private BucketController bucketController;
    [SerializeField] private BoatLeakManager boatLeakManager;
    public override void Interact(Player player)
    {

        if (player.HasObjectPickUp())
        {
            //The player is holding something
            if (player.GetObjectPickUp().TryGetComponent<BucketController>(out BucketController bucketController))
            {
                //The player has a bucket
                if (bucketController.isFull)
                {
                    Debug.Log("Player Bucket Emptied");
                    bucketController.isFull = false;
                    
                }
                else
                {
                    
                   
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
