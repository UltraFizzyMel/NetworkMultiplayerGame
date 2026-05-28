using System;
using Unity.Netcode;
using UnityEngine;

public class BucketZone : Interactable, IObjectPickUpParent
{
    [SerializeField] private ObjectPickUpSO objectPickUpSO;
    
    //[SerializeField] private Player player;
    //[SerializeField] private BucketController bucketController;




    public override void Interact(Player player)
    {
        Debug.Log("Interact!!");
        if (!HasObjectPickUp())
        {
            //There is no pickup here
            Debug.Log("No Pick-up");
            if (player.HasObjectPickUp()) {
                //Player has an object in their hands                
                player.GetObjectPickUp().SetObjectPickUpParent(this);

                if (MusicManager.Instance != null)
                {
                    if (player.GetObjectPickUp().TryGetComponent<BucketController>(out BucketController bucketController))
                        MusicManager.Instance.PlaySFX(SFXType.PickupBucket);
                    else if (player.GetObjectPickUp().TryGetComponent<TapeController>(out TapeController tapeController))
                        MusicManager.Instance.PlaySFX(SFXType.PickupDuctTape);
                }                 
                
                Debug.Log("Carrying!!");
            }
            else {
                //player not carrying anything
                Debug.Log("Empty!!");
            }
        }
        else
        {
            //There is a pick-up here
            Debug.Log("There is a Pick-up!!");
            if (player.HasObjectPickUp())
            {
                //player is carrying something
                Debug.Log("Carrying!!");
            }
            else
            {
                //player not carrying pickup
                GetObjectPickUp().SetObjectPickUpParent(player);
                ClearObjectPickUp();
                Debug.Log("Empty!!");
            }
        }
        //Transform objectPickUpTransform = Instantiate(objectPickUpSO.prefab, bucketHoldPoint); //Instantiate object
        //objectPickUpTransform.GetComponent<ObjectPickUp>().SetBucketZone(this);
    }

}
