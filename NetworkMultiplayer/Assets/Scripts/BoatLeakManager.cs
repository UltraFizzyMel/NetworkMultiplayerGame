using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.VisualScripting;
using System.Collections.Generic;

public class BoatLeakManager : NetworkBehaviour
{
    [Header("Water Settings")]
    public float currentWaterLevel = 0f;
    public float maxWaterLevel = 100f;
    public float minWaterLevel = 0f;
    public float leakRate = 0.01f;
    public int activeLeaks = 0;
    public GameObject waterPlane;

    public GameObject leakPrefab;
    public float leakInterval = 15f;
    public bool bucketUsed;
    public bool bucketRebound;
    public BucketController bucketController;
    private Leak leak;
    [SerializeField] private List<LeakLocations> leakLocationsList;
    private LeakLocations leakLocation;


    private void Start()
    {
        bucketController = GameObject.Find("TempBucket").GetComponent<BucketController>();
    }

    public override void OnNetworkSpawn()
    {
        RequestLeakSpawnServerRpc();
    }

    // Update is called once per frame
    void Update()
    {
        if (bucketUsed) 
        {
            currentWaterLevel -= bucketController.bucketCapacity;
            if(currentWaterLevel <= 0f ) { currentWaterLevel = 0f; }
        }

        if (bucketRebound)
        {
            currentWaterLevel += bucketController.bucketCapacity;
            if (currentWaterLevel >= bucketController.bucketCapacity) { currentWaterLevel = bucketController.bucketCapacity; }
        }

        SetWaterLevel(currentWaterLevel);

        if (activeLeaks > 0)
        {
            if (currentWaterLevel < maxWaterLevel)
            {
                waterPlane.transform.Translate(Vector3.up * leakRate * activeLeaks * Time.deltaTime);
                currentWaterLevel = waterPlane.transform.position.y;
            } 
        }
    }

    // used when a leak is spawned
    public void AddLeak()
    { activeLeaks++; }

    public void RepairLeak() 
    { activeLeaks = Mathf.Max(0, activeLeaks - 1); }

    [ServerRpc]
    public void RequestLeakSpawnServerRpc()
    {
        StartCoroutine(SpawnLeaks());
    }

    IEnumerator SpawnLeaks()
    {
        while (true)
        {
            float time = Random.Range(leakInterval, leakInterval + 5);

            yield return new WaitForSeconds(time);
            GameObject leakInstance = Instantiate(leakPrefab, PickRandomSurface(), leakLocation.SetLeakRotation()/*, leakLocation.gameObject.transform*/);// create a leak
            leak =leakInstance.GetComponent<Leak>();
            leakInstance.GetComponent<NetworkObject>().Spawn();
            leak.boatLeakManager = this; // Connect the leak to this leak manaager

            AddLeak();
        }
    }

   // IEnumerator BucketCooldown()



    public void SetWaterLevel(float currentWaterLevel)
    {
        waterPlane.transform.position = new Vector3(waterPlane.transform.position.x, currentWaterLevel, waterPlane.transform.position.z);
    }

    public void RemoveWater(float bucketCapacity)
    {
        currentWaterLevel = currentWaterLevel - bucketCapacity;
        if (currentWaterLevel <= 0f) { currentWaterLevel = 0f; }
        waterPlane.transform.position = new Vector3(waterPlane.transform.position.x, currentWaterLevel, waterPlane.transform.position.z);
    }

    private Vector3 PickRandomSurface()
    {
        
        if (leakLocationsList.Count > 0)
        {
            int randomIndex = Random.Range(0, leakLocationsList.Count);
            leakLocation = leakLocationsList[randomIndex];
                
        }
        return leakLocation.FindRandomLeakLocation();
    }
}
