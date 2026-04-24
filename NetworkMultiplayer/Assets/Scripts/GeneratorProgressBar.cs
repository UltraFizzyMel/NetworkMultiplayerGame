using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class GeneratorProgressBar : MonoBehaviour
{
    [SerializeField] private Generator generator;
    [SerializeField] private Image barImage;

    private void Start()
    {
        generator.OnFuelChanged += Generator_OnFuelChanged;
        barImage.fillAmount = 1f;
    }

    private void Generator_OnFuelChanged(object sender, Generator.OnFuelChangedEventArgs e)
    {
        barImage.fillAmount = e.fuelNormalized;

        if (e.fuelNormalized <= 0f || e.fuelNormalized == 1f)
        {
            //Hide();
        }
        else
        {
            Show();
        }

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
