using UnityEngine;
using Unity.Netcode;

public class BucketController : NetworkBehaviour
{
    public bool isFull = false;

    public GameObject waterVisual;
    public float bucketCapacity = 0.5f;

    public void Fill()
    {
        isFull = true;
        if (waterVisual != null) waterVisual.SetActive(true);
    }

    public void Empty()
    {
        isFull = false;
        if (waterVisual != null) waterVisual.SetActive(false);
    }



}
