/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 26/01/2025
 * 
 * Versión: 1.0.1
 * 
 * Descripción. Genera la barra lateral con las categorías y colores de los bloques de Scratch. Permitirá seleccionar una categoría y mostrar los bloques correspondientes en el Canvas.
 */

using UnityEngine;
using UnityEngine.UIElements;

public class ScratchUI : MonoBehaviour
{
    private ScrollView toolbox;
    private VisualElement root;
    //private ScrollBlocks scrollBlocks; // Referencia al componente ScrollBlocks
    //private BlockMangerCanvas blockManagerCanvas; // Referencia al script de Canvas

    void OnEnable()
    {
        // Cargar el archivo UXML
        var uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("UIDocument no encontrado en la escena - Scratch UI.");
            return;
        }
        
        var root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogError("RootVisualElement es NULL. ScratchUI");
            return;
        }

        // Buscar la Toolbox
        toolbox = root.Q<ScrollView>("Toolbox");
        if (toolbox == null)
        {
            Debug.LogError("No se encontró Toolbox en el UXML. Verifica el nombre en el UXML.");
            return;
        }

        // Evitar el blqueo de los eventos de clic en los bloques
        toolbox.pickingMode = PickingMode.Ignore;


        Debug.Log("Toolbox encontrado correctamente.");
        
        // Lista de categorías con colores
        var categories = new (string name, Color color)[]
        {
            ("Movimiento", new Color(0.2f, 0.4f, 1f)),     // Azul
            ("Apariencia", new Color(0.6f, 0.4f, 1f)),    // Morado
            ("Sonido", new Color(1f, 0.4f, 0.6f)),        // Rosa
            ("Eventos", new Color(1f, 0.8f, 0f)),         // Amarillo
            ("Control", new Color(1f, 0.6f, 0f)),         // Naranja
            ("Sensores", new Color(0.4f, 0.8f, 1f)),      // Celeste
            ("Operadores", new Color(0.4f, 0.8f, 0.4f)),  // Verde
            ("Variables", new Color(1f, 0.6f, 0.2f)),     // Naranja oscuro
            ("Mis bloques", new Color(1f, 0.4f, 0.4f))    // Rojo
        };

        // Crear los botones de categorías
        foreach (var category in categories)
        {
            AddCategory(category.name, category.color);
        }
        
        this.AdjustCanvasPanels();

    }

    private void AddCategory(string categoryName, Color color)
    {
        if (toolbox == null)
        {
            Debug.LogError("Toolbox es NULL. No se pueden agregar categorías.");
            return;
        }
        // Crear botón de categoría
        var categoryButton = new VisualElement();
        categoryButton.AddToClassList("category-button");

        // Círculo de color
        var categoryIcon = new VisualElement();
        categoryIcon.AddToClassList("category-icon");
        categoryIcon.style.backgroundColor = color;

        // Texto debajo
        var categoryLabel = new Label(categoryName);
        categoryLabel.AddToClassList("category-label");

        // Añadir ícono y texto al botón
        categoryButton.Add(categoryIcon);
        categoryButton.Add(categoryLabel);

        // Evento de clic
        categoryButton.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log($"Categoría seleccionada: {categoryName}");
        });

        // Agregar el botón a la toolbox
        toolbox.Add(categoryButton);
    }

    void AdjustCanvasPanels()
    {
        RectTransform zonaDeBloques = GameObject.Find("BlockZone").GetComponent<RectTransform>();
        RectTransform espacioDeTrabajo = GameObject.Find("WorkSpace").GetComponent<RectTransform>();

        // Obtiene el ancho de la Toolbox desde UI Toolkit
        float toolboxWidth = toolbox.resolvedStyle.width;

        // Ajustar posiciones
        zonaDeBloques.offsetMin = new Vector2(toolboxWidth, 0);
        espacioDeTrabajo.offsetMin = new Vector2(toolboxWidth + zonaDeBloques.rect.width, 0);
    }

}
