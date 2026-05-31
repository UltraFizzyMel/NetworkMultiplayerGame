using UnityEngine;

public class SteeringWheel : Interactable
{
    [SerializeField] private float steeringStrength = 1f;

    private Player currentPlayer;

    public Transform steeringPosition;

    private void Update()
    {
        if (currentPlayer == null)
            return;

        if (!currentPlayer.IsSteering())
        {
            currentPlayer = null;
        }
    }

    public override void Interact(Player player)
    {
        if (currentPlayer != null)
            return;

        currentPlayer = player;

        player.EnterSteering(this);
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
}