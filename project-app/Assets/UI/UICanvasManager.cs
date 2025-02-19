/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 17/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Clase que genera la vista de la interfaz de usuario de scratch
 */

using UnityEngine;
using UnityEngine.UI;

public class UICanvasManager : MonoBehaviour
{
    public string logo;
    public string[] iconNames;

   
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Busco el UIManager y lo asigno como padre
        GameObject uiManager = GameObject.Find("UIManager");
        if (uiManager == null)
        {
            uiManager = new GameObject("UIManager");
        }

        // Creo el Canvas principal que contendrá los paneles
        GameObject canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(uiManager.transform);
        UnityEngine.Canvas canvas = canvasGO.AddComponent<UnityEngine.Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1280, 720);
        canvasScaler.matchWidthOrHeight = 1f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Creo el panel superior
        GameObject topPanel = this.CreatePanel("TopPanel", canvasGO.transform, new Vector2(0, 0.95f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Color(0.6f, 0.4f, 1f, 1f));
        Canvas topCanvas = topPanel.AddComponent<Canvas>();
        topCanvas.overrideSorting = true;
        topCanvas.sortingOrder = 1000;

        //Agregar el logo a la izquierda del panel superior
        if (!string.IsNullOrEmpty(logo))
        {
            this.AddLogoToPanel(topPanel, logo);
        }

        // Agregar iconos a la derecha
        if (iconNames != null && iconNames.Length > 0)
        {
           this.AddIconsToPanel(topPanel, iconNames);
        }
        // Creo el panel izquierdo 
        GameObject leftPanel = this.SetupLeftPanel(canvasGO);
        //Cargar las categorias en el LeftPanel
        this.LoadCategories(leftPanel);

        // Creo el panel medio 
        GameObject middlePanel = this.CreatePanel("MiddlePanel", canvasGO.transform, new Vector2(0.15f, 0), new Vector2(0.5f, 0.95f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Color(0.976f, 0.976f, 0.976f, 1f));

        // Crear el panel derecho 
        GameObject rightPanel = this.CreatePanel("RightPanel", canvasGO.transform, new Vector2(0.5f, 0), new Vector2(1, 0.95f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Color(0.976f, 0.976f, 0.976f, 1f));

      
    }

    /**
     * Crea un panel con los parámetros indicados
     * @param name Nombre del panel
     * @param parent Transform del padre
     * @param anchorMin Posición mínima del ancla
     * @param anchorMax Posición máxima del ancla
     * @param offsetMin Offset mínimo
     * @param offsetMax Offset máximo
     * @param pivot Punto de pivote
     * @param color Color del panel
     * @return GameObject con el panel creado
     */
    GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Vector2 pivot, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.pivot = pivot;

        Image img = panel.AddComponent<Image>();
        img.color = color;

        return panel;
    }

    /**
     * Añade un borde al panel indicado
     * @param panel GameObject al que se le añadirá el borde
     * @param anchorMin Posición mínima del ancla
     * @param anchorMax Posición máxima del ancla
     */
    void AddBorder(GameObject panel, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject border = new GameObject("Border");
        border.transform.SetParent(panel.transform);
        RectTransform rect = border.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = new Vector2(2, 0);

        Image img = border.AddComponent<Image>();
        img.color = Color.gray;
    }

    void AddLogoToPanel(GameObject panel, string spriteName)
    {
        Debug.Log("Cargando sprite desde Resources: " + spriteName);
        //Sprite sprite = Resources.Load<Sprite>(spriteName);
        Texture2D logoTexture = Resources.Load<Texture2D>("Icons/" + spriteName);

        if (logoTexture != null)
        {
            Debug.Log("Texture2D encontrada, creando objeto Image en TopPanel.");
            GameObject logo = new GameObject("TopPanelLogo");
            logo.transform.SetParent(panel.transform);
            RectTransform logoRect = logo.AddComponent<RectTransform>();
            Image logoImage = logo.AddComponent<Image>();

            // Convertir Texture2D a Sprite
            Sprite sprite = Sprite.Create(logoTexture, new Rect(0, 0, logoTexture.width, logoTexture.height), new Vector2(0.5f, 0.5f));
            logoImage.sprite = sprite;

            // Alinear el logo a la izquierda con un margen de 10px
            logoRect.anchorMin = new Vector2(0, 0.5f);
            logoRect.anchorMax = new Vector2(0, 0.5f);
            logoRect.pivot = new Vector2(0, 0.5f);
            logoRect.sizeDelta = new Vector2(panel.GetComponent<RectTransform>().rect.width * 0.05f, panel.GetComponent<RectTransform>().rect.height * 0.5f);
            logoRect.anchoredPosition = new Vector2(20, -5); // Desplazamiento de 10px a la izquierda

            Debug.Log("Logo agregado correctamente en el Panel");
        }
        else
        {
            Debug.LogError("No se encontró el sprite en Resources: " + spriteName);
        }
    }

    void AddIconsToPanel(GameObject panel, string[] iconNames)
    {
        float iconSize = panel.GetComponent<RectTransform>().rect.height * 0.6f;
        float padding = 10f;
        float startX = panel.GetComponent<RectTransform>().rect.width - (iconSize + padding) * iconNames.Length;

        for (int i = 0; i < iconNames.Length; i++)
        {
            string iconName = iconNames[i];
            Texture2D iconTexture = Resources.Load<Texture2D>("Icons/" + iconName);
            if (iconTexture != null)
            {
                GameObject icon = new GameObject("Icon_" + iconName);
                icon.transform.SetParent(panel.transform);
                RectTransform iconRect = icon.AddComponent<RectTransform>();
                Image iconImage = icon.AddComponent<Image>();
                UnityEngine.UI.Button iconButton = icon.AddComponent<UnityEngine.UI.Button>();
                iconButton.onClick.AddListener(() => OnIconButtonClick(iconName));

                if (iconName == "stopFlag")
                {
                    icon.SetActive(false);
                }

                Sprite sprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f));
                iconImage.sprite = sprite;

                iconRect.anchorMin = new Vector2(1, 0.5f);
                iconRect.anchorMax = new Vector2(1, 0.5f);
                iconRect.pivot = new Vector2(1, 0.5f);
                iconRect.sizeDelta = new Vector2(iconSize, iconSize);
               // iconRect.anchoredPosition = new Vector2(startX + (i * (iconSize + padding)), 0);
                iconRect.anchoredPosition = new Vector2(-((iconSize + padding) * (iconNames.Length - i)), 0);

            }
            else
            {
                Debug.LogError("No se encontró la Texture2D para el icono: " + iconName);
            }
        }
    }

    private void OnIconButtonClick(string iconName)
    {
        var actions = new System.Collections.Generic.Dictionary<string, System.Action>
        {
           { "GreenFlag", () => {
                Debug.Log("Ejecutar acción de inicio");
           }},
            { "load_icon", () => {
                Debug.Log("Cargar datos");
            }},
            { "save_icon", () => {
                Debug.Log("Guardar datos");
            }},
            { "stopFlag", () => {
                Debug.Log("Detener ejecución");
            
            }}
        };

        if (actions.TryGetValue(iconName, out var action))
        {
            action.Invoke();
        }
        else
        {
            Debug.Log("Acción no definida para: " + iconName);
        }
    }

    private void adaptScreenToMobile(CanvasScaler canvas)
    {
        CanvasScaler canvasScaler = canvas.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1080, 1920); // Resolución base
        canvasScaler.matchWidthOrHeight = 0.5f; // Ajuste equilibrado entre ancho y alto

    }

    void LoadCategories(GameObject contentPanel)
    {
        if (contentPanel == null)
        {
            Debug.LogError("LeftPanel no encontrado, no se pueden cargar categorías.");
            return;
        }

        CategoryLoader categoryLoader = gameObject.AddComponent<CategoryLoader>();
        categoryLoader.contentPanel = contentPanel;
        categoryLoader.xmlFileName = "XML/Categories";
        categoryLoader.categoryPrefab = Resources.Load<GameObject>("Prefabs/CategoryPrefab");
        categoryLoader.LoadCategoriesFromXML();
    }

    GameObject SetupLeftPanel(GameObject parent)
    {
        GameObject leftPanel = CreatePanel("LeftPanel", parent.transform, new Vector2(0, 0), new Vector2(0.15f, 0.95f), Vector2.zero, Vector2.zero, new Vector2(0, 1), Color.white);

        // Agregar ScrollRect al LeftPanel
        ScrollRect scrollRect = leftPanel.AddComponent<ScrollRect>();
        scrollRect.vertical = true;
        scrollRect.horizontal = false;

        GameObject contentPanel = new GameObject("ContentPanel");
        //contentPanel.transform.SetParent(viewport.transform, false);
        contentPanel.transform.SetParent(leftPanel.transform, false);

        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 500);

        VerticalLayoutGroup layoutGroup = contentPanel.AddComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = contentPanel.AddComponent<VerticalLayoutGroup>();
        }
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.spacing = 5; //Espacio entre elementos
        layoutGroup.padding = new RectOffset(5, 5, 5, 5); // Ajusto espaciado superior

        ContentSizeFitter contentFitter = contentPanel.AddComponent<ContentSizeFitter>();
        if (contentFitter == null)
        {
            contentFitter = contentPanel.AddComponent<ContentSizeFitter>();
        }
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect; // Asignao el contentPanel al ScrollRect

        return contentPanel;
    }


}

