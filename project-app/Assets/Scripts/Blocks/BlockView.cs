using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BlockView : MonoBehaviour
{
    public string BlockType { get; private set; }
    public RectTransform RectTransform { get; private set; }
    public Image BlockImage { get; private set; }

    private TextMeshProUGUI textStart;
    private TMP_InputField inputField;
    private TextMeshProUGUI textEnd;

    public void Initialize(string type, string labelStart, string labelEnd, Sprite blockSprite, Color categoryColor)
    {
        BlockType = type;

        // Configurar el RectTransform
        RectTransform = gameObject.AddComponent<RectTransform>();
        RectTransform.sizeDelta = new Vector2(200, 60);
        RectTransform.anchorMin = new Vector2(0, 1);
        RectTransform.anchorMax = new Vector2(0, 1);
        RectTransform.pivot = new Vector2(0, 1);

        // Agregar imagen del bloque
        BlockImage = gameObject.AddComponent<Image>();
        BlockImage.sprite = blockSprite;
        BlockImage.type = Image.Type.Sliced;
        BlockImage.color = categoryColor;

        // Crear contenedor para los textos y valores
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0.5f);
        contentRect.anchorMax = new Vector2(1, 0.5f);
        contentRect.pivot = new Vector2(0, 0.5f);
        contentRect.sizeDelta = new Vector2(180, 50);

        // Agregar textos y entrada numérica
        textStart = CreateTextElement(labelStart, content.transform, new Vector2(10, 0));
        inputField = CreateInputField(content.transform, new Vector2(90, 0));
        textEnd = CreateTextElement(labelEnd, content.transform, new Vector2(150, 0));
    }

    private TextMeshProUGUI CreateTextElement(string text, Transform parent, Vector2 position)
    {
        GameObject textGO = new GameObject("Text", typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 24;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(60, 40);
        rect.anchoredPosition = position;

        return textComponent;
    }

    private TMP_InputField CreateInputField(Transform parent, Vector2 position)
    {
        GameObject inputGO = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputGO.transform.SetParent(parent, false);

        RectTransform rect = inputGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(40, 40);
        rect.anchoredPosition = position;

        Image bgImage = inputGO.GetComponent<Image>();
        bgImage.color = Color.white;

        TMP_InputField inputField = inputGO.GetComponent<TMP_InputField>();
        inputField.text = "10";
        inputField.textComponent.fontSize = 24;
        inputField.textComponent = CreateTextElement("10", inputGO.transform, Vector2.zero);

        return inputField;
    }
}
