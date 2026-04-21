using UnityEngine;

public class RisingWater : Interactable
{
    public override void Interact(Player player)
    {
        if (player.HasObjectPickUp())
        {
            //The player is holding something
           if(GetObjectPickUp().GetComponent<BucketController>())
           {
                //The player has a bucket
           }
           else
           {
                //The player does not have a bucket
                return;
           }
            
        }
        else
        {
            // The player is not holding something
            return;
        }
    }
}
