using System.Collections;
using System.Security.Cryptography;
using Unity.Netcode;
using UnityEditor.Overlays;
using UnityEngine;

public class RockSetter : NetworkBehaviour
{
    [SerializeField] GameObject[] rocks;
    

    [SerializeField] private int rocksActive; 
    [SerializeField] private int rockNo;
    [SerializeField] private float waitTime;
    [SerializeField] RockSetter previousSetter;
    
    


    public override void OnNetworkSpawn()
    {
        StartCoroutine(SelectActiveRocks());
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
        }
        else
        {
            rockNo = Random.Range(1, 4);
            rocks[rockNo-1].SetActive(true);
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
        }
    }
}
