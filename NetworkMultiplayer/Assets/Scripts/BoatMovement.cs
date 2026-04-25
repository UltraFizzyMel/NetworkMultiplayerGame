using System;
using Unity.Netcode;
using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    public event EventHandler<OnBoatMovedEventArgs> OnBoatMoved;
    public class OnBoatMovedEventArgs : EventArgs
    {
        public float progressNormalized;
    }

     public float boatProgressMax = 5f;
    //[SerializeField] private float boatProgress = 0f;
    [SerializeField] private float boatSpeed = 0.5f;
   // private bool isMoving;
    [SerializeField] private GameObject PlayerUI;
    [SerializeField ] private Generator generator;

    public NetworkVariable<float> boatProgress = new(
       0f,
       NetworkVariableReadPermission.Everyone,
       NetworkVariableWritePermission.Server
   );

    public void Update()
    {
        float progress = 0;
        if (generator.FuelCheck()) { progress = boatSpeed; }
        //else { { progress = 0; } }
        boatProgress.Value += progress * Time.deltaTime;
        OnBoatMoved?.Invoke(this, new OnBoatMovedEventArgs { progressNormalized = boatProgress.Value / boatProgressMax });

    }

    
}
