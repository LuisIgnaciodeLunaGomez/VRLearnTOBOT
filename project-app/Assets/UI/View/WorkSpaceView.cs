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
 * Descripción: 
 */



using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using UnityEngine;

public class WorkSpaceView : MonoBehaviour
{

    [SerializeField] private RectTransform m_codingArea; //Panel donde se van a mostrar los bloques
    private Dictionary<string, BlockView> m_blockViews = new Dictionary<string, BlockView>(); // Diccionario de bloques en la vista
    private WorkSpace m_workSpace; // Espacio de trabajo
    private BlockDataLoader.BlockData m_blockDataLoader; // Cargador de datos de bloques
    private UpdateState m_upDateState { get; set; }
    RectTransform CodingArea => m_codingArea;

    public void BindModel(WorkSpace workSpace, RectTransform codignArea)
    {
        if (this.m_workSpace != null)
        {
            UnBindModel(); // Desvincular el modelo antes de vincular uno nuevo
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
            BuildViews();
        }
    }

    public void UnBindModel()
    {
        m_workSpace = null;
        m_blockViews.Clear();

    }

    private void BuildViews()
    {
        foreach (Block block in m_workSpace.GetAllBlocks())
        {
            CreateBlockView(block, m_blockDataLoader);

        }

        //Asegurar que los bloques se alineen bien
        foreach (BlockView view in m_blockViews.Values)
        {
            view.BuildLayout();
        }
    }

    private BlockView CreateBlockView(Block block, BlockDataLoader.BlockData blockData)
    {
        Debug.Log($"Creando bloque: {block.Type} en la posición: {block.XY} con {blockData.args.Count} argumentos.");

        if (block == null || blockData == null)
        {
            Debug.LogError("Error: No se puede crear la vista del bloque porque el bloque o los datos son nulos.");
            return null;
        }
        BlockView view = BlockViewFactory.CreateView(block, blockData, this);

        if (view == null)
        {
            Debug.LogError($" Error: La fábrica de bloques devolvió null para el bloquee {block.Type}.");
            return null;
        }

        view.inToolBox = false; //Indica que el bloque no está en el toolbox

        // Asegurarme que el bloque se coloca en `m_codingArea`
        view.transform.SetParent(m_codingArea, false);

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
            Debug.LogWarning($"Añadiendo InLineGroup a {block.Type} porque no estaba presente.");
            lineGroup = view.gameObject.AddComponent<InLineGroup>(); // Agrega el componente
        }

        // Verificar la posición antes de asignarla
        Debug.Log($"Posición inicial del bloque {block.Type}: {block.XY}");
        // Actualizar la posición correctamente
        view.XY = block.XY;  // Asegurar que la coordenada de bloque se respete

        view.UpdatePosition(block.XY);

        // Me Aseguro que InLineGroup se registra en los hijos del bloque
        if (!view.Childs.Contains(lineGroup))
        {
            Debug.Log($" Registrando InLineGroup en {block.Type}");
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
        if (block.InputList != null && block.InputList.Count > 0)
        {
            ConnectionView connectionInput = new GameObject("Connection_input").AddComponent<ConnectionView>();
            connectionInput.transform.SetParent(inputView.transform, false);
            inputView.Childs.Add(connectionInput);
        }

        // Si el bloque tiene hijos, asegurarse de crearlos y conectarlos
        foreach (Block childBlock in block.ChildBlocks)
        {
            Debug.Log($"Buscando conectar {block.Type} con {childBlock.Type}");
            BlockView childView = CreateBlockView(childBlock, blockData);
            if (childView != null)
            {
                Debug.Log($"Conectando {block.Type} con {childBlock.Type}");
                ConnectBlocks(view, childView); // Crear la conexión visual
            }
        }

        m_blockViews[block.ID] = view;

        Debug.Log($"Bloque {block.Type} añadido a m_blockViews.");

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

        BlockConnection connection = null;

        if (child.Block.PreviousConnection != null)
            connection = child.Block.PreviousConnection.TargetConnection;
        else if (child.Block.OutputConnection != null)
            connection = child.Block.OutputConnection.TargetConnection;

        if (connection != null)
        {
            Debug.Log($"🔗 Conexión establecida entre {parent.Type} y {child.Type}");

            connection.FireUpdate(UpdateState.Connected);
        }
        else
        {
            Debug.LogWarning($"No se encontró conexión para {child.Type}");
        }
    }

    public void CleanViews()
    {
        foreach (BlockView view in m_blockViews.Values)
        {
            Destroy(view.gameObject);
        }
        m_blockViews.Clear();
    }

    /* public void AddBlockView(BlockView blockView)
     {
         BlockView view = blockView as BlockView;
         if (view != null && !m_blockViews.ContainsKey(view.Block.ID))
         {
             view.transform.SetParent(m_CodingArea, false);
             m_blockViews[view.Block.ID] = view;
         }
     }*/

    public void AddBlockView(BlockView blockView)
    {
        m_blockViews[blockView.Block.ID] = blockView;
    }

    void RemoveBlockView(BlockView blockView)
    {
        BlockView view = blockView as BlockView;
        if (view != null && m_blockViews.ContainsKey(view.Block.ID))
        {
            Destroy(view.gameObject);
            m_blockViews.Remove(view.Block.ID);
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


}
