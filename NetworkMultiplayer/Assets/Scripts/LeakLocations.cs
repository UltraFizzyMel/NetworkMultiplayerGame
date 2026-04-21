using UnityEngine;

public class LeakLocations : MonoBehaviour
{
    [SerializeField] private bool hasVariableX;
    [SerializeField] private bool hasVariableZ;
    

    [SerializeField] private bool usesMaxX;
    [SerializeField] private bool usesMaxZ;
    [SerializeField] private float boundsAdjustment = 1f;//adjustment to prevent leak from appearing as if off the surface
    [SerializeField] private float surfaceAdjustment = 0.01f;//Adjustment to place leak above the surface

    [SerializeField] Bounds bounds;
    private Vector3 randomSpawnPosition;
    private Renderer renderer;


    public void Start()
    {
        GetLeakSurface();
    }

    public void GetLeakSurface()
    {
        renderer = GetComponent<Renderer>();
        Debug.Log(renderer);
        if (renderer != null) return;
        bounds = renderer.localBounds;
    }

    public Vector3 FindRandomLeakLocation()
    {
        float Z = 0;
        float X = 0;
        if (hasVariableX) 
        { 
            if (usesMaxZ)
            { Z = bounds.max.z + surfaceAdjustment; }
            else { Z = bounds.min.z - surfaceAdjustment; }

            X = Random.Range(((bounds.min.x) + boundsAdjustment), (bounds.max.x - boundsAdjustment));
            
        }
        else if (hasVariableZ) 
        {
            if (usesMaxX)
            { X = bounds.max.x + surfaceAdjustment; }
            else { X = bounds.min.x - surfaceAdjustment; }

            Z = Random.Range(((bounds.min.z) + boundsAdjustment), (bounds.max.z - boundsAdjustment));
        }
        
        float randomY = Random.Range(((bounds.min.y) + boundsAdjustment), (bounds.max.y - boundsAdjustment));

        Debug.Log(randomY);
        Debug.Log(Z);
        Debug.Log(Z);
        randomSpawnPosition = new Vector3(X, randomY, Z);
        return randomSpawnPosition;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
    }


}
