using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class WaterLevelUI : NetworkBehaviour
{
    [SerializeField] private BoatLeakManager boatLeakManager;
    [SerializeField] private Image barImage;

    private void OnEnable()
    {
        if (boatLeakManager != null)
            boatLeakManager.currentWaterLevel.OnValueChanged += Water_OnLevelChanged;
        barImage.fillAmount = 0f;
    }

    private void OnDisable()
    {
        if (boatLeakManager != null)
            boatLeakManager.currentWaterLevel.OnValueChanged -= Water_OnLevelChanged;
    }

    private void Water_OnLevelChanged(float previousValue, float newValue)
    {
        
        barImage.fillAmount = newValue / boatLeakManager.maxWaterLevel;

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