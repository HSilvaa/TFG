using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HackerText : MonoBehaviour, IPointerEnterHandler
{
    private TextMeshProUGUI _text;
    private string _defaultText;
    public float speed = 0.02f;

    void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _defaultText = _text.text;
    }

    private void OnEnable()
    {
        _text.text = _defaultText;
    }

    private IEnumerator StartAnimation()
    {
        int letterIndex = 0;

        // Mostrar texto inicial completamente aleatorio pero con espacio fijo si lo hay
        _text.text = GenerateInitialRandomText(_defaultText);

        yield return new WaitForSeconds(speed * 2f); // pequeño retardo visual

        while (letterIndex <= _defaultText.Length)
        {
            int randomCount = 0;

            while (randomCount < 5)
            {
                _text.text = RandomizeText(_defaultText, letterIndex);
                yield return new WaitForSeconds(speed);
                randomCount++;
            }

            letterIndex++;
        }
    }

    public float GetTotalTime()
    {
        return speed * _defaultText.Length * 5;
    }

    private string GenerateInitialRandomText(string text)
    {
        string randomCharacters = "ABCDEFGHJKLNOPQRSTUVXYZ234567890";
        StringBuilder sb = new StringBuilder();

        foreach (char c in text)
        {
            if (c == ' ')
                sb.Append(' ');
            else
                sb.Append(randomCharacters[Random.Range(0, randomCharacters.Length)]);
        }

        return sb.ToString();
    }

    private string RandomizeText(string text, int startIndex)
    {
        string randomCharacters = "ABCDEFGHJKLNOPQRSTUVXYZ234567890";
        StringBuilder sb = new StringBuilder(text);

        for (int i = startIndex; i < text.Length; i++)
        {
            if (text[i] == ' ')
                continue;

            sb[i] = randomCharacters[Random.Range(0, randomCharacters.Length)];
        }

        return sb.ToString();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartCoroutine(StartAnimation());
    }
}
