using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RockSetter : NetworkBehaviour
{
    [SerializeField] GameObject[] rocks;

    [SerializeField] private int rocksActive; 
    [SerializeField] private int rockNo;
    [SerializeField] private float waitTime;
    [SerializeField] RockSetter previousSetter;
    [SerializeField] private GameObject[] activeRocks;

    public override void OnNetworkSpawn()
    {
        if(IsServer)
        { StartCoroutine(SelectActiveRocks()); } 
       
        
    }

 

    public IEnumerator SelectActiveRocks()
    {
        yield return new WaitForSeconds(waitTime);

        // if there are previous rocks set one of the new rocks based on the previous set rocks
        if (previousSetter != null)
        {
            int rockOne = 0;
            while (rockOne == 0 || rockOne == previousSetter.rockNo)
            {
                
                rockOne = Random.Range(1, 4);
                rockNo = rockOne;
                yield return null;
            }
            rocks[rockOne-1].SetActive(true);
            RequestSpawnClientRpc(rockOne - 1);
        }
        else
        {
            rockNo = Random.Range(1, 4);
            rocks[0].SetActive(true);
            RequestSpawnClientRpc(0);
        }

        
        if (rocksActive > 1)
        {
           int rockTwo = 0;
            while (rockTwo == 0 || rockTwo == rockNo || rockTwo == previousSetter.rockNo)
            {
                rockTwo = Random.Range(1, 4);
                yield return null;
            }
            rocks[rockTwo-1].SetActive(true);
            RequestSpawnClientRpc(rockTwo - 1);
        }
    }

    [ClientRpc]
    public void RequestSpawnClientRpc(int index)
    {
       if(!IsServer)
        { rocks[index].SetActive(true); }
    }
}
