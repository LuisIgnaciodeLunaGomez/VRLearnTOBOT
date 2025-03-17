/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 22/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Clase que representa un bloque visual en la interfaz de usuario premite la vinculación del modelo lógico con la UI
 * 
 */


using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

using TMPro;

public class BlockView : BaseView
{

    [SerializeField] private List<Image> m_BgImages = new List<Image>(); //Lista de imagenes que forman el fondo del bloque 
    [SerializeField] private Dictionary<string, BlockView> mBlockViews = new Dictionary<string, BlockView>(); //Diccionario que contiene los bloques
    [SerializeField] private HorizontalLayoutGroup m_inLineGroup;


    public List<BaseView> GetChildren() => base.Childs;

    private Block m_Block; //Referencia al modelo lógico del bloque

    private WorkSpaceView m_WorkSpaceView; //Referencia al gestor de la interfaz de usuario

    public override ViewType Type => ViewType.Block;

    public bool inToolBox { get; set; }
    public Vector2 Position { get; set; }
    public RectTransform ViewRectransform { get; set; }
    string BlockType => this.m_Block?.Type ?? "Unknown";
    public Block Block =>this.m_Block;

    /**
     * Descripción: Víncula el modelo lógico y los datos a la vista
     * @param: Block block
     * @param: BlockDataLoader.BlockData blockData
     */
    public void BindModel(Block block, BlockDataLoader.BlockData blockData)
    {
        if (this.m_Block == block) return; //Si el modelo lógico del bloque es el mismo que el modelo lógico del bloque actual, no se hace nada

        unBindModel(); // Si hay un bloque anterior, desvincularlo

        this.m_Block = block;
        this.m_Block.Initialize(blockData);
       //Debug.Log($"Vinculando modelo de bloque: {BlockType} en {this.m_Block.XY}");

        Childs.Clear(); // Limpia hijos anteriores

        Transform inLineGroup = transform.Find("InLineGroup");

        if (inLineGroup == null)
        {
            //Debug.LogWarning($"El bloque `{BlockType}` no tiene un InLineGroup. Creando uno.");
            GameObject groupObject = new GameObject("InLineGroup", typeof(RectTransform));
            groupObject.transform.SetParent(transform);
            groupObject.transform.localPosition = Vector3.zero;
            groupObject.transform.localScale = Vector3.one;

            //Agregar un LayoutGroup para organizar los elementos detnro
            var layout = groupObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.spacing = 5f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            inLineGroup = groupObject.transform;

            // Agregar el componente InLineGroup para que se reconozca como tal
            //groupObject.AddComponent<InLineGroup>();
            inLineGroup.gameObject.AddComponent<InLineGroup>();
        }


        // Configurar el HorizontalLayoutGroup (existente o recién creado)
        HorizontalLayoutGroup hLayout = inLineGroup.GetComponent<HorizontalLayoutGroup>();
 
        if (hLayout == null)
        {
            hLayout = inLineGroup.gameObject.AddComponent<HorizontalLayoutGroup>();
        }
        // Configurar el RectTransform del InLineGroup
        RectTransform lineGroupRect = inLineGroup.GetComponent<RectTransform>();
        lineGroupRect.sizeDelta = Vector2.zero; // Tamaño del bloque original
        lineGroupRect.localScale = Vector3.one; // Mantener escala 1:1 dentro del bloque
        lineGroupRect.anchorMin = new Vector2(0, 0f); // Centrar verticalmente
        lineGroupRect.anchorMax = new Vector2(0, 0.7f);
        lineGroupRect.pivot = new Vector2(0.5f, 0.5f);
        lineGroupRect.anchoredPosition = Vector2.zero;

        InLineGroup inLineGroupComponent = inLineGroup.GetComponent<InLineGroup>();
        inLineGroupComponent.Childs.Clear();

        // Crear elementos para los argumentos basados en los datos del XML
        foreach (var arg in m_Block.BlockData.args)
        {
            GameObject argumentObject = new GameObject(arg.type == "label" ? arg.value : arg.name);         

            if (arg.type == "label")
            {
                
                TextMeshProUGUI textComponent = argumentObject.AddComponent<TextMeshProUGUI>();
                textComponent.text = arg.value;
                textComponent.fontSize = 32;
                textComponent.color = Color.white;
                textComponent.alignment = TextAlignmentOptions.Center;
                textComponent.enableAutoSizing = true;
                LabelView labelView = argumentObject.AddComponent<LabelView>();
                inLineGroupComponent.Childs.Add(labelView);

                //Debug.Log($"Añadido LabelView: {arg.value}, Total Childs: {inLineGroupComponent.Childs.Count}");
            }
            else if (arg.type == "input")
            {
              ;
                TMP_InputField inputField = argumentObject.AddComponent<TMP_InputField>();
                inputField.text = arg.defaultValue ?? "10";
                inputField.contentType = TMP_InputField.ContentType.IntegerNumber;

                InputView inputView = argumentObject.AddComponent<InputView>();
                //inputView.SetBackgroundSprite(backgroundImage.sprite);
                inLineGroupComponent.Childs.Add(inputView);
                Debug.Log($"Añadido InputView: {arg.name}, Total Childs: {inLineGroupComponent.Childs.Count}");
            }

            else if (arg.type == "image")
            {
                // Creo un objeto Image para el ícono
                Image imageComponent = argumentObject.AddComponent<Image>();
                Debug.Log($"Valor de arg.name: '{arg.name}'");
                // Cargo el sprite desde Resources 
                Sprite sprite = Resources.Load<Sprite>($"Icons/{arg.name}");

                if (sprite != null)
                {
                    imageComponent.sprite = sprite;
                    imageComponent.preserveAspect = true; // Mantengo la proporción de la imagen
                                                          // Ajusto el tamaño del RectTransform para que se vea bien (puedes personalizarlo)
                    RectTransform rectTransform = argumentObject.GetComponent<RectTransform>();
                    //rectTransform.sizeDelta = new Vector2(sprite.rect.width , sprite.rect.height);
                    rectTransform.sizeDelta = new Vector2(80f, 80f);
                    Debug.Log($"Sprite {arg.name} cargado correctamente. Tamaño: {sprite.rect.width}x{sprite.rect.height}");
                }


                // Añadir al InLineGroup
                argumentObject.transform.SetParent(inLineGroup, false);
                argumentObject.transform.localScale = Vector3.one;
            }

            if (argumentObject != null)
            {
         
                argumentObject.transform.SetParent(inLineGroup, false);
                argumentObject.transform.localScale = Vector3.one;

               // Childs.Add(argumentObject);
               // Debug.Log($" Argumento añadido y agregado a Childs: {arg.type} - {arg.value}");

            }
            else
            {
                Debug.LogWarning($"El objeto `{argumentObject?.name}` no tiene el componente BaseView y no se puede añadir a Childs.");
            }

            // Verificar si se crearon hijos
            if (inLineGroup.childCount == 0)
            {
                Debug.LogError($"No se crearon hijos en el bloque `{BlockType}`. Verifica que el XML está bien definido.");
            }
            else
            {
                Debug.Log($"Se añadieron {inLineGroup.childCount} hijos al bloque `{BlockType}`.");
            }
            Canvas.ForceUpdateCanvases();


            // Agregar el bloque al espacio de trabajo
            if (m_WorkSpaceView != null)
            {
                Debug.Log($"Bloque {BlockType} agregado al WorkSpaceView.");
                m_WorkSpaceView.AddBlockView(this);
            }
            else
            {
                Debug.LogError($"m_WorkSpaceView no está asignado para el bloque {BlockType}.");
            }

            Debug.Log($"Total de hijos en InLineGroup: {inLineGroup.childCount}");
           // Debug.Log($"Total de hijos en Childs: {Childs.Count}");

           /* if (Childs.Count == 0)
            {
                Debug.LogError($" No se encontraron hijos en el bloque {BlockType}. Asegúrate de que el prefab tenga hijos.");
            }*/

            // Agregar el bloque al espacio de trabajo
            if (m_WorkSpaceView != null)
            {
                Debug.Log($"Bloque {BlockType} agregado al WorkSpaceView.");
                m_WorkSpaceView.AddBlockView(this);
            }

            else
            {
                Debug.LogError($"m_WorkSpaceView no está asignado para el bloque {BlockType}.");
            }

             //Debug.Log($"Nuevo sizeDelta aplicado: {ViewRectransform.sizeDelta}");
        }

        this.UpdateSize();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(inLineGroup.GetComponent<RectTransform>());


    }

