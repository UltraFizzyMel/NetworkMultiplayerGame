using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class GeneratorProgressBar : MonoBehaviour
{
    //[SerializeField] private Leak leak;
    [SerializeField] private Image barImage;

    private void Start()
    {
        leak.OnProgressChanged += Leak_OnProgressChanged;
        barImage.fillAmount = 1f;
    }

    private void Leak_OnProgressChanged(object sender, Leak.OnProgressChangedEventArgs e)
    {
        barImage.fillAmount = e.progressNormalized;

        if (e.progressNormalized <= 0f || e.progressNormalized == 1f)
        {
            Hide();
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
