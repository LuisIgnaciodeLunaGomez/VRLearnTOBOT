/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 10/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:  Manejo de los valores dentro de los bloques
 * 
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputView : BaseView
{
    [SerializeField] private TMP_InputField m_inputField; // Campo de entrada de usuario
    [SerializeField] private Image m_backGroundImage; // Imagen de fondo 9-Slice
    [SerializeField] private Sprite m_DefaultBackgroundSprite;

    public override ViewType Type => ViewType.Input;

    public void Awake()
    {
        if (this.m_inputField == null)
        {
            this.m_inputField = GetComponent<TMP_InputField>();
            if (this.m_inputField == null)
            {
                this.m_inputField = gameObject.AddComponent<TMP_InputField>();
                Debug.Log($"InputView.Awake: Añadido TMP_InputField a {gameObject.name}");
            }
            else
            {
                Debug.Log($"InputView.Awake: Usando TMP_InputField existente en {gameObject.name}");
            }
        }

        if (this.m_backGroundImage == null)
        {
            this.m_backGroundImage = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            Debug.Log($"InputView.Awake: Añadida Image a {gameObject.name}");
        }
        else
        {
            Debug.Log($"InputView.Awake: Usando Image existente en {gameObject.name}");
        }
        this.SetupInputField();
        this.SetUpBackgroundImage();
        Debug.Log($"InputView.Awake: Configuración completada para {gameObject.name}");
    }

    public void SetupInputField()
    {
        Debug.Log($"InputView.SetupInputField: Configurando InputField en {gameObject.name}");
        // Crear el Text Area y Viewport si no existen
        if (this.m_inputField.textViewport == null)
        {
            GameObject viewport = new GameObject("Text Area", typeof(RectTransform));
            viewport.transform.SetParent(transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0.5f, 0.5f);
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;

            Debug.Log($"InputView.SetupInputField: Creado Text Area con sizeDelta {viewportRect.sizeDelta}");

            // Añadir TextMeshProUGUI para el texto
            GameObject textObject = new GameObject("Text", typeof(TextMeshProUGUI));
            textObject.transform.SetParent(viewport.transform, false);
            TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
            textComponent.fontSize = 24;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;

            RectTransform textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            Debug.Log($"InputView.SetupInputField: Configurado TextComponent RectTransform");
            this.m_inputField.textViewport = viewportRect;
            this.m_inputField.textComponent = textComponent;
        }
        else
        {
            Debug.Log($"InputView.SetupInputField: Usando TextViewport existente con sizeDelta {this.m_inputField.textViewport.sizeDelta}");
        }

        // Configurar el placeholder

        if (this.m_inputField.placeholder == null)
        {
            GameObject placeholderObj = new GameObject("Placeholder", typeof(TextMeshProUGUI));
            placeholderObj.transform.SetParent(this.m_inputField.textViewport, false);
            TextMeshProUGUI placeholderText = placeholderObj.GetComponent<TextMeshProUGUI>();
            placeholderText.text = "10";
            placeholderText.fontSize = 24;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            
            RectTransform placeholderRect = placeholderText.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.anchoredPosition = Vector2.zero;
            Debug.Log($"InputView.SetupInputField: Configurado Placeholder RectTransform");

            this.m_inputField.placeholder = placeholderText;
            Debug.Log($"InputView.SetupInputField: Añadido Placeholder con texto '10'");
        }

        else
        {
            Debug.Log($"InputView.SetupInputField: Usando Placeholder existente con texto '{((TextMeshProUGUI)this.m_inputField.placeholder).text}'");
        }

        this.m_inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        if (string.IsNullOrEmpty(this.m_inputField.text)) this.m_inputField.text = "10";
        Debug.Log($"InputView.SetupInputField: Configurado texto inicial '{this.m_inputField.text}'");

    }

    private void SetUpBackgroundImage()
    {
        Debug.Log($"InputView.SetupBackgroundImage: Configurando fondo para {gameObject.name}");

        if (m_DefaultBackgroundSprite == null)
        {
            m_DefaultBackgroundSprite = Resources.Load<Sprite>("Icons/Input_Field");
            if (m_DefaultBackgroundSprite == null)
            {
                Debug.LogError("No se encontró el sprite 'InputField' en Resources.");
                return;
            }
            else
            {
                Debug.Log($"InputView.SetupBackgroundImage: Cargado sprite 'InputField_0' con tamaño {m_DefaultBackgroundSprite.rect}");
            }
        }
        this.m_backGroundImage.sprite = m_DefaultBackgroundSprite;
        this.m_backGroundImage.type = Image.Type.Sliced;
        this.m_backGroundImage.preserveAspect = false;

        RectTransform imageRect = this.m_backGroundImage.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.sizeDelta = Vector2.zero;
        Debug.Log($"InputView.SetupBackgroundImage: Configurada imagen con sizeDelta {imageRect.sizeDelta}");
    }

    public void SetBackgroundSprite(Sprite sprite)
    {
        if (this.m_backGroundImage != null)
        {

            this.m_DefaultBackgroundSprite = sprite;
            this.m_backGroundImage.sprite = sprite;
            Debug.Log($"InputView.SetBackgroundSprite: Cambiado sprite a {sprite.name}");
        }
    }

    protected override Vector2 CalculateSize()
    {
        Debug.Log($"InputView.CalculateSize: Calculando tamaño para {gameObject.name}");
        if (this.m_inputField == null || this.m_inputField.textComponent == null)
        {
            SetupInputField();
            Debug.Log($"InputView.CalculateSize: Reconfigurado InputField y TextComponent");
        }
        

        //Tamaño preferido del texto
        Vector2 textSize = this.m_inputField.textComponent.GetPreferredValues();
        Vector2 minSize = new Vector2(50f, 50f); // Tamaño mínimo
        Debug.Log($"InputView.CalculateSize: TextSize = {textSize}");

        // Tamaño mínimo de la imagen de fondo (si tiene un tamaño intrínseco)
        Vector2 imageMinSize = this.m_backGroundImage.sprite != null
            ? new Vector2(this.m_backGroundImage.sprite.rect.width, this.m_backGroundImage.sprite.rect.height)
            : minSize;
        Debug.Log($"InputView.CalculateSize: ImageMinSize = {imageMinSize}");


        // Calcular el tamaño combinado
        Vector2 size = new Vector2(
            Mathf.Max(textSize.x, imageMinSize.x, minSize.x),
            Mathf.Max(textSize.y, imageMinSize.y, minSize.y)
        );// Actualizar el LayoutElement

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;
            Debug.Log($"InputView.CalculateSize: Actualizado LayoutElement a {size}");
        }
        else
        {
            Debug.LogWarning($"InputView.CalculateSize: No se encontró LayoutElement en {gameObject.name}");
        }

        Debug.Log($"InputView.CalculateSize: Tamaño calculado = {size}");    
            return size;
    }



    public ConnectionInputView GetConnectionView()
    {
        return Childs.Count > 0 ? Childs[Childs.Count - 1] as ConnectionInputView : null;
    }



}
