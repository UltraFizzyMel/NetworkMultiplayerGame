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
            GeneratorRpc();
    }

    private void OnDisable()
    {
        if (generator != null)
            generator.OnFuelChanged -= Generator_OnFuelChanged;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void GeneratorRpc()
    {
        generator.OnFuelChanged += Generator_OnFuelChanged;
        barImage.fillAmount = 0f;
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
