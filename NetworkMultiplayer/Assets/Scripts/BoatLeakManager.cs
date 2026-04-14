using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class BoatLeakManager : NetworkBehaviour
{
    [Header("Water Settings")]
    public float currentWaterLevel = 0f;
    public float maxWaterLevel = 100f;
    public float leakRate = 0.01f;
    public int activeLeaks = 0;
    public GameObject waterPlane;

    public GameObject leakPrefab;
    public float leakInterval = 15f;



    // Update is called once per frame
    void Update()
    {
        if (activeLeaks > 0)
        {
            if (waterPlane.transform.position.y < maxWaterLevel)
            {
                
                waterPlane.transform.Translate(Vector3.up * leakRate * activeLeaks * Time.deltaTime);
            } 
        }
    }

    // used when a leak is spawned
    public void AddLeak()
    { activeLeaks++; }

    public void RepairLeak() 
    { activeLeaks = Mathf.Max(0, activeLeaks - 1); }

    IEnumerator SpawnLeaks()
    {
        while (true)
        {
            Instantiate(leakPrefab, transform.position, Quaternion.identity);
            float time = Random.Range(leakInterval, leakInterval + 5);

            yield return new WaitForSeconds(time);
        }
    }


}
