using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class GeneratorProgressBar : MonoBehaviour
{
    [SerializeField] private Generator generator;
    [SerializeField] private Image barImage;

    private void OnEnable()
    {
        if (generator != null)
           generator.fuelingProgress.OnValueChanged += Generator_OnFuelChanged;
        barImage.fillAmount = 0f;
    }

    private void OnDisable()
    {
        if (generator != null)
            generator.fuelingProgress.OnValueChanged -= Generator_OnFuelChanged;
    }

    private void Generator_OnFuelChanged(float previousValue, float newValue)
    {
        //barImage.fillAmount = e.fuelNormalized;// The fill amount is equal to the normalized fuel value, It has to be normalized as the fill amount is a float from 0f-1f
        barImage.fillAmount = newValue/ generator.fuelMax;
        /*if (e.fuelNormalized <= 0f || e.fuelNormalized == 1f)
        {
            //Hide();
        }
        else
        {
            Show();
        }*/

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
