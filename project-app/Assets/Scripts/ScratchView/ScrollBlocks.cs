using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ScrollBlocks : MonoBehaviour
{
    private VisualElement scrollBlocks; // Zona donde se mostrarán los bloques

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Referencia a la zona de bloques
        scrollBlocks = root.Q<VisualElement>("ScrollBlocks");
        if (scrollBlocks == null)
        {
            Debug.LogError("No se encontró la zona de bloques (ScrollBlocks).");
            return;
        }


        // Mostrar los bloques de eventos por defecto
        //ShowEventBlocks();
    }

    public void ShowEventBlocks()

    {
        Debug.Log("Cargando bloques de eventos...");

        // Limpia los bloques existentes
        scrollBlocks.Clear();
        var eventBlock = gameObject.AddComponent<EventBlockMutator>();

        // Ejemplo de bloque "al hacer clic en"
        var block1 = eventBlock.CreateEventBlock("al hacer clic en", " ");
        
        //Ojo los bloques se crean en evetBlocks.css o en json
        
        block1.AddToClassList("hat-block");

        scrollBlocks.Add(block1);

        // Agregar un menú desplegable al bloque
       // eventBlock.AddDropdown("Tecla:");

        // Ejemplo de bloque con número
       // var block2 = eventBlock.CreateEventBlock("cuando volumen del sonido
        // Añade los bloques de eventos
        foreach (var blockData in EventBlocks.Blocks)
        {
            Debug.Log($"Creando bloque: {blockData.text}");
            var block = CreateBlock(blockData.text, blockData.iconPath);
            scrollBlocks.Add(block);
        }
    }

    VisualElement CreateBlock(string text, string iconPath)
    {
        Debug.Log($"Creando bloque con texto: {text}");

        var block = new VisualElement();
        block.AddToClassList("block");

        // Contenido del bloque
        var content = new VisualElement();
        content.AddToClassList("block-content");

        // Texto
        var label = new Label(text);
        label.AddToClassList("block-label");

        // Icono
        if (!string.IsNullOrEmpty(iconPath))
        {
            Debug.Log($"Cargando ícono desde: {iconPath}");
            var icon = new VisualElement();
            icon.AddToClassList("block-icon");
            var iconTexture = Resources.Load<Texture2D>(iconPath);
            if (iconTexture != null)
            {
                icon.style.backgroundImage = new StyleBackground(iconTexture);
            }
            else
            {
                Debug.LogError($"No se pudo cargar el ícono: {iconPath}");
            }
            content.Add(icon);
        }

        content.Add(label);
        block.Add(content);

        return block;
    }

    VisualElement CreateHatBlock(string label, string iconPath)
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/HatBlock.uxml");
        if (visualTree != null)
        {
            var hatBlock = visualTree.CloneTree();
            var labelElement = hatBlock.Q<Label>("HatBlockLabel");
            labelElement.text = label;

            var iconElement = hatBlock.Q<VisualElement>("hat-block-icon");
            if (!string.IsNullOrEmpty(iconPath))
            {
                var iconTexture = Resources.Load<Texture2D>(iconPath);
                iconElement.style.backgroundImage = new StyleBackground(iconTexture);
            }

            return hatBlock;
        }
        else
        {
            Debug.LogError("No se encontró el archivo HatBlock.uxml.");
            return null;
        }
    }

}
