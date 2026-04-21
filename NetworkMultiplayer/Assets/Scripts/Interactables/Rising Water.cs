using UnityEngine;

public class RisingWater : Interactable
{
    public override void Interact(NetworkPlayer networkPlayer)
    {
        if (networkPlayer.HasObjectPickUp())
        {
           GetObjectPickUp();
        }
        else
        {
            return;
        }
    }
}
