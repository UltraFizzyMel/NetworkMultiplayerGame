using UnityEngine;

public class BucketZone : MonoBehaviour
{
    [SerializeField] private Transform bucketPrefab;
    [SerializeField] private Transform bucketPlacement;
    [SerializeField] private NetworkPlayer networkPlayer;
    //[SerializeField] private BucketController bucketController;
    public void Interact()
    {
        Debug.Log("Interact");
        
        Transform bucketTransform = Instantiate(bucketPrefab, bucketPlacement);
        bucketTransform.localPosition = Vector3.zero;
        
    }
}
