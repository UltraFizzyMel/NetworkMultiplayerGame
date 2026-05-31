using UnityEngine;

public class RockObstacle : MonoBehaviour
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

    [Header("Collision")]
    public bool destroyOnHit = true;
}