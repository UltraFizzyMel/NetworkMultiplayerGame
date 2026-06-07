using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RockObstacle : NetworkBehaviour
{
    [Header("Leaks")]
    public int instantLeakAmount = 2;

    [Header("Movement Damage")]
    public float speedDamage = 0.1f;

    [Header("Steering")]
    public float steeringKnockback = 3f;

    [Header("FX")]
    public float cameraShakeStrength = 1f;
    public float cameraShakeDuration = 0.5f;

    public bool wasHit = false;

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDestroyServerRpc()
    {
        RequestDestroyClientRpc();

        //NetworkObject no = GetComponent<NetworkObject>();

        /*if (no != null && no.IsSpawned)
            no.Despawn(true);   // true = destroy the GameObject, not just unregister it
        else
            Destroy(gameObject);*/
    }

    [ClientRpc]
    public void RequestDestroyClientRpc()
    {
        Destroy(gameObject);
        //DestroySelf();
    }

    public void DestroySelf()
    {
        NetworkObject no = gameObject.GetComponent<NetworkObject>();

        if (no != null && no.IsSpawned)
            no.Despawn();
        else
            Destroy(gameObject);
    }

    public void EndSceneCheck()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "WonGame" || currentScene == "LostGame")
        {
            Destroy(this.gameObject);
        }
    }
}