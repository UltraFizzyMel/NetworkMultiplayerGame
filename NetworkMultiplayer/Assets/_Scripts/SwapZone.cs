using UnityEngine;

public class SwapZone : MonoBehaviour
{
    private BoxCollider[] _colliders;

    private void Awake()
    {
        // Collects every BoxCollider on this GameObject and all children.
        _colliders = GetComponentsInChildren<BoxCollider>();

        if (_colliders.Length == 0)
            Debug.LogWarning($"[SwapZone] No BoxColliders found under {gameObject.name}");
    }

    // Returns the position unchanged if it is inside any collider in the zone.
    // Returns the nearest valid point across all colliders if it is outside.
    public Vector3 GetSafePosition(Vector3 position)
    {
        // If position is already inside any of the zone's colliders, it's valid.
        // Return it as-is so the swap feels natural.
        foreach (BoxCollider col in _colliders)
        {
            if (IsInsideCollider(position, col))
                return position;
        }

        // Position is outside the zone (e.g. cabin player was against the divider
        // and technically in deck space). Find the nearest point across ALL
        // colliders in the zone — not just the nearest collider's surface,
        // but the nearest point inside any of them.
        float minDist = float.MaxValue;
        Vector3 best = position;

        foreach (BoxCollider col in _colliders)
        {
            // ClosestPoint returns a point ON the surface of the collider when
            // position is outside it, or the position itself when inside.
            Vector3 candidate = col.ClosestPoint(position);
            float dist = Vector3.SqrMagnitude(candidate - position);

            if (dist < minDist)
            {
                minDist = dist;
                best = candidate;
            }
        }

        // Small upward offset so PhysicsSafeSpawn's raycast has clearance
        // to settle the player onto the surface cleanly.
        best += Vector3.up * 0.1f;

        Debug.Log($"[SwapZone] Destination clamped into '{gameObject.name}'");
        return best;
    }

    // BoxCollider.bounds is axis-aligned in world space — fast and sufficient
    // for checking containment without needing a physics query.
    private static bool IsInsideCollider(Vector3 point, BoxCollider col)
    {
        return col.bounds.Contains(point);
    }

#if UNITY_EDITOR
    // Draws each child collider in the Scene view so you can see zone coverage.
    private void OnDrawGizmosSelected()
    {
        if (_colliders == null)
            _colliders = GetComponentsInChildren<BoxCollider>();

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
        foreach (BoxCollider col in _colliders)
        {
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = col.transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.matrix = prev;
        }
    }
#endif
}