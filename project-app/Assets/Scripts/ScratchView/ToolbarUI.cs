using UnityEngine;
using UnityEngine.UIElements;

public class ToolbarUI : MonoBehaviour
{
    private VisualElement toolbar;

    void OnEnable()
    {
        // Cargar el UXML
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Configurar el logo de Scratch
        var logoElement = root.Q<VisualElement>("ScratchLogo");
        if (logoElement != null)
        {
            var logoTexture = Resources.Load<Texture2D>("Icons/Scratchlogo");
            if (logoTexture != null)
            {
                logoElement.style.backgroundImage = new StyleBackground(logoTexture);
            }
            else
            {
                Debug.LogError("No se encontró el logo de Scratch.");
            }
        }
        // Referencia a la barra de herramientas
        toolbar = root.Q<VisualElement>("ToolBar");

        // Asegúrate de que la barra de herramientas está inicializada
        if (toolbar == null)
        {
            Debug.LogError("No se encontró la barra de herramientas (ToolBar) en el UXML.");
            return;
        }

        // Buscar los contenedores de botones
        var toolbarCenter = root.Q<VisualElement>("toolbar-center");
        var toolbarRight = root.Q<VisualElement>("toolbar-right");

        if (toolbarCenter == null || toolbarRight == null)
        {
            Debug.LogError("No se encontraron los contenedores (toolbar-center o toolbar-right).");
            return;
        }
        // Agregar elementos al ToolBar

        AddIcon(toolbarCenter, "Guardar", "save_icon", () =>
        {
            Debug.Log("Guardar clickeado");
        });
        AddIcon(toolbarCenter, "Cargar", "load_icon", () =>
        {
            Debug.Log("Cargar clickeado");
        });
        AddIcon(toolbarRight, "Bandera Verde", "GreenFlag", () =>
        {
            Debug.Log("Bandera verde clickeada");
        });
        AddIcon(toolbarRight, "Stop", "stopFlag", () =>
        {
            Debug.Log("Stop Flage clickeado");
        });
    }

    void AddIcon(VisualElement parent, string tooltip, string iconName, System.Action onClick)
    {
        // Cargar el icono desde Resources
        var iconTexture = Resources.Load<Texture2D>($"Icons/{iconName}");
        if (iconTexture == null)
        {
            Debug.LogError($"No se encontró el icono: {iconName}");
            return;
        }

        // Crear el elemento del botón
        var iconButton = new VisualElement();
        iconButton.AddToClassList("icon-button");
        iconButton.style.backgroundImage = new StyleBackground(iconTexture);
        iconButton.style.width = 40;
        iconButton.style.height = 40;
        iconButton.tooltip = tooltip;

        // Evento de clic
        iconButton.RegisterCallback<ClickEvent>(evt => onClick());

        parent.Add(iconButton);
    }

}
