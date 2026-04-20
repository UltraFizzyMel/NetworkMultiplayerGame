using System;
using Unity.Netcode;
using UnityEngine;

public class BucketZone : Interactable, IObjectPickUpParent
{
    [SerializeField] private ObjectPickUpSO objectPickUpSO;
    
    //[SerializeField] private NetworkPlayer networkPlayer;
    //[SerializeField] private BucketController bucketController;




    public override void Interact(NetworkPlayer networkPlayer)
    {
        Debug.Log("Interact!!");
        if (!HasObjectPickUp())
        {
            //There is no pickup here
            Debug.Log("No Pick-up");
            if (networkPlayer.HasObjectPickUp()) {
                //Player has an object in their hands
                networkPlayer.GetObjectPickUp().SetObjectPickUpParent(this);
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
            if (networkPlayer.HasObjectPickUp())
            {
                //player is carrying something
                Debug.Log("Carrying!!");
            }
            else
            {
                //player not carrying pickup
                GetObjectPickUp().SetObjectPickUpParent(networkPlayer);
                ClearObjectPickUp();
                Debug.Log("Empty!!");
            }
        }
        //Transform objectPickUpTransform = Instantiate(objectPickUpSO.prefab, bucketHoldPoint); //Instantiate object
        //objectPickUpTransform.GetComponent<ObjectPickUp>().SetBucketZone(this);
    }

}
