using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RockSetter : MonoBehaviour
{
    [SerializeField] GameObject[] rocks;

    [SerializeField] private int rocksActive; 
    [SerializeField] private int rockNo;
    [SerializeField] private float waitTime;
    [SerializeField] RockSetter previousSetter;
    [SerializeField] private GameObject[] activeRocks;

    /*public override void OnNetworkSpawn()
    {
        if(IsServer)
        { StartCoroutine(SelectActiveRocks()); }
    }*/

    private IEnumerator Start()
    {
        // Only host/server should choose rocks
        if (!Unity.Netcode.NetworkManager.Singleton.IsServer)
            yield break;

        yield return new WaitForSeconds(waitTime);

        StartCoroutine(SelectActiveRocks());
    }

    public IEnumerator SelectActiveRocks()
    {
        yield return new WaitForSeconds(waitTime);

        // if there are previous rocks set one of the new rocks based on the previous set rocks
        if (previousSetter != null)
        {
            int rockOne = -1;
            while (rockOne == -1 || rockOne == previousSetter.rockNo)
            {
                
                rockOne = Random.Range(0, rocks.Length);
                rockNo = rockOne;
                yield return null;
            }
            //rocks[rockOne-1].SetActive(true);
            EnableRock(rocks[rockOne]);
            //EnableRockClientRpc(rockOne);
            //RequestSpawnClientRpc(rockOne - 1);
        }
        else
        {
            rockNo = Random.Range(0, rocks.Length);
            //rocks[0].SetActive(true);
            EnableRock(rocks[0]);
            //EnableRockClientRpc(0);
            //RequestSpawnClientRpc(0);
        }

        
        if (rocksActive > 1)
        {
           int rockTwo = -1;
            while (rockTwo == -1 || rockTwo == rockNo || (previousSetter != null && rockTwo == previousSetter.rockNo))
            {
                rockTwo = Random.Range(0, rocks.Length);
                yield return null;
            }
            EnableRock(rocks[rockTwo]);
            //EnableRockClientRpc(rockTwo);
            //RequestSpawnClientRpc(rockTwo - 1);
        }
    }

    private void EnableRock(GameObject rock)
    {
        //rock.GetComponent<Collider>().enabled = true;
        //rock.GetComponent<MeshRenderer>().enabled = true;

        RockObstacle obstacle = rock.GetComponent<RockObstacle>();

        if (obstacle != null)
        {
            obstacle.IsEnabled.Value = true;
        }
    }

    [ClientRpc]
    private void EnableRockClientRpc(int index)
    {
        //if (IsServer) return;

        rocks[index].GetComponent<Collider>().enabled = true;
        rocks[index].GetComponent<MeshRenderer>().enabled = true;
    }

    /*[ClientRpc]
    public void RequestSpawnClientRpc(int index)
    {
       if(!IsServer)
        { rocks[index].SetActive(true); }
    }*/
}
