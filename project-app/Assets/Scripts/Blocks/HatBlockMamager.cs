using UnityEngine;
using UnityEngine.UIElements;

public class HatBlockMamager : MonoBehaviour
{

    /* private VisualElement workspace;

     void OnEnable()
     {
         var uiDocument = GetComponent<UIDocument>();
         var root = uiDocument.rootVisualElement;

         // Referencia al Workspace
         workspace = root.Q<VisualElement>("Workspace");

         // Crear y añadir el Hat_Block al Workspace
         var newBlock = CreateHatBlock("al hacer clic en", "Icons/green_flag.png");
         workspace.Add(newBlock);
     }

     VisualElement CreateHatBlock(string text, string iconPath)
     {
         // Crear el bloque principal
         var block = new VisualElement();
         block.AddToClassList("hat-block");

         // Contenedor del contenido
         var content = new VisualElement();
         content.AddToClassList("hat-block-content");

         // Texto del bloque
         var label = new Label(text);
         label.AddToClassList("hat-block-label");

         // Icono
         var icon = new VisualElement();
         icon.AddToClassList("hat-block-icon");
         icon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>(iconPath));

         // Añadir elementos al contenedor
         content.Add(label);
         content.Add(icon);
         block.Add(content);

         return block;
     }*/
}

