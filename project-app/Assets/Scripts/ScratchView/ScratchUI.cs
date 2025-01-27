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
 * Versión: 1.0.0
 */

using UnityEngine;
using UnityEngine.UIElements;

public class ScratchUI : MonoBehaviour
{
    private ScrollView toolbox;
    private VisualElement workspace;
    private ScrollBlocks scrollBlocks; // Referencia al componente ScrollBlocks


    void OnEnable()
    {
        // Cargar el archivo UXML
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Referencias a la caja de herramientas y el workspace
        toolbox = root.Q<ScrollView>("Toolbox");
        workspace = root.Q<VisualElement>("Workspace");
        // Cargar y aplicar el archivo USS

        // Buscar el componente ScrollBlocks en la jerarquía
        scrollBlocks = Object.FindFirstObjectByType<ScrollBlocks>();
        // Asegúrate de que toolbox no es null
        if (toolbox == null)
        {
            Debug.LogError("No se encontró el Toolbox. Verifica el ID en el UXML.");
            return;
        }
        //  var styleSheet = Resources.Load<StyleSheet>("../../UI/Styles/BlockStyles");
        if (workspace == null)
        {
            Debug.LogError("No se encontró el Workspace. Verifica el ID en el UXML.");
            return;
        }
        if (scrollBlocks == null)
        {
            Debug.LogError("No se encontró el componente ScrollBlocks. Asegúrate de que está agregado a un objeto de la escena.");
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

            
            // Evento de clic
            categoryButton.RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log($"Categoría seleccionada: {categoryName}");
                scrollBlocks.ShowBlocksByCategory(categoryName);
            });
            /*  categoryButton.RegisterCallback<ClickEvent>(evt =>
              {
                  Debug.Log($"Categoría seleccionada: {categoryName}");

                  if (categoryName == "Eventos")
                  {
                      var scrollBlocksComponent = Object.FindFirstObjectByType<ScrollBlocks>();                if (scrollBlocksComponent != null)
                      {
                          scrollBlocksComponent.ShowEventBlocks();
                      }
                      else
                      {
                          Debug.LogError("No se encontró el componente ScrollBlocks.");
                      }
                  }
              });*/

            // Agregar el botón a la toolbox
            toolbox.Add(categoryButton);
        }
    }
}
