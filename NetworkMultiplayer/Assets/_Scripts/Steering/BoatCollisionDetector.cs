using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BoatCollisionDetector : NetworkBehaviour
{
    [SerializeField] private BoatLeakManager deckLeaks;
    [SerializeField] private BoatLeakManager cabinLeaks;

    private bool collisionCooldown;
    [SerializeField] private float collisionCooldownTime = 3f;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        if (collisionCooldown)
            return;

        if (!other.CompareTag("Obstacle"))
            return;

        RockObstacle rock =
            other.GetComponent<RockObstacle>();

        if (rock == null)
            return;

        collisionCooldown = true;

        Debug.Log(
            $"[Boat] Rock collision! +" +
            $"{rock.instantLeakAmount} leaks"
        );

        if (deckLeaks != null)
        {
            deckLeaks.SpawnImmediateLeaks(rock.instantLeakAmount);
        }

        if (cabinLeaks != null)
        {
            cabinLeaks.SpawnImmediateLeaks(rock.instantLeakAmount);
        }

        //Damage Systems
        BoatMovement.Instance.ApplyPermanentSlow(rock.speedDamage);

        BoatSteeringManager.Instance.ApplySteeringKnockback(rock.steeringKnockback);

        TriggerCameraShakeClientRpc(rock.cameraShakeStrength, rock.cameraShakeDuration);

        /*if (MusicManager.Instance != null)
            MusicManager.Instance.PlaySFX(SFXType.BoatCrash);*/

        if (rock.destroyOnHit)
        {
            NetworkObject no =
                rock.GetComponent<NetworkObject>();

            if (no != null && no.IsSpawned)
                no.Despawn();

            else
                Destroy(rock.gameObject);
        }

        StartCoroutine(CollisionCooldownRoutine());
    }

    private IEnumerator CollisionCooldownRoutine()
    {
        yield return new WaitForSeconds(collisionCooldownTime);

        collisionCooldown = false;
    }

    [ClientRpc]
    private void TriggerCameraShakeClientRpc(
    float intensity,
    float duration
)
    {
        foreach (Player player in PlayerRegistry.Players)
        {
            if (!player.IsOwner)
                continue;

            player.StartCameraShake(
                intensity,
                duration
            );
        }
    }
}