    public void Dispose()
    {
        if(m_Block != null) unBindModel(); // Si el bloque no es nulo, desvincularlo
        GameObject.Destroy(ViewRectransform.gameObject); // Destruye el bloque
    }

    public void unBindModel()
    {
        m_Block = null; // Desvincula el bloque
    }

    public void UpdatePosition(Vector2 position)
    {
        if (ViewRectransform != null)
        {
            ViewRectransform.anchoredPosition = position; // Actualiza la posición del bloque en la interfaz

            Debug.Log($" Bloque {BlockType} movido a: {position}");
            // Notificar al bloque que su posición ha cambiado
            if (m_Block != null)
            {
                m_Block.XY = position;
            }
            else
            {
                Debug.LogError($" No se puede actualizar la posición del bloque {BlockType} porque ViewRectransform es nulo.");

            }
        }
    }

    public void UpdateLayout()
    {
        // Asegurar que los bloques se distribuyan correctamente en el contenedor
        if (ViewRectransform != null)
        {
            ViewRectransform.SetAsLastSibling();
        }
    }

    /**
     * Añade una imagen de fondo al bloque
     * @param image Imagen de fondo a añadir
     */
    public void AddBgImage(Image image)
    {
        if (image != null && !m_BgImages.Contains(image)) //Si la imagen de fondo no es nula y no ha sido añadida antes
            m_BgImages.Add(image); //Añade la imagen de fondo a la lista de imágenes de fondo del bloque
    }

