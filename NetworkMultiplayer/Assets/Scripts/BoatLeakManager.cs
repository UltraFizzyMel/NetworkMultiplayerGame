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
        if (!IsServer) return;
        StartCoroutine(WaitForGameReady());
        //RequestLeakSpawnServerRpc();
    }

    private IEnumerator WaitForGameReady()
    {
        yield return new WaitUntil(() => GameManager.Instance != null && GameManager.Instance.GameReady());

        StartCoroutine(SpawnLeaks());
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;
        if (IsSpawned)
        { 
            WaterLevelServerRpc();
            RisingWaterServerRpc();
        }
    }

    // used when a leak is spawned
    public void AddLeak()
    { 
        activeLeaks++; 
    }

    public void RepairLeak()
    { 
        activeLeaks = Mathf.Max(0, activeLeaks - 1); 
    }

    //[ServerRpc]
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestLeakSpawnServerRpc()
    {
        //RequestLeakSpawnClientRpc();
        StartCoroutine(SpawnLeaks());
    }

    /*[ClientRpc]
    public void RequestLeakSpawnClientRpc()
    {
        StartCoroutine(SpawnLeaks());
    }*/

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void WaterLevelServerRpc()
    {
       // WaterLevelClientRpc();
        SetWaterLevel(currentWaterLevel);
    }

    [ClientRpc]
    public void WaterLevelClientRpc()
    {
        //SetWaterLevel(currentWaterLevel);
    }

    IEnumerator SpawnLeaks()
    {
        while (true)
        {
            float time = Random.Range(leakInterval, leakInterval + 5);

            yield return new WaitForSeconds(time);
            // create a leak
            GameObject leakInstance = Instantiate(leakPrefab, PickRandomSurface(), leakLocation.SetLeakRotation()); //, leakLocation.gameObject.transform);
            leakInstance.transform.RotateAround(leakLocation.transform.position, Vector3.up, leakLocation.rotationAdjustment);
            leak =leakInstance.GetComponent<Leak>();
            leakInstance.GetComponent<NetworkObject>().Spawn();
            leak.boatLeakManager = this; // Connect the leak to this leak manaager

            AddLeak();
        }
    }

    public void SpawnImmediateLeaks(int amount)
    {
        if (!IsServer)
            return;

        for (int i = 0; i < amount; i++)
        {
            GameObject leakInstance =
                Instantiate(
                    leakPrefab,
                    PickRandomSurface(),
                    leakLocation.SetLeakRotation()
                );

            leakInstance.transform.RotateAround(
                leakLocation.transform.position,
                Vector3.up,
                leakLocation.rotationAdjustment
            );

            leak = leakInstance.GetComponent<Leak>();

            leakInstance.GetComponent<NetworkObject>().Spawn();

            leak.boatLeakManager = this;

            AddLeak();
        }

        Debug.Log($"[BoatLeakManager] Spawned {amount} collision leaks.");
    }

    // IEnumerator BucketCooldown()


    public void SetWaterLevel(float currentWaterLevel)
    {
        waterPlane.transform.position = new Vector3(waterPlane.transform.position.x, currentWaterLevel, waterPlane.transform.position.z);
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RisingWaterServerRpc()
    {
        RisingWaterClientRpc();
    }

    [ClientRpc]
    public void RisingWaterClientRpc()
    {
        RisingWater();
    }
    public void RisingWater()
    {
        if (activeLeaks > 0)
        {
            if (currentWaterLevel < maxWaterLevel)
            {
                waterPlane.transform.Translate(activeLeaks * leakRate * Time.deltaTime * Vector3.up);
                currentWaterLevel = waterPlane.transform.position.y;
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RemoveWaterServerRpc(float bucketCapacity)
    {
        //RequestLeakSpawnClientRpc();
        RemoveWaterClientRpc(bucketCapacity);
    }

    [ClientRpc]
    public void RemoveWaterClientRpc(float bucketCapacity)
    {
        RemoveWater(bucketCapacity);
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

    public bool CheckLossCondition()
    {
        if (currentWaterLevel >= maxWaterLevel)
        { return true; }
        else
        { return false; }
    }
}