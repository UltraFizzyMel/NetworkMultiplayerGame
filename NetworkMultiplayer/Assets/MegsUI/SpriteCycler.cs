using UnityEngine;
using UnityEngine.UI;

public class SpriteCycler : MonoBehaviour
{
   public Texture[] images;
    public float cycleTime = 1f;

    private RawImage rawImage;
    private int index = 0;

    void Start()
    {
        rawImage = GetComponent<RawImage>();

        if (images != null && images.Length > 0)
        {
            rawImage.texture = images[0];
            InvokeRepeating(nameof(NextImage), cycleTime, cycleTime);
        }
    }

    void NextImage()
    {
        if (images.Length == 0) return;

        index = (index + 1) % images.Length;
        rawImage.texture = images[index];
    }
}
