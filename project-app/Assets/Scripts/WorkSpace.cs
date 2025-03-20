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
 * Versión: 1.0.1
 * 
 * Descripción: Gestor lógico del área de trabajo de la aplicación 
 */

using System.Collections.Generic;
using UnityEngine;

public class WorkSpace : MonoBehaviour
{
    public string Id;
    private static Dictionary<string, WorkSpace> m_WorkspaceDB = new Dictionary<string, WorkSpace>();
    public List<Block> TopBlocks { get; private set; } //Lista de bloques principales en el espacio de trabajo
    public Dictionary<string, Block> BlockDB { get; private set; }//Diccionario de bloques en el espacio de trabajo
    private GameObject m_MiddlePanel;
    private GameObject m_RightPanel;
    private List<BlockBehaviour> m_blocks = new List<BlockBehaviour>(); //Componentes que representan bloques
    private List<BlockConnection> pendingConnections = new List<BlockConnection>(); // Lista de conexiones pendientes

    public List<BlockBehaviour> blocksInWorkspace => new List<BlockBehaviour>(FindObjectsOfType<BlockBehaviour>());

    private Dictionary<EConnection, BlockConnectionDB> ConnectionDBs; //Almacena las conexiones organizadas por tipo de conexión

    /**
     * Descripción: Constructor de la clase
     * @param: optId: Identificador del espacio de trabajo
     */
    public WorkSpace(string optId = null)
    {
        Id = optId ?? Utilidades.GenUid();

        if (m_WorkspaceDB.ContainsKey(Id))
        {
            m_WorkspaceDB[Id] = this;
            //Debug.LogWarning("Ya existe un espacio de trabajo con el ID" + Id);
            return;
        }

        else
        {
            m_WorkspaceDB.Add(Id, this);
        }
        TopBlocks = new List<Block>();
        BlockDB = new Dictionary<string, Block>();
    }


    /**
     * Descripción: Obtiene todos los bloques principales
     * return: Lista de bloques
     * */
    public List<Block> GetTopBlocks()
    {
        return new List<Block>(TopBlocks);
    }

    /**
     * Descripción:Limpia el espacio de trabajo
     */
    public void Clear()
    {

        while (TopBlocks.Count > 0)
        {

            Block block = TopBlocks[TopBlocks.Count - 1];
              TopBlocks.RemoveAt(TopBlocks.Count - 1);
            block = null; // Libera memoria

        }

        TopBlocks.Clear();
        BlockDB.Clear();
    }

    /** 
     * Descripción: Obtener un blooque por su ID
     * return: Bloque
     */
    public Block GetBlockByID(string ID)
    {
        Block block = null; //Crea un bloque
        BlockDB.TryGetValue(ID, out block); //Conecta con la base de datos para obtener el valor
        return block; //Devuelve el bloque
    }

    /**
     * Descripción: Obtiene todos los bloques en el espacio de trabajo
     * return: Lista de bloques
     */
    public List<Block> GetAllBlocks()
    {
        return new List<Block>(BlockDB.Values);

    }

    /**
     * Descripción: Agrega un bloque principal al espacio de trabajo
     * @param: block: Bloque a añadir
     */
    public void AddTopBlocks(Block block)
    {
        if (block == null) return;

        if (!TopBlocks.Contains(block))
        {
            TopBlocks.Add(block);
        }
        BlockDB[block.ID] = block;

    }

    /**
     * Descripción: Elimna un bloque principal del espacio de trabajo
     * @param: block: Bloque a eliminar
     */
    public void RemoveTopBlock(Block block)
    {
        if (TopBlocks.Contains(block))
        {
            TopBlocks.Remove(block);
        }
        if (BlockDB.ContainsKey(block.ID))
        {
            BlockDB.Remove(block.ID);
        }
    }

    /**
     * Descripción: Método que recupera un espacio de trabajo por su ID
     * @param: ID: Identificador del espacio de trabajo
     */
    public static WorkSpace GetWorkSpace(string ID)
    {
        return m_WorkspaceDB.TryGetValue(ID, out var workspace) ? workspace : null;
    }

