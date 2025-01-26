using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class EventBlockMutator : MonoBehaviour
{
    private VisualElement eventBlock;

    public VisualElement CreateEventBlock(string text, string iconPath)
    {
        // Cargar la estructura base desde UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/EventBlockBase.uxml");
        if (visualTree == null)
        {
            Debug.LogError("No se encontró el archivo EventBlockBase.uxml.");
            return null;
        }

        eventBlock = visualTree.CloneTree();
        var blockLabel = eventBlock.Q<Label>("BlockLabel");
        var blockIcon = eventBlock.Q<VisualElement>("BlockIcon");

        // Configurar texto
        blockLabel.text = text;
        // Configurar fondo con el ícono
        
        var blockBackground = eventBlock.Q<VisualElement>("Icons/Hat_block_grey.png");

        // Configurar icono
        if (!string.IsNullOrEmpty(iconPath))
        {
            var iconTexture = Resources.Load<Texture2D>(iconPath);
            if (iconTexture != null)
            {
                blockBackground.style.backgroundImage = new StyleBackground(iconTexture);
            }
            else
            {
                Debug.LogError($"No se pudo cargar el ícono: {iconPath}");
            }
        }

        return eventBlock;
    }

    public void AddDropdown(string dropdownText)
    {
        // Crear opciones del menú desplegable
        var options = new List<string> { "Opción 1", "Opción 2", "Opción 3" };

        // Crear PopupField con opciones
        var dropdown = new PopupField<string>("", options, 0);
        dropdown.label = dropdownText; // Etiqueta del menú desplegable

        // Agregar el menú desplegable al contenido del bloque
        var blockContent = eventBlock.Q<VisualElement>("BlockContent");
        blockContent.Add(dropdown);
    }


    public void AddNumberInput()
    {
        // Crear campo de texto para números
        var numberField = new IntegerField();
        numberField.value = 0; // Valor inicial

        // Agregar al contenido del bloque
        var blockContent = eventBlock.Q<VisualElement>("BlockContent");
        blockContent.Add(numberField);
    }
}
