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
 * Versión: 1.0.2
 * 
 * Descripción: : Carga la definición de las categorías desde un XML y crea los elementos UI correspondientes en el panel izquierdo.
 */

using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class CategoryLoader : MonoBehaviour
{

    public GameObject contentPanel; // Panel donde se colocarán las categorías
    public string xmlFileName = "XML/Categories"; 
    public GameObject categoryPrefab; 
    public UICanvasView uiCanvasManageViewr; 

    void Start()
    {
        this.LoadCategoriesFromXML();
    }

    /**
     * Descripción: Carga las categorías desde un archivo XML y crea los elementos UI correspondientes.
     */
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

        //Eliminación de categorías previas para evitar duplicados
        foreach (Transform child in contentPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Carga del archivo XML
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
            // Debug.Log("Categoría: " + name + " Color: " + categoryColor);
            CreateCategoryUI(name, categoryColor);

        }
    }

    /**
     * Descripción: Crea un nuevo objeto de categoría en la UI.
     * @param name Nombre de la categoría.
     * @param color Color de la categoría.
     */
    void CreateCategoryUI(string name, Color color)
    {
        GameObject newCategory = Instantiate(categoryPrefab, contentPanel.transform);
        newCategory.name = name;
        newCategory.SetActive(true);
        RectTransform rect = newCategory.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect= newCategory.AddComponent<RectTransform>();
            //Debug.LogError($"No se encontró el componente RectTransform en el prefab {categoryPrefab.name}");
             rect.anchorMin = new Vector2(0.5f, 1);
             rect.anchorMax = new Vector2(0.5f, 1);
             rect.pivot = new Vector2(0.5f, 1);
             rect.anchoredPosition = new Vector2(0, -rect.sizeDelta.y * contentPanel.transform.childCount);
             rect.sizeDelta = new Vector2(50,50);  // Tamaño de la categoria
        }

        LayoutElement layoutElement = newCategory.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = newCategory.AddComponent<LayoutElement>();
            //Debug.LogError($"No se encontró el componente LayoutElement en el prefab {categoryPrefab.name} se ha añadido" );
        }

        layoutElement.minHeight = 25;
        layoutElement.minWidth = 25;
        layoutElement.preferredHeight = 25;
        layoutElement.preferredWidth = 25;

        Canvas prefabCanvas = newCategory.GetComponentInChildren<Canvas>();
        if (prefabCanvas != null)
        {
            prefabCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            prefabCanvas.sortingOrder = 500;
        }

        // Configuración de la opacidad y visibilidad
        CanvasGroup canvasGroup = newCategory.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = newCategory.AddComponent<CanvasGroup>();
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

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

            Button iconButton = icon.AddComponent<Button>();
            iconButton.onClick.AddListener(() => OnCategoryButtonClick(name, color));
        }

        TextMeshProUGUI text = newCategory.transform.Find("CategoryText").GetComponent<TextMeshProUGUI>();
      
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

    /**
     * Descripción: Convierte un valor hexadecimal a un color de Unity.
     * @param hex Valor hexadecimal del color.
     * @return Color correspondiente al valor hexadecimal.
     */
    Color HexToColor(string hex)
    {
        Color color;
        if (UnityEngine.ColorUtility.TryParseHtmlString(hex, out color))
        {
            return color;
        }
        return Color.black;
    }

    /**
     * Descripción: Método que se llama al hacer clic en un botón de categoría.
     * @param categoryName Nombre de la categoría.
     * @param categoryColor Color de la categoría.
     */
    void OnCategoryButtonClick(string categoryName, Color categoryColor)
    {
        //Debug.Log("Se presionó la categoría: " + categoryName);
        if (uiCanvasManageViewr != null)
        {
            uiCanvasManageViewr.UpdateMiddlePanel(categoryName, categoryColor);
        }
        else
        {
            Debug.LogError("uiCanvasManager es nulo. Revisar que se ha asignado correctamente.");
        }
    }

}