    /**
     * Descripción: Método que elimina un espacio de trabajo
     */
    public void Dispose()
    {
        m_WorkspaceDB.Remove(Id);
        Clear();
    }

    /**
     * Descripción: Inicializa el espacio de trabajo con los paneles Middle y Right
     * @param middle: Panel central para mostrar los bloques por categorías
     * @param right: Panel derecho - área de codificación y conexión de bloques
     */
    public void Initialized(GameObject middle, GameObject right)
    {

        this.m_MiddlePanel = middle;
        this.m_RightPanel = right;
        //Debug.Log("WorksPace inizializado con MiddlePanel y RightPanel");

        //Inicializo la base de datos de conexiones
        ConnectionDBs = new Dictionary<EConnection, BlockConnectionDB>
        {
            { EConnection.NextStatement, new BlockConnectionDB() },
            { EConnection.PrevStatement, new BlockConnectionDB() },
            { EConnection.InputValue, new BlockConnectionDB() },
            { EConnection.OutputValue, new BlockConnectionDB() }
        };
        Debug.Log(" bases de datos de conexiones configuradas.");
    }

    void Awake()
    {

        Id = Utilidades.GenUid();
        TopBlocks = new List<Block>();
        BlockDB = new Dictionary<string, Block>();

        if (m_WorkspaceDB.ContainsKey(Id))
        {
            m_WorkspaceDB[Id] = this;
            Debug.LogWarning("Ya existe un espacio de trabajo con el ID " + Id);
        }
        else
        {
            m_WorkspaceDB.Add(Id, this);
        }

    }

    #region conexiones
    /**
     * Descripción: Método que busca la conexión más cercana a una conexión dada
     * @param: connection: Conexión de referencia
     * @param : maxRadius: Radio máximo de búsqueda
     * @param : dxy: Vector de desplazamiento
     * @return: Conexión más cercana
     */
    public BlockConnection FindClosest(BlockConnection connection, float maxRadius, Vector2 dxy = default)
    {
        if (connection == null) return null;

        EConnection oppositeType = connection.type switch
        {
            EConnection.NextStatement => EConnection.PrevStatement,
            EConnection.PrevStatement => EConnection.NextStatement,
            EConnection.InputValue => EConnection.OutputValue,
            EConnection.OutputValue => EConnection.InputValue,
            _ => connection.type
        };

        BlockConnectionDB db = ConnectionDBs.ContainsKey(oppositeType) ? ConnectionDBs[oppositeType] : null;
        if (db == null) return null;

        //return db.FindClosest(connection, maxRadius, dxy);
        BlockConnection closest = db.FindClosest(connection, maxRadius, dxy);
        if (closest == null)
        {
           // Debug.LogWarning($"FindClosest: No se encontró una conexión en el radio {maxRadius}. Buscando la más cercana.");
            closest = db.FindClosest(connection, maxRadius * 2, dxy); // Busca en un radio mayor
        }

        return closest;
    }

