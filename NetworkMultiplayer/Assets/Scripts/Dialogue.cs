using TMPro;
using UnityEngine;

public class Dialogue : MonoBehaviour
{
    public TMP_Text textBox;
    [TextArea] public string[] lines;

    public float speed = 0.05f;

    int lineIndex = 0;
    int charIndex = 0;

    float timer;

    void Start()
    {
        textBox.text = lines[lineIndex];
        textBox.maxVisibleCharacters = 0;
    }

    void Update()
    {
        // Typing effect
        timer += Time.deltaTime;

        if (timer >= speed)
        {
            timer = 0f;

            if (charIndex < lines[lineIndex].Length)
            {
                charIndex++;
                textBox.maxVisibleCharacters = charIndex;
            }
        }

       
    }
}
