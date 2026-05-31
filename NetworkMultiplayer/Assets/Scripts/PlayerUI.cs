using UnityEngine;
using UnityEngine.UI;

public class PlayerUI: MonoBehaviour 
{
    [SerializeField] private BoatMovement boatMovement;
    [SerializeField] private Image barImage;

    private void OnEnable()
    {
        if (boatMovement != null)
            boatMovement.distanceToLighthouse.OnValueChanged += Boat_OnPositionChanged;
        //boatMovement.boatProgress.OnValueChanged += Boat_OnPositionChanged;
        barImage.fillAmount = 0f;
    }

    private void OnDisable()
    {
        if (boatMovement != null)
            boatMovement.distanceToLighthouse.OnValueChanged -= Boat_OnPositionChanged;
        //boatMovement.boatProgress.OnValueChanged -= Boat_OnPositionChanged;
    }

    private void Boat_OnPositionChanged(float previousValue, float newValue)
    {
        //barImage.fillAmount = e.fuelNormalized;// The fill amount is equal to the normalized fuel value, It has to be normalized as the fill amount is a float from 0f-1f
        //barImage.fillAmount = newValue / boatMovement.boatProgressMax;
        barImage.fillAmount = boatMovement.GetProgressNormalized();
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
