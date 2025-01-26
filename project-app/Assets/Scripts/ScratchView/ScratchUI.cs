using UnityEngine;
using UnityEngine.UIElements;

public class ScratchUI : MonoBehaviour
{
    private ScrollView toolbox;
    private VisualElement workspace;

    void OnEnable()
    {
        // Cargar el archivo UXML
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Referencias a la caja de herramientas y el workspace
        toolbox = root.Q<ScrollView>("Toolbox");
        //  workspace = root.Q<VisualElement>("Workspace");

        // Asegúrate de que toolbox no es null
        if (toolbox == null)
        {
            Debug.LogError("No se encontró el Toolbox. Verifica el ID en el UXML.");
            return;
        }

        // Lista de categorías con sus colores
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

        // Crear los botones
        foreach (var category in categories)
        {
            AddCategory(category.name, category.color);
        }

        void AddCategory(string categoryName, Color color)
        {
            // Crear botón de categoría
            var categoryButton = new VisualElement();
            categoryButton.AddToClassList("category-button");

            // Icono redondo
            var categoryIcon = new VisualElement();
            categoryIcon.AddToClassList("category-icon");
            categoryIcon.style.backgroundColor = color; // Aplica color dinámico

            // Texto debajo
            var categoryLabel = new Label(categoryName);
            categoryLabel.AddToClassList("category-label");

            // Añadir ícono y texto al botón
            categoryButton.Add(categoryIcon);
            categoryButton.Add(categoryLabel);

            // Agregar evento de clic
            categoryButton.RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log($"Categoría seleccionada: {categoryName}");
            });

            // Agregar el botón a la toolbox
            toolbox.Add(categoryButton);
        }
    }
}
