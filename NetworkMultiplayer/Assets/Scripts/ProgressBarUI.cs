using UnityEngine;
using UnityEngine.UI;

public class ProgressBarUI : MonoBehaviour
{
    [SerializeField] private Leak leak;
    [SerializeField] private Image barImage;

    private void Start()
    {
        leak.OnProgressChanged += Leak_OnProgressChanged;
        barImage.fillAmount = 0f;
    }

    private void Leak_OnProgressChanged(object sender, Leak.OnProgressChangedEventArgs e) { 
    barImage.fillAmount = e.progressNormalized;
    }

}
