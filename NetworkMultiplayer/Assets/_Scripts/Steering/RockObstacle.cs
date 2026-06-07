using System.Data;
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

    public NetworkVariable<bool> IsEnabled = new(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    public bool stateChange = true;

    public override void OnNetworkSpawn()
    {
        IsEnabled.OnValueChanged += OnEnabledChanged;
        ApplyState(IsEnabled.Value);
    }

    public override void OnNetworkDespawn()
    {
        IsEnabled.OnValueChanged -= OnEnabledChanged;
    }

    private void OnEnabledChanged(bool oldValue, bool newValue)
    {
        ApplyState(newValue);
    }

    private void ApplyState(bool enabled)
    {
        if (!stateChange)
            return;

        GetComponent<Collider>().enabled = enabled;
        GetComponent<MeshRenderer>().enabled = enabled;
    }


    public void DestroyRock()
    {
        if (!IsServer)
            return;

        NetworkObject.Despawn(true);
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