    /**
     * Descripción: Método que registra un bloque y sus conexiones en la BBDD
     * @param: block: Bloque a registrar
     */
    public void AddBlock(BlockBehaviour block)
    {
        if (block == null)
        {
           // Debug.LogError("AddBlock: BlockBehaviour es null.");
            return;
        }

        if (block.blockModel == null)
        {
            //Debug.LogError($"AddBlock: WorkSpace: blockModel es null para el bloque {block.blockType}.");
            return;
        }
        if (block != null && !block.isATemplate)
        {
        
            if (block.NextConnection != null)
            {
                if (block.NextConnection.sourceBlock == null)
                {
                 //   Debug.LogWarning($"El bloque {block.blockType} tiene nextConnection pero no tiene sourceBlock.");
                    //return;
                    pendingConnections.Add(block.NextConnection);
                }
                else
                {
                    ConnectionDBs[EConnection.NextStatement].AddConnection(block.NextConnection);
                }
            }

            // Registrar previousConnection
            if (block.previousConnection != null && block.previousConnection.sourceBlock != null)
            {
                ConnectionDBs[EConnection.PrevStatement].AddConnection(block.previousConnection); //Añade la conexión a la base de datos
              //  Debug.Log($"AddBlock: WorkSpace: Registrada previousConnection para {block.blockType} en PrevStatement DB, SourceBlock: {block.previousConnection.sourceBlock.gameObject.name}");
            }
            else
            {
              //  Debug.LogWarning($"AddBlock: WorkSpace: previousConnection para {block.blockType} es null o SourceBlock es null.");
                return;
            }

            // Registrar conexiones de entrada (inputList)
            foreach (var input in block.blockModel.inputList)
            {
                if (input.Connection != null && input.Connection.type == EConnection.InputValue && input.Connection.sourceBlock != null)
                {
                    ConnectionDBs[EConnection.InputValue].AddConnection(input.Connection); //Añade la conexión a la base de datos
                 //   Debug.Log($"AddBlock: WorkSpace:Registrada inputConnection para {block.blockType} en InputValue DB, SourceBlock: {input.Connection.sourceBlock.gameObject.name}");
                }
                else
                {
                  //  Debug.LogWarning($"AddBlock: WorkSpace: inputConnection para {block.blockType} tiene SourceBlock null o tipo incorrecto, no se registra.");
                }
            }

            if (block.blockModel.outputConnection != null && block.blockModel.outputConnection.type == EConnection.OutputValue && block.blockModel.outputConnection.sourceBlock != null)
            {
                ConnectionDBs[EConnection.OutputValue].AddConnection(block.blockModel.outputConnection); //Añade la conexión a la base de datos
               // Debug.Log($"AddBlock: WorkSpace: Registrada outputConnection para {block.blockType} en OutputValue DB, SourceBlock: {block.blockModel.outputConnection.sourceBlock.gameObject.name}");
            }
            else
            {
               // Debug.LogWarning($"AddBlock: WorkSpace: outputConnection para {block.blockType} tiene SourceBlock null o tipo incorrecto, no se registra.");
            }


          //  Debug.Log($"Addblock: WorkSpace: Bloque {block.blockType} añadido al WorkSpace con conexiones registradas. " +
           //        $"NextStatement DB: {ConnectionDBs[EConnection.NextStatement].Count}, " +
           //        $"PrevStatement DB: {ConnectionDBs[EConnection.PrevStatement].Count}");
        }
    }

    /**
     * Descrición: Método que elimina un bloque y sus conexiones de la BBDD
     * @param: block: Bloque a eliminar
     */
    public void RemoveBlock(BlockBehaviour block)
    {
        if (block != null && !block.isATemplate)
        {
            ConnectionDBs[EConnection.NextStatement].RemoveConnection(block.NextConnection);
            ConnectionDBs[EConnection.PrevStatement].RemoveConnection(block.previousConnection);
            foreach (var input in block.blockModel.inputList)
            {
                if (input.Connection != null && input.Connection.type == EConnection.InputValue)
                {
                    ConnectionDBs[EConnection.InputValue].RemoveConnection(input.Connection);
                }
            }
            if (block.blockModel.outputConnection != null && block.blockModel.outputConnection.type == EConnection.OutputValue)
            {
                ConnectionDBs[EConnection.OutputValue].RemoveConnection(block.blockModel.outputConnection);
            }
            Debug.Log($"Bloque {block.blockType} removido del WorkSpace.");
        }
    }
    #endregion

/**
 * Descripción: Método que verifica si hay otros bloques en el área de trabajo
*@param: currentBlock: Bloque actual
*/
public bool HasOtherBlocks(BlockBehaviour currentBlock)
{
    return m_blocks.Count > 1; // Retorna true si hay más de un bloque (excluyendo el actual)
}

}