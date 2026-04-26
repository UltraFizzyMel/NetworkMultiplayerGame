using UnityEngine;

public class LeakLocations : MonoBehaviour
{
    [SerializeField] private bool hasVariableX;
    [SerializeField] private bool hasVariableZ;
    

    [SerializeField] private bool usesMaxX;
    [SerializeField] private bool usesMaxZ;

    [SerializeField] private float xAdjustment;
    [SerializeField] private float yAdjustment;
    [SerializeField] private float zAdjustment;
    //[SerializeField] private float boundsAdjustment = 1f;//adjustment to prevent leak from appearing as if off the surface
    //[SerializeField] private float surfaceAdjustment = 0.01f;//Adjustment to place leak above the surface
    [SerializeField] public float rotationAdjustment;


    // [SerializeField] Bounds leakBounds;
    private Vector3 randomSpawnPosition;
   // private Renderer renderer;
    


    public void Start()
    {
        GetLeakSurface();
    }

    public void GetLeakSurface()
    {
        //renderer = GetComponent<Renderer>();
        //Debug.Log(renderer);
        //if (renderer != null) return;
        //leakBounds = renderer.bounds;
    }

    public Vector3 FindRandomLeakLocation()
    {
        
        float Z = 0;
        float X = 0;
        if (hasVariableX)
        {
            if (usesMaxX)
            { Z = this.transform.position.z + zAdjustment; }
            else { Z = this.transform.position.z - zAdjustment; }

            X = Random.Range((this.transform.position.x - xAdjustment), (this.transform.position.x + xAdjustment));

        }
        else if (hasVariableZ)
        {
            if (usesMaxZ)
            { X = this.transform.position.x + xAdjustment*2; }
            else { X = this.transform.position.x - xAdjustment; }

            Z = Random.Range((this.transform.position.z - zAdjustment), (this.transform.position.z + zAdjustment));
        }
        float randomY = Random.Range((this.transform.position.y - yAdjustment), (this.transform.position.y + yAdjustment));
        Debug.Log(randomY);
        
        randomSpawnPosition = new Vector3(X, randomY, Z);
        return randomSpawnPosition;
    }

    public Quaternion SetLeakRotation()
    {
        float y;
        if (hasVariableX)
        { if (usesMaxX)
            { y = 0 + rotationAdjustment; }
            else
            { y = 180 + rotationAdjustment; }
        }
        else
        { if (usesMaxZ)
            { y = 90 + rotationAdjustment; }
            else
            { y = 270 + rotationAdjustment; }
        }


            return Quaternion.Euler(90, y, 0); 
    }

    private void OnDrawGizmos()
    {
        //Gizmos.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
        Gizmos.matrix = transform.localToWorldMatrix;
        { Gizmos.DrawWireCube(this.transform.position - new Vector3(0,0,0), (new Vector3(xAdjustment, yAdjustment, zAdjustment))*2); }

    }



    /*float Z = 0;
        float X = 0;
        if (hasVariableX) 
        { 
            if (usesMaxZ)
            { Z = leakBounds.max.z + surfaceAdjustment; }
            else { Z = leakBounds.min.z - surfaceAdjustment; }

            X = Random.Range(((leakBounds.min.x) + boundsAdjustment), (leakBounds.max.x - boundsAdjustment));
            
        }
        else if (hasVariableZ) 
        {
            if (usesMaxX)
            { X = leakBounds.max.x + surfaceAdjustment; }
            else { X = leakBounds.min.x - surfaceAdjustment; }

            Z = Random.Range(((leakBounds.min.z) + boundsAdjustment), (leakBounds.max.z - boundsAdjustment));
        }
        
        float randomY = Random.Range(((leakBounds.min.y) + boundsAdjustment), (leakBounds.max.y - boundsAdjustment));
        */

}
