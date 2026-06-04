using UnityEngine;

public class SteeringWheel : Interactable
{
    public Transform steeringPosition;

    [Header("Wheel Visual")]
    // Assign the wheel mesh Transform (the part that actually spins),
    // not the parent SteeringWheel GameObject.
    [SerializeField] private Transform wheelVisual;
    [SerializeField] private float maxWheelRotation = 720f;  // Degrees at full lock
    [SerializeField] private float wheelRotationSpeed = 8f;
    // When true the wheel slowly returns to centre while nobody is steering.
    // When false it holds its last position — set in Inspector to taste.
    [SerializeField] private bool returnToNeutralWhenIdle = false;

    private Player _currentPlayer;
    private Quaternion _baseWheelRotation;

    private void Start()
    {
        if (wheelVisual != null)
            _baseWheelRotation = wheelVisual.localRotation;
    }

    private void Update()
    {
        // Clear the reference if the player stopped steering (e.g. swapped out)
        if (_currentPlayer != null && !_currentPlayer.IsSteering())
            _currentPlayer = null;

        RotateWheelVisual();
    }

    // ─── Interactions ────────────────────────────────────────────────────────
    public override void Interact(Player player)
    {
        if (_currentPlayer != null)
            return;

        if (player.IsDeckPlayer.Value)
        {
            Debug.Log("[SteeringWheel] Only the cabin player can steer.");
            return;
        }

        _currentPlayer = player;
        player.EnterSteering(this);
        FogZoneManager.Instance?.ReapplySteeringBlocks();
    }

    /*public override void Cancel(Player player)
    {
        if (currentPlayer != player)
            return;

        currentPlayer = null;

        player.ExitSteering();
    }*/

    public void HandleSteeringInput(float horizontal)
    {
        BoatSteeringManager.Instance.SetSteering(horizontal);
    }

    // ─── Wheel visual rotation ────────────────────────────────────────────────
    private void RotateWheelVisual()
    {
        if (wheelVisual == null) return;
        if (BoatSteeringManager.Instance == null) return;

        float steering = BoatSteeringManager.Instance.SteeringAmount.Value;

        // Decide the target Z angle:
        // +Z for right (positive steering), -Z for left (negative steering)
        float targetZ = 0f;

        if (_currentPlayer != null)
        {
            // Someone is actively steering — track the live steering amount
            targetZ = steering * maxWheelRotation;
        }
        else if (!returnToNeutralWhenIdle)
        {
            // Hold last position when nobody is steering
            targetZ = steering * maxWheelRotation;
        }
        // else targetZ stays 0 — wheel returns to neutral

        Quaternion targetRot = _baseWheelRotation * Quaternion.Euler(0f, 0f, targetZ);

        wheelVisual.localRotation = Quaternion.Lerp(
            wheelVisual.localRotation,
            targetRot,
            Time.deltaTime * wheelRotationSpeed);
    }
}