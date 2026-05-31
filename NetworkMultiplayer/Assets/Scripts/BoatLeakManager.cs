using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.VisualScripting;
using System.Collections.Generic;

public class BoatLeakManager : NetworkBehaviour
{
    [Header("Water Settings")]
    //public float currentWaterLevel = 0f;
    public float maxWaterLevel = 100f;
    public float minWaterLevel = 0f;
    public float leakRate = 0.01f;
    //public int activeLeaks = 0;
    public GameObject waterPlane;

    [Header("Leak Spawning")]
    public GameObject leakPrefab;
    public float leakInterval = 15f;
    //public bool bucketUsed;
    //public bool bucketRebound;
    public BucketController bucketController;
    //private Leak leak;

    [SerializeField] private List<LeakLocations> leakLocationsList;
    private LeakLocations _leakLocation;

    public NetworkVariable<float> currentWaterLevel = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> activeLeaks = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Start()
    {
        var bucket = GameObject.Find("TempBucket");
        if (bucket != null)
            bucketController = bucket.GetComponent<BucketController>();
    }

    public override void OnNetworkSpawn()
    {
        currentWaterLevel.OnValueChanged += OnWaterLevelChanged;
        UpdateWaterPlane(currentWaterLevel.Value);

        if (!IsServer) return;
        StartCoroutine(WaitForGameReady());
        //RequestLeakSpawnServerRpc();
    }

    public override void OnNetworkDespawn()
    {
        currentWaterLevel.OnValueChanged -= OnWaterLevelChanged;
    }

    private void OnWaterLevelChanged(float _, float newValue) => UpdateWaterPlane(newValue);
        
    // Update is called once per frame
    private void Update()
    {
        if (!IsServer || !IsSpawned) return;
        RisingWater();
    }

    // ─── Water logic (server only, synced via NetworkVariable) ───────────────
    private void RisingWater()
    {
        if (activeLeaks.Value <= 0) return;
        if (currentWaterLevel.Value >= maxWaterLevel) return;

        currentWaterLevel.Value = Mathf.Min(
            currentWaterLevel.Value + activeLeaks.Value * leakRate * Time.deltaTime,
            maxWaterLevel);
    }

    private void UpdateWaterPlane(float waterLevel)
    {
        if (waterPlane == null) return;
        waterPlane.transform.position = new Vector3(
            waterPlane.transform.position.x,
            waterLevel,
            waterPlane.transform.position.z);
    }

    // ─── Leak count ──────────────────────────────────────────────────────────
    // used when a leak is spawned
    public void AddLeak()
    {
        //activeLeaks++;
        if (IsServer) activeLeaks.Value++;
    }

    public void RepairLeak()
    {
        //activeLeaks = Mathf.Max(0, activeLeaks - 1); 
        if (IsServer) activeLeaks.Value = Mathf.Max(0, activeLeaks.Value - 1);
    }

    // ─── Leak spawning (server only) ─────────────────────────────────────────
    private IEnumerator WaitForGameReady()
    {
        yield return new WaitUntil(() => GameManager.Instance != null && GameManager.Instance.GameReady());

        StartCoroutine(SpawnLeaks());
    }

    private IEnumerator SpawnLeaks()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(leakInterval, leakInterval + 5f));
            SpawnSingleLeak();
        }
    }

    public void SpawnImmediateLeaks(int amount)
    {
        if (!IsServer) return;
        for (int i = 0; i < amount; i++) SpawnSingleLeak();
        Debug.Log($"[BoatLeakManager] Spawned {amount} collision leaks.");
    }

    private void SpawnSingleLeak()
    {
        GameObject leakInstance = Instantiate(
            leakPrefab, PickRandomSurface(), _leakLocation.SetLeakRotation());

        leakInstance.transform.RotateAround(
            _leakLocation.transform.position, Vector3.up, _leakLocation.rotationAdjustment);

        Leak leakScript = leakInstance.GetComponent<Leak>();
        leakInstance.GetComponent<NetworkObject>().Spawn();
        leakScript.boatLeakManager = this;
        AddLeak();
    }

    // ─── Bucket integration ──────────────────────────────────────────────────
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RemoveWaterServerRpc(float bucketCapacity)
    {
        currentWaterLevel.Value = Mathf.Max(
            minWaterLevel,
            currentWaterLevel.Value - bucketCapacity);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────
    private Vector3 PickRandomSurface()
    {
        if (leakLocationsList.Count > 0)
        {
            _leakLocation = leakLocationsList[Random.Range(0, leakLocationsList.Count)];
        }
        return _leakLocation.FindRandomLeakLocation();
    }

    // CheckLossCondition is now read from NetworkVariable so it's correct on every machine, not just the host.
    public bool CheckLossCondition() => currentWaterLevel.Value >= maxWaterLevel;

    //[ServerRpc]
    /*[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestLeakSpawnServerRpc()
    {
        //RequestLeakSpawnClientRpc();
        StartCoroutine(SpawnLeaks());
    }

    /*[ClientRpc]
    public void RequestLeakSpawnClientRpc()
    {
        StartCoroutine(SpawnLeaks());
    }/

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
    }*/
}