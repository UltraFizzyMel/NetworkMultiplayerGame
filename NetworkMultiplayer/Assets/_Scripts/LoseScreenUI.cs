using UnityEngine;

public class LoseScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject waterText;
    [SerializeField] private GameObject fogText;

    private void Start()
    {
        LossType lossType = SessionData.Instance.currentLossType;

        switch (lossType)
        {
            case LossType.Sank:
                fogText.SetActive(false);
                waterText.SetActive(true);
                break;

            case LossType.Fog:
                waterText.SetActive(false);
                fogText.SetActive(true);
                break;

            default:
                waterText.SetActive(false);
                fogText.SetActive(false);
                break;
        }
    }
}
