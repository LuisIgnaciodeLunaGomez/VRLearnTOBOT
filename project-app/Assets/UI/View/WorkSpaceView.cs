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
 * Descripción: Crea la vista del espacio de trabajo
 */


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorkSpaceView : MonoBehaviour
{

    [SerializeField] private RectTransform m_codingArea; //Panel donde se van a mostrar los bloques
    [SerializeField] private BlockStatusView m_StatusView; //Vista de estado de los bloques
    private Dictionary<string, BlockView> m_blockViews = new Dictionary<string, BlockView>(); // Diccionario de bloques en la vista
    private WorkSpace m_workSpace; // Espacio de trabajo
    private BlockDataLoader.BlockData m_blockDataLoader; // Cargador de datos de bloques
    private UpdateState m_upDateState { get; set; }
    RectTransform CodingArea => m_codingArea;

    public void BindModel(WorkSpace workSpace, RectTransform codignArea)
    {
        if (this.m_workSpace != null)
        {
            this.UnBindModel(); // Desvincular el modelo antes de vincular uno nuevo
        }

        this.m_workSpace = workSpace;
        this.m_codingArea = codignArea;

       // GameObject middelPanel = GameObject.Find("MiddlePanel");
       /* if (middelPanel != null)
        {
            m_codingArea = middelPanel.GetComponent<RectTransform>();
        }*/

        if (workSpace.GetAllBlocks().Count > 0)
        {
            this.BuildViews();
        }

        Debug.Log("BindModel: WorkSpaceView: WorkSpaceView vinculado al modelo.");
    }

    public void UnBindModel()
    {
        this.m_workSpace = null;
        this.m_blockViews.Clear();

    }

    private void BuildViews()
    {
        foreach (Block block in this.m_workSpace.GetAllBlocks())
        {
            CreateBlockView(block, this.m_blockDataLoader);

        }

        //Asegurar que los bloques se alineen bien
        foreach (BlockView view in this.m_blockViews.Values)
        {
            view.BuildLayout();
            BlockConnection connection = view.Block.previousConnection?.targetConnection ?? view.Block.outputConnection?.targetConnection;

            if (connection != null)
            {
                if (connection.sourceBlock != null)
                {
                    Debug.Log($"Conectando {view.Block.type} a {connection.sourceBlock.blockModel.type}");
                }
                else
                {
                    Debug.Log($"Conectando {view.Block.type} a null");
                }
                connection.FireUpdate(UpdateState.Connected);
            }
        }
    }

    private BlockView CreateBlockView(Block block, BlockDataLoader.BlockData blockData)
    {
        Debug.Log($"Creando bloque: {block.type} en la posición: {block.XY} con {blockData.args.Count} argumentos.");

        if (block == null || blockData == null)
        {
            Debug.LogError("Error: No se puede crear la vista del bloque porque el bloque o los datos son nulos.");
            return null;
        }
        BlockView view = BlockViewFactory.CreateView(block, blockData, this);

        if (view == null)
        {
            Debug.LogError($" Error: La fábrica de bloques devolvió null para el bloquee {block.type}.");
            return null;
        }

        view.inToolBox = false; //Indica que el bloque no está en el toolbox

        // Asegurarme que el bloque se coloca en `m_codingArea`
        view.transform.SetParent(m_codingArea, false);

        // Obtener el tamaño original del prefab
        Vector2 originalSize = BlockDataLoader.GetBlockSize(blockData.spriteName);
        if (originalSize == Vector2.zero)
        {
            originalSize = new Vector2(316, 175); // Valor por defecto
            Debug.LogWarning($"Tamaño no encontrado para {blockData.spriteName}, usando 316x175 como predeterminado.");
        }

        RectTransform blockRect = view.GetComponent<RectTransform>();
        blockRect.sizeDelta = originalSize; // Establecer tamaño original
        blockRect.localScale = Vector3.one; // Restablecer escala a 1 para evitar distorsión

        // Añadir LayoutElement para controlar el tamaño
        LayoutElement layoutElement = blockRect.GetComponent<LayoutElement>() ?? blockRect.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = originalSize.x;
        layoutElement.preferredHeight = originalSize.y;
        layoutElement.flexibleWidth = 0;
        layoutElement.flexibleHeight = 0;

        // Agregar los argumentos (inputs, labels)
        foreach (var arg in blockData.args)
        {
            if (arg.type == "label")
            {
                Debug.Log($"Añadiendo etiqueta: {arg.value}");
                view.AddLabel(arg.value);
            }
            else if (arg.type == "input")
            {
                Debug.Log($"Añadiendo input: {arg.name} ({arg.inputType}) con valor {arg.value}");
                view.AddInput(arg.name, arg.inputType, arg.value);
            }
        }

        //Me  aseguro que el bloque tenga un InLineGroup
        InLineGroup lineGroup = view.gameObject.GetComponent<InLineGroup>();
        if (lineGroup == null)
        {
            Debug.LogWarning($"Añadiendo InLineGroup a {block.type} porque no estaba presente.");
            lineGroup = view.gameObject.AddComponent<InLineGroup>(); // Agrega el componente
        }

        // Configurar el HorizontalLayoutGroup dentro de InLineGroup
        HorizontalLayoutGroup hLayout = lineGroup.GetComponent<HorizontalLayoutGroup>();
        if (hLayout == null)
        {
            hLayout = lineGroup.gameObject.AddComponent<HorizontalLayoutGroup>();
        }
        hLayout.childAlignment = TextAnchor.MiddleLeft; // Centrar verticalmente
        hLayout.childForceExpandWidth = false; // No forzar expansión
        hLayout.childForceExpandHeight = false;
        hLayout.spacing = 5; // Espacio entre elementos
        hLayout.padding = new RectOffset(10, 10, 0, 0); // Padding para centrar el contenido

        // Configurar el RectTransform del InLineGroup
        RectTransform lineGroupRect = lineGroup.GetComponent<RectTransform>();
        lineGroupRect.sizeDelta = originalSize; // Ajustar al tamaño del bloque
        lineGroupRect.localScale = new Vector3(1, 1, 1); // Mantener escala 1:1 dentro del bloque


        // Verificar la posición antes de asignarla
        Debug.Log($"Posición inicial del bloque {block.type}: {block.XY}");
        // Actualizar la posición correctamente
        view.XY = block.XY;  // Asegurar que la coordenada de bloque se respete

        view.UpdatePosition(block.XY);

        // Me Aseguro que InLineGroup se registra en los hijos del bloque
        if (!view.Childs.Contains(lineGroup))
        {
            Debug.Log($" Registrando InLineGroup en {block.type}");
            view.Childs.Add(lineGroup);
        }

        // Agregar Input dentro de LineGroup
        InputView inputView = new GameObject("Input_").AddComponent<InputView>();
        inputView.transform.SetParent(lineGroup.transform, false);
        lineGroup.Childs.Add(inputView);

        // Agregar Field Label (etiqueta del bloque)
        FieldView fieldLabel = new GameObject("Field_label").AddComponent<FieldView>();
        fieldLabel.transform.SetParent(inputView.transform, false);
        inputView.Childs.Add(fieldLabel);

        // Agregar Connection_output (salida del bloque)
        ConnectionView connectionOutput = new GameObject("Connection_output").AddComponent<ConnectionView>();
        connectionOutput.transform.SetParent(view.transform, false);
        view.Childs.Add(connectionOutput);

        //Agregar Connection_input (entrada del bloque si aplica)
        if (block.inputList != null && block.inputList.Count > 0)
        {
            ConnectionView connectionInput = new GameObject("Connection_input").AddComponent<ConnectionView>();
            connectionInput.transform.SetParent(inputView.transform, false);
            inputView.Childs.Add(connectionInput);
        }

        // Posicionar el bloque
        view.XY = block.XY;
        view.UpdatePosition(block.XY);

        BlockBehaviour blockBehaviour = view.GetComponent<BlockBehaviour>();
        blockBehaviour?.Initialize(blockData, m_workSpace);

        // Si el bloque tiene hijos, asegurarse de crearlos y conectarlos
        foreach (Block childBlock in block.childBlocks)
        {
            Debug.Log($"[BuildBlockView] Conectando bloque hijo: {childBlock.type}");
            BlockView childView = CreateBlockView(childBlock, blockData);

            if (childView != null)
            {
                Debug.Log($"Conectando {block.type} con {childBlock.type}");
                ConnectBlocks(view, childView); // Crear la conexión visual
            }
            BlockConnection connection = childBlock.previousConnection?.targetConnection ??
                          childBlock.outputConnection?.targetConnection;

            if (connection != null)
            {
                Debug.Log($"Conexión establecida entre {block.type} y {childBlock.type}");
                connection.FireUpdate(UpdateState.Connected);
            }
            else
            {
                Debug.LogWarning($"No se encontró conexión para el bloque {childBlock.type}");
            }
        }

        m_blockViews[block.ID] = view;

        Debug.Log($"Bloque {block.type} añadido at m_blockViews.");

        return view;
    }

    private void ConnectBlocks(BlockView parent, BlockView child)
    {
        if (parent == null || child == null)
        {

            Debug.LogError(" Error al conectar bloques: Parent o Child es null.");
            return;

        }

        Debug.Log($"Conectando bloques: {parent.Type} -> {child.Type}");

        /* BlockConnection connection = null;

         if (child.Block.previousConnection != null)
             connection = child.Block.previousConnection.targetConnection;
         else if (child.Block.outputConnection != null)
             connection = child.Block.outputConnection.targetConnection;

         if (connection != null)
         {
             Debug.Log($"Conexión establecida entre {parent.Type} y {child.Type}");

             connection.FireUpdate(UpdateState.Connected);
         }
         else
         {
             Debug.LogWarning($"No se encontró conexión para {child.Type}");
         }*/

        BlockConnection parentConnection = parent.Block.nextConnection ?? parent.Block.outputConnection;
        BlockConnection childConnection = child.Block.previousConnection ?? child.Block.inputList[0]?.Connection;

        if (parentConnection != null && childConnection != null)
        {
            parentConnection.targetConnection = childConnection;
            childConnection.targetConnection = parentConnection;

            Debug.Log($"Conexión establecida entre {parent.Block.type} y {child.Block.type}");

            parentConnection.FireUpdate(UpdateState.Connected);
            childConnection.FireUpdate(UpdateState.Connected);
        }
        else
        {
            Debug.LogWarning($"No se encontró conexión válida para {child.Block.type}");
        }
    }

    public void CleanViews()
    {
        foreach (BlockView view in this.m_blockViews.Values)
        {
            Destroy(view.gameObject);
        }
        this.m_blockViews.Clear();
    }


    public void AddBlockView(BlockView blockView)
    {
        this.m_blockViews[blockView.Block.ID] = blockView;
    }

    public void RemoveBlockView(BlockView blockView)
    {
        if (blockView == null)
        {
            Debug.LogError("RemoveBlockView: blockView es null.");
            return;
        }

        if (blockView.Block == null)
        {
            //Debug.LogError($"RemoveBlockView: blockView {blockView.name} no tiene un Block asignado.");
            return;
        }
        BlockView view = blockView as BlockView;
        if (view != null && this.m_blockViews.ContainsKey(view.Block.ID))
        {
            Destroy(view.gameObject);
            this.m_blockViews.Remove(view.Block.ID);
        }
        else
        {
            Debug.LogWarning($"RemoveBlockView: El bloque con ID {blockView.Block.ID} no está en la lista.");
        }
    }
    
    public void Dispose()
    {
        UnBindModel();
        foreach (var blockView in m_blockViews.Values)
        {
            Destroy(blockView.gameObject);
        }
        m_blockViews.Clear();

        BlockViewSettings.Dispose();
       // Resources.UnloadUnusedAssets();
    }

    public void Initialized(GameObject middlePanel, GameObject rightPanel)
    {
        if (rightPanel != null)
        {
            m_codingArea = rightPanel.GetComponent<RectTransform>();
            if (m_codingArea == null)
            {
                Debug.LogError("Initialized: BLockSpaceView: No se encontró RectTransform en rightPanel.");
            }
        }
        else
        {
            Debug.LogError("Initialized: BLockSpaceView: rightPanel es null en Initialized.");
        }
        Debug.Log("Initialized: BLockSpaceView: WorkSpaceView inicializado con middlePanel y rightPanel.");
    }
}
