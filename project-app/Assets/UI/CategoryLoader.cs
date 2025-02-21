/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 18/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Clase que genera la vista de la interfaz de usuario de scratch
 */

using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class CategoryLoader : MonoBehaviour
{

    public GameObject contentPanel; // Referencia al panel donde se colocarán las categorías
    public string xmlFileName = "XML/Categories"; // Ruta en Resources
    public GameObject categoryPrefab; // Prefab de la categoría
    public UICanvasManager uiCanvasManager; // Referencia al gestor de la interfaz de usuario

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        this.LoadCategoriesFromXML();

    }

    public void LoadCategoriesFromXML()
    {
        if (contentPanel == null)
        {
            Debug.LogError("ContentPanel no asignado en CategoryLoader");
            return;
        }

        if (categoryPrefab == null)
        {
            Debug.LogError("Prefab de categoría no asignado en CategoryLoader");
            return;
        }
        //Elimino categorías previas para evitar duplicados
        foreach (Transform child in contentPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Cargo el archivo XML
        TextAsset xmlData = Resources.Load<TextAsset>(xmlFileName);
        if (xmlData == null)
        {
            Debug.LogError("No se pudo cargar el archivo XML: " + xmlFileName);
            return;
        }

        XDocument xmlDoc = XDocument.Parse(xmlData.text);
        IEnumerable<XElement> categories = xmlDoc.Element("Categories").Elements("Category");


        foreach (XElement category in categories)
        {
            string name = category.Element("Name").Value;
            string colorHex = category.Element("Color").Value;
            Color categoryColor = HexToColor(colorHex);
            Debug.Log("Categoría: " + name + " Color: " + categoryColor);
            CreateCategoryUI(name, categoryColor);

        }
    }

    void CreateCategoryUI(string name, Color color)
    {
        GameObject newCategory = Instantiate(categoryPrefab, contentPanel.transform);

        // Cambiar el nombre del objeto instanciado al nombre de la categoría
        newCategory.name = name;

        // Asegurar que el objeto está activo
        newCategory.SetActive(true);

        // Ajustar escala y posición
        RectTransform rect = newCategory.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -rect.sizeDelta.y * contentPanel.transform.childCount);
            rect.sizeDelta = new Vector2(50, 50);  // Tamaño de cada categoria
        }

        else
        {
            Debug.LogError($"No se encontró el componente RectTransform en el prefab {categoryPrefab.name}");
            //
            // rect.localScale = Vector3.one;
            //rect.anchoredPosition3D = Vector3.zero;
            //rect.localRotation = Quaternion.identity;
            //rect.sizeDelta = new Vector2(200, 50);  // Ajusta el tamaño si es necesario
        }

       
        LayoutElement layoutElement = newCategory.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = newCategory.AddComponent<LayoutElement>();
            //Debug.LogError($"No se encontró el componente LayoutElement en el prefab {categoryPrefab.name} se ha añadido" );
        }

        layoutElement.minHeight = 50;
        layoutElement.minWidth = 50;
        layoutElement.preferredHeight = 55;
        layoutElement.preferredWidth = 100;

        //Configurar la opacidad y visibilidad
        Canvas prefabCanvas = newCategory.GetComponentInChildren<Canvas>();
        if (prefabCanvas != null)
        {

            prefabCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            prefabCanvas.sortingOrder = 500;
        }

        // Configurar la opacidad y visibilidad
        CanvasGroup canvasGroup = newCategory.GetComponent<CanvasGroup>();
        if(canvasGroup == null)
        {
            canvasGroup = newCategory.AddComponent<CanvasGroup>();
        }
        if (canvasGroup != null)
       {
            canvasGroup.alpha = 1f;
        }
        
        // Verificar el RectTransform después de crearlo
        Debug.Log($"Categoría creada: {name} con tamaño {rect.sizeDelta} y posición {rect.anchoredPosition}");

        Transform iconTransform = newCategory.transform.Find("CategoryIcon");

        if (iconTransform != null)
        {
            Image icon = iconTransform.GetComponent<Image>();

            if (icon != null)
            {
                icon.color = color;
            }
            else
            {
                Debug.LogError($"No se encontró el componente Image en CategoryIcon del prefab {categoryPrefab.name}");
            }

            UnityEngine.UI.Button iconButton = icon.AddComponent<UnityEngine.UI.Button>();
            iconButton.onClick.AddListener(() => OnCategoryButtonClick(name, color));
        }
        // Configurar el texto
        //TextMeshProUGUI text = newCategory.transform.Find("CategoryText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI text = newCategory.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = name;
        }
        else
        {
            Debug.LogError($"No se encontró el componente TextMeshProUGUI en CategoryText del prefab {categoryPrefab.name}");
        }

        newCategory.SetActive(true);
        newCategory.GetComponent<CanvasGroup>().alpha = 1f;
        newCategory.transform.localScale = Vector3.one;

        Debug.Log($"Categoría creada: {name} con tamaño {rect.sizeDelta} y posición {rect.anchoredPosition}");
    }

    Color HexToColor(string hex)
    {
        Color color;
        if (UnityEngine.ColorUtility.TryParseHtmlString(hex, out color))
        {
            return color;
        }
        return Color.black;
    }

    // Función que se ejecuta cuando un botón de categoría es presionado
    void OnCategoryButtonClick(string categoryName, Color categoryColor)
    {
        Debug.Log("Se presionó la categoría: " + categoryName);
        if (uiCanvasManager != null)
        {
            uiCanvasManager.UpdateMiddlePanel(categoryName, categoryColor);
        }
        else
        {
            Debug.LogError("uiCanvasManager es nulo. Asegúrate de que se ha asignado correctamente.");
        }
    }

}