    /**
     * Añade un color de fondo a la imagen del prefab
     * @param color Color de fondo a añadir
     */

    public void ChangeBgColor(Color color)
    {

        Image bgImage = GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.color = color;
        }
        else
        {
            Debug.LogWarning($"No se encontró Image en {gameObject.name} para cambiar el color.");
        }
    }

    public void AddBlockView(BlockView blockView)
    {
        mBlockViews[blockView.Block.ID] = blockView;
    }

    public void BuildLayout()
    {
        // BaseView startView = this.GetLineGroup(0).GetTopmostChild();
        //startView.UpdateLayout(startView.HeaderXY);

        InLineGroup lineGroup = GetLineGroup(0);
        if (lineGroup == null)
        {
            Debug.LogWarning($"No se encontró un InLineGroup en el bloque {BlockType}.");
            return;
        }

        BlockView startView = lineGroup.GetComponentInChildren<BlockView>();
        if (startView != null)
        {
            startView.UpdateLayout(startView.HeaderXY);
        }
        else
        {
            Debug.LogWarning($"No se encontró un TopmostChild en el InLineGroup del bloque {BlockType}.");
        }
    }

    public InLineGroup GetLineGroup(int index)
    {
        int count = 0;

        Debug.Log($"Buscando el {index}th InLineGroup en {BlockType}, Total de hijos en transform: {transform.childCount}");

        foreach (Transform child in transform)
        {
            Debug.Log($" Recorriendo hijo: {child.GetType().Name}");
            InLineGroup view = child.GetComponent<InLineGroup>();
            if (view != null)
            {
                if (count == index)
                {
                    Debug.Log($"Encontrado InLineGroup en {BlockType}.");
                    return view;
                }
                count++;
            }
        }
        Debug.LogErrorFormat("<color=red>Can't find the {0}th lineGroup in block view of {1}.</color>", index, this.GetType().Name);
        return null;
    }

    protected override Vector2 CalculateSize()
    {
        //bool alignRight = false;

        //Calcular el tamaño de los hijos
        Vector2 size = Vector2.zero;

        InLineGroup lineGroup = GetLineGroup(0);
        if (lineGroup != null)
        {
            size = lineGroup.CalculatedSize;

            Debug.Log($"Tamaño calculado desde InLineGroup: {size}"); 
        
        }


        // Ajustar el tamaño basado en los InputView
        foreach (var child in Childs)
        {
            if (child is InputView inputView)
            {
               // Vector2 inputSize = inputView.UpdateSize();

                inputView.UpdateSize(); // Forzar la actualización antes de obtener el tamaño
                Vector2 inputSize = inputView.GetComponent<RectTransform>().sizeDelta; // Obtener el tamaño actualizado

                size.x = Mathf.Max(size.x, inputSize.x + 20); // Añadir margen para evitar cortes
                size.y = Mathf.Max(size.y, inputSize.y);
                Debug.Log($"BlockView.CalculateSize: Considerando InputView tamaño: {inputSize}");
            }
        }
        //Debug.Log($"Calculando tamaño del bloque {BlockType}: {size}");


        if (size.x == 0 || size.y == 0)
        {
            Debug.LogError($"Tamaño inválido para el bloque {BlockType}, puede que no se renderice correctamente.");
        }


        Debug.Log($"[BlockView:CalculateSize] {gameObject.name} - InLineGroup Size: {size}");


      /*  if (m_BgImages.Count > 0 && m_BgImages[0] is CustomMeshImage customMeshImage)
        {
            List<Vector4> dimensions = new List<Vector4>
            {
            new Vector4(0, 0, size.x, size.y)
            };
            customMeshImage.SetDrawDimensions(dimensions.ToArray());

            Debug.Log($"[BlockView:CalculateSize] {gameObject.name} - Background Image SizeDelta: {m_BgImages[0].rectTransform.sizeDelta}");

        }*/

        //   ((CustomMeshImage)m_BgImages[0]).SetDrawDimensions(dimensions.ToArray());
        return size;
    }
   
      void Awake()
    {
        // Find or set m_WorkSpaceView (adjust based on your hierarchy or setup)
        m_WorkSpaceView = FindFirstObjectByType<WorkSpaceView>(); // Example: Find in scene
        if (m_WorkSpaceView == null)
        {
            Debug.LogError("No WorkSpaceView found in the scene for BlockView initialization.");
        }
    }
    public void SetWorkSpaceView(WorkSpaceView workSpaceView)
    {
        m_WorkSpaceView = workSpaceView;
    }

    public void AddLabel(string text)
    {
        GameObject labelObj = new GameObject("Label");
        Text label = labelObj.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.alignment = TextAnchor.MiddleCenter;
        label.transform.SetParent(this.transform, false);
    }

    public void AddInput(string name, string type, string defaultValue)
    {
        GameObject inputObj = new GameObject(name);
        InputField input = inputObj.AddComponent<InputField>();
        input.text = defaultValue;
        input.transform.SetParent(this.transform, false);
    }

    private void UpdateSize()
    {
        Vector2 size = CalculateSize();
        if (ViewRectransform != null)
        {
            ViewRectransform.sizeDelta = size;
            Image bgImage = GetComponent<Image>();
            if (bgImage != null && bgImage.type == Image.Type.Sliced)
            {
                bgImage.GetComponent<RectTransform>().sizeDelta = size;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(ViewRectransform); // Fuerzo la actualización
        }
        Debug.Log($"Actualizando tamaño del bloque {BlockType} a: {size}");
    }

    public void NotifyBlockView()
    {
        //this.UpdateSize();

        // LayoutRebuilder.ForceRebuildLayoutImmediate(ViewRectransform);

        if (this.m_inLineGroup != null) { 
        
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.m_inLineGroup.GetComponent<RectTransform>());
        }

        // Calcular el tamaño total del contenido (basado en el InLineGroup)
        Vector2 contentSize = CalculateContentSize();
        ViewRectransform.sizeDelta = contentSize;

        // Ajustar la imagen de fondo
        UpdateBackgroundSize();

        // Forzar la actualización del canvas para reflejar los cambios inmediatamente
        Canvas.ForceUpdateCanvases();

        Debug.Log($"BlockView actualizado a: {contentSize}");
    }

    void Start()
{
    HorizontalLayoutGroup hLayout = GetComponent<HorizontalLayoutGroup>();
    if (hLayout != null)
    {
        hLayout.childForceExpandWidth = true;  // Forzar expansión del ancho
        hLayout.childControlWidth = true;
    }
}
    private Vector2 CalculateContentSize()
    {
        if (this.m_inLineGroup == null) return ViewRectransform.sizeDelta; // Valor por defecto si no hay InLineGroup

        RectTransform groupRect = this.m_inLineGroup.GetComponent<RectTransform>();
        Vector2 size = groupRect.sizeDelta;

        // Agregar márgenes o padding si es necesario
        size.x += this.m_inLineGroup.padding.left + this.m_inLineGroup.padding.right;
        size.y += this.m_inLineGroup.padding.top + this.m_inLineGroup.padding.bottom;

        // Asegurar un tamaño mínimo
        if (size.x < 100f) size.x = 100f;
        if (size.y < 50f) size.y = 50f;

        return size;
    }

    private void UpdateBackgroundSize()
    {
        Image bgImage = GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.GetComponent<RectTransform>().sizeDelta = ViewRectransform.sizeDelta;
        }
    }

}
