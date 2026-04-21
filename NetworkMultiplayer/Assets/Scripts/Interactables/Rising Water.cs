using UnityEngine;

public class RisingWater : Interactable
{
    public override void Interact(Player player)
    {
        if (player.HasObjectPickUp())
        {
           GetObjectPickUp();
        }
        else
        {
            return;
        }
    }
}
