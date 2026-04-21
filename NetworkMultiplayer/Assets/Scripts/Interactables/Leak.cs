using UnityEngine;

public class Leak : Interactable
{
    public BoatLeakManager boatLeakManager;

    private float fixingProgressMax;

    public override void Interact(Player player)
    {

        if (player.HasObjectPickUp())
        {
            //The player is holding something
            

        }
        else
        {
            // The player is not holding something
            Debug.Log("Player has no item");
            return;
        }
    }

    public void DestroySelf()
    {
        boatLeakManager.RepairLeak();
        Destroy(gameObject);
    }
}
