using UnityEngine;

public class OffBoat : Interactable
{
    [SerializeField] private BucketController bucketController;
    //[SerializeField] private BoatLeakManager boatLeakManager;
    public override void Interact(Player player)
    {

        if (player.HasObjectPickUp())
        {
            //The player is holding something
            if (player.GetObjectPickUp().TryGetComponent<BucketController>(out BucketController bucketController))
            {
                //The player has a bucket
                if (bucketController.isFull.Value)
                {
                    //Debug.Log("Player Bucket Emptied");
                    Debug.Log($"[OffBoat]: Bucket full state after emptying: {bucketController.isFull.Value}");
                    //MusicManager.Instance.PlaySFX(SFXType.WaterOutBucket);
                    player.animator.Play("Captain|BucketThrow");
                    //bucketController.isFull.Value = false;
                    bucketController.EmptyServerRpc();
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

    public override string GetInteractText(Player player)
    {
        if (!player.HasObjectPickUp())
            return "";

        if (player.GetObjectPickUp().TryGetComponent<BucketController>(out BucketController bucket))
        {
            if (bucket.isFull.Value)
                return interactText;
        }

        return "";
    }
}
