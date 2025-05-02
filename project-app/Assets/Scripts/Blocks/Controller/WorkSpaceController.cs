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
 * Versión: 2.0.0
 * 
 * Descripción: Proporcionar una API segura para que otros controladores (InputController, BlockDragController, ExecutionController) modifiquen el WorkspaceModel.
 * 
 * Aplicar reglas de negocio o validaciones antes de confirmar cambios en el modelo.
 *
 * Orquestar acciones complejas que involucran múltiples modelos (conectar dos bloques).
 *
 * Gestionar el historial de Undo/Redo.
 * 
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class WorkspaceController : MonoBehaviour
{
   
    public static WorkspaceController Instance { get; private set; }

    private WorkSpaceModel m_WorkspaceModel; 
    [SerializeField] private WorkSpaceView m_WorkspaceView; 

    public bool IsReadOnly() => m_WorkspaceModel?.Options.ReadOnly ?? true;

    void Awake()
    {
        //Debug.LogError("<color=red>HASHCODE_CHECK - MiControlador - AWAKE - HashCode(this): " + this.GetHashCode());
        if (Instance == null)
        {
            Instance = this; 

        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }
    public void InitializeController(WorkSpaceModel workspace, WorkSpaceView view)
    {
        m_WorkspaceModel = workspace ?? throw new ArgumentNullException(nameof(workspace));
        m_WorkspaceView = view;
        if (m_WorkspaceView == null) m_WorkspaceView = FindFirstObjectByType<WorkSpaceView>();
        if (m_WorkspaceView == null) Debug.LogError("WorkspaceController: WorkSpaceView reference is missing!", this.gameObject);
        //  Debug.Log("WorkspaceController Initialized with UBlockly.Workspace.");
       // Debug.LogError($"<color=red>HASHCODE_CHECK - WorkspaceController Initialize - Received/Stored Workspace HashCode: {m_WorkspaceModel?.GetHashCode()}");

    }

    #region API para Otros Controladores


    public BlockModel ConfirmAddBlock(BlockModel potentialBlock)
    {
        Debug.Log($"WorkspaceController.ConfirmAddBlock: Called for block {potentialBlock?.ID} ({potentialBlock?.Type}). isTemplateClone?: true.");

        BlockModel ublocklyBlock = potentialBlock as BlockModel; 

        if (ublocklyBlock == null)
        {
            Debug.LogError("WorkspaceController.ConfirmAddBlock: Expected a UBlockly.BlockModel but received a different type.");
            return null; 
        }


        if (IsReadOnly() || m_WorkspaceModel == null) return null;

        ublocklyBlock.SetParent(null); 

        Debug.Log($"WorkspaceController: Confirmed and added BlockModel {ublocklyBlock.ID} to UBlockly Workspace TopBlocks.");

        if (IsReadOnly() || m_WorkspaceModel == null || potentialBlock == null)
        {
            Debug.LogWarning($"ConfirmAddBlock aborted. ReadOnly:{IsReadOnly()} WSModelNull:{m_WorkspaceModel == null} BlockNull:{potentialBlock == null}.");
            return null;
        }

        BlockView potentialBlockView = m_WorkspaceView?.GetBlockView(potentialBlock);
        if (potentialBlockView != null)
        {
            Debug.Log($"  Found BlockView for confirmed block. Forcing OnXYUpdated...");
            potentialBlockView.OnXYUpdated(); // Fuerza la sincronizacion Location<->View y adicion a DB si es Visible/!Hidden/!Connecte
        }
        else
        {
            Debug.LogError($"  BlockView not found for confirmed block {potentialBlock.ID}. Cannot force OnXYUpdated!"); // Si falla aquí, la vista puede que no se haya creado o vinculado correctamente
        }

        Debug.Log($"WorkspaceController: Confirmed block {potentialBlock.ID} ({potentialBlock.Type}). Final checks:");
        Debug.Log($"  Is in BlockDB: {m_WorkspaceModel.BlockDB.ContainsKey(potentialBlock.ID)}");
        Debug.Log($"  Is in TopBlocks: {m_WorkspaceModel.TopBlocks.Contains(potentialBlock)}");

        return ublocklyBlock; 
    }


    public bool RequestBlockUnplug(BlockModel blockToUnplug, bool healStack) 
    {
        if (IsReadOnly() || blockToUnplug == null || m_WorkspaceModel == null) return false;

        blockToUnplug.UnPlug(healStack);


        Debug.Log($"WorkspaceController: Requested Unplug BlockModel {blockToUnplug.ID}.");
        return true; 
    }


    public void RequestBlockMove(BlockModel block, Vector2 newLogicalPosition) 
    {
        if (IsReadOnly() || block == null || m_WorkspaceModel == null || !block.Movable) return;

        block.XY = newLogicalPosition;

        Debug.Log($"WorkspaceController: BlockModel {block.ID} model XY updated to {newLogicalPosition}. Relying on View updates for ConnectionDB.");
    }

    public void RequestDeleteBlock(BlockModel block) 
    {
        if (IsReadOnly() || block == null || m_WorkspaceModel == null || !block.Deletable) return;

        block.Dispose(false); 

        Debug.Log($"WorkspaceController: Requested deletion of BlockModel {block.ID}.");
    }
    public bool RequestConnection(ConnectionModel connection1, ConnectionModel connection2) 
    {
        if (IsReadOnly() || connection1 == null || connection2 == null || m_WorkspaceModel == null) return false;

        try
        {
            Debug.Log($"WorkspaceController: Requesting connect {connection1} <-> {connection2}");
            connection1.Connect(connection2); 
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"WorkspaceController: Connection failed - {e.Message}");
            return false;
        }
    }
    public bool RequestFieldSetValue(FieldModel fieldModel, string newValue) 
    {
        if (IsReadOnly() || fieldModel == null || m_WorkspaceModel == null) return false;
        if (fieldModel.SourceBlock != null && !fieldModel.SourceBlock.Editable)
        {
            Debug.LogWarning("FieldSetValue rejected: BlockModel is not editable.");
            return false;
        }

        fieldModel.SetValue(newValue); 

       
        Debug.Log($"WorkspaceController: Field '{fieldModel.Name}' value set request processed by UBlockly model.");

             return true;
    }

     public bool RequestFieldVariableChange(FieldVariableModel fieldModel, string newVariableName) 
    {
        if (IsReadOnly() || fieldModel == null || m_WorkspaceModel == null) return false;
        if (fieldModel.SourceBlock != null && !fieldModel.SourceBlock.Editable) return false;
      fieldModel.SetValue(newVariableName); 

        Debug.Log($"WorkspaceController: FieldVariable '{fieldModel.Name}' variable name set request processed for '{newVariableName}'.");
        return true;
    }

      public BlockModel RequestCloneBlockBegin(BlockModel templateModelSource, Vector2 initialPosition) 
    {
        if (IsReadOnly() || templateModelSource == null || m_WorkspaceModel == null) return null;

       // Debug.Log($"WorkspaceController: Requesting Clone of {templateModelSource.Type}");

        //BlockModel clonedModel = templateModelSource.Clone();
        BlockModel clonedModel = BlockFactory.Instance.CreateBlock(m_WorkspaceModel, templateModelSource.Type, Utilidades.GenUid());
        clonedModel.XY = initialPosition; 

        if (clonedModel != null)
        {
            m_WorkspaceModel.RemoveTopBlock(clonedModel);
       BlockDragController.Instance?.RegisterPendingClone(clonedModel); 
          //  Debug.Log($"WorkspaceController: Created Pending Clone {clonedModel.ID}");
        }
        return clonedModel;
    }


    public void RegisterClonedBlock(BlockModel block) 
    {
        if (block == null || m_WorkspaceModel == null || IsReadOnly()) return;

    
        if (block.ParentBlock == null && !m_WorkspaceModel.TopBlocks.Contains(block))
        {
            Debug.Log($"Registering previously pending clone {block.ID} that was dropped loose.");
            block.SetParent(null); 
        }
    }


    public void RequestLoadWorkspace()
    {
    
        Debug.LogWarning("RequestLoadWorkspace called directly. UI for loading needed.");
        string savedXml = PlayerPrefs.GetString("LastWorkspace_UBlockly", "");
        if (!string.IsNullOrEmpty(savedXml))
        {
            RequestLoadWorkspaceFromData(savedXml);
        }
    }

    public void RequestLoadWorkspaceFromData(string xmlData)
    {
        if (IsReadOnly()) { Debug.LogWarning("Workspace is read-only, load cancelled."); return; }
        if (string.IsNullOrEmpty(xmlData) || m_WorkspaceModel == null) return;
        if (m_WorkspaceModel == null) { Debug.LogError("Load cancelled: Workspace Model is not initialized."); return; }
        if (m_WorkspaceView == null) { Debug.LogError("Load cancelled: Workspace View is not initialized."); return; }


        Debug.Log($"WorkspaceController: Loading from provided XML data (length: {xmlData.Length})...");
        try
        {
        
            m_WorkspaceModel.Clear();
            Debug.Log("Workspace model cleared.");

            m_WorkspaceView.CleanViews();
            Debug.Log("Workspace views cleaned.");


            var xmlDoc = Xml.TextToDom(xmlData);
            List<string> newBlockIds = Xml.DomToWorkspace(xmlDoc, m_WorkspaceModel);
            Debug.Log($"Loaded {newBlockIds?.Count ?? 0} top-level blocks into model {m_WorkspaceModel.Id}.");

            m_WorkspaceView.BuildViews();
            Debug.Log("Workspace views rebuilt from loaded model.");


            m_WorkspaceModel.UpdateProcedureDB();
            m_WorkspaceModel.UpdateVariableStore(true); 

            Debug.Log("<color=green>Workspace loaded successfully from data.</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"WorkspaceController: Error during LoadWorkspaceFromData: {ex.Message}\n{ex.StackTrace}");
                m_WorkspaceModel?.Clear();
            m_WorkspaceView?.CleanViews();
        }
    }

    public void RequestSaveWorkspace()
    {
        if (m_WorkspaceModel == null) { Debug.LogError("Cannot save, WorkspaceModel is null."); return; }
        // if (IsReadOnly()) { Debug.LogWarning("Workspace is read-only, save cancelled."); return; }

        Debug.Log("WorkspaceController: Requesting Save Workspace...");
        try
        {
            var workspaceXml = Xml.WorkspaceToDom(m_WorkspaceModel);
            string xmlData = Xml.DomToText(workspaceXml);

            PlayerPrefs.SetString("LastWorkspace_UBlockly", xmlData);
            PlayerPrefs.Save();
            Debug.Log($"Workspace saved successfully to PlayerPrefs.\n{xmlData}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"WorkspaceController: Error during SaveWorkspace: {ex.Message}\n{ex.StackTrace}");
        }
    }

    #endregion

    #region degugging conexiones

    public void DebugLogConnectionDBs()
    {
        if (m_WorkspaceModel?.ConnectionDBList == null)
        {
            Debug.LogWarning("Connection DBs are not initialized in WorkspaceModel.");
            return;
        }

        Debug.Log("--- Connection Databases State ---");
        
        // WorkSpaceView currentView = WorkSpaceView.Active;

        foreach (var kvp in m_WorkspaceModel.ConnectionDBList)
        {
            EConnection type = kvp.Key;
            BlockConnectionDB db = kvp.Value;
            Debug.Log($"DB Type: {type} ({db.Count} connections)");

            for (int i = 0; i < db.Count; i++)
            {
                ConnectionModel conn = db[i];
                string sourceBlockId = conn.SourceBlock?.ID ?? "NULL_BLOCK";
                string sourceBlockType = conn.SourceBlock?.Type ?? "NULL_TYPE";
                string targetConnId = GetConnectionModelID(conn.TargetConnection);
                string targetBlockType = conn.TargetBlock?.Type ?? "NULL_TARGET_TYPE";

                // Info del modelo Connection
                string checksInfo = (conn.Check == null || conn.Check.Count == 0) ? "ANY" : string.Join(", ", conn.Check);

                // Información visual 
                Vector3 visualWorldPos = Vector3.negativeInfinity;
                BlockView sourceBlockView = m_WorkspaceView?.GetBlockView(conn.SourceBlock);
                if (sourceBlockView != null)
                {
                    ConnectionView connView = sourceBlockView.FindConnectionView(conn);
                    if (connView != null)
                    {
                        visualWorldPos = connView.ViewTransform.position; // Posición World (Unity UI)
                    }
                    else
                    {
                        // Debug.LogWarning($"Debug: No ConnectionView found for Model Type {conn.Type} on block {sourceBlockType} ({sourceBlockId}). View mismatch?");
                    }
                }


                Debug.Log($"  [{i}] ConnModel ID:{GetConnectionModelID(conn)}, Type:{conn.Type}, IsSuperior:{conn.IsSuperior}, " +
                          $"ModelLoc:({conn.Location.x:F2}, {conn.Location.y:F2}), InDB:{conn.InDB}, Hidden:{conn.Hidden}, " +
                            (conn.IsConnected ? // Solo muestro detalles si está conectado
                            $"to Conn:{GetConnectionModelID(conn.TargetConnection)} Type:{conn.TargetConnection.Type} on Block:{conn.TargetBlock.Type} ({conn.TargetBlock.ID})" 
                             : "Not Connected" ) +
                             $"SourceBlock:({sourceBlockType}:{sourceBlockId}), Check:[{checksInfo}]");

                if (visualWorldPos != Vector3.negativeInfinity)
                {
                    Debug.Log($"     -> Visual World Pos:({visualWorldPos.x:F2}, {visualWorldPos.y:F2}) (via {sourceBlockView.name})");
                }
                else if (sourceBlockView != null)
                {
                   
                }
                else
                {
                    // Debug.LogWarning($"Debug: Source BlockView not found for model {sourceBlockType} ({sourceBlockId}). No visual info available.");
                }

                // Depuración extra si el bloque está siendo arrastrado
                bool isDraggingBlock = (BlockDragController.Instance != null && sourceBlockView != null && BlockDragController.Instance.IsDraggingBlock(conn.SourceBlock));
                if (isDraggingBlock && sourceBlockView.ViewTransform != null)
                {
                    Debug.Log($"     ^^^ Source block IS being dragged. View Anchored Pos: ({sourceBlockView.ViewTransform.anchoredPosition.x:F2}, {sourceBlockView.ViewTransform.anchoredPosition.y:F2}) in Parent: {sourceBlockView.ViewTransform.parent?.name}");
                }
            }
            Debug.Log($"--- End DB Type: {type} ---");
        }
        Debug.Log("--- End Connection Databases State ---");
    }

    // Identifica IDs de conexión si los ConnectionModel no tienen ID propio
    private string GetConnectionModelID(ConnectionModel conn)
    {
        if (conn == null) return "NULL_CONN";
       
        string sourceId = conn.SourceBlock?.ID ?? "SOURCE_BLOCK_NULL";


        if (conn.Type == EConnection.InputValue || conn.Type == EConnection.NextStatement)
        {
          
            string inputName = conn.Input?.Name ?? "INPUT_OR_NAME_NULL";

           
            if (conn.Input == null)
            {
                Debug.LogWarning($"GetConnectionModelID: Connection (Source: {sourceId}, Type: {conn.Type}) has NULL Input field!");
            }
            

            return $"{sourceId}.{conn.Type}.{inputName}"; 
        }
        else
        {
            return $"{sourceId}.{conn.Type}";
        }
    }

    // Método para obtener datos serializables de una ConnectionModel (helper)
    private DebugConnectionData GetDebugConnectionData(ConnectionModel conn)
    {
        if (conn == null) return null;

        return new DebugConnectionData
        {
            ConnectionModelId = GetConnectionModelID(conn), // Usamos el helper ID del logging
            Type = conn.Type.ToString(),
            IsSuperior = conn.IsSuperior,
            LocationX = conn.Location.x,
            LocationY = conn.Location.y,
            InDB = conn.InDB,
            Hidden = conn.Hidden,
            IsConnected = conn.IsConnected,
            TargetConnectionId = GetConnectionModelID(conn.TargetConnection), // ID del target
            SourceBlockId = conn.SourceBlock?.ID,
            SourceBlockType = conn.SourceBlock?.Type,
            Checks = conn.Check?.ToArray() // Guardar el array de checks
        };
    }

    // Método para obtener datos serializables de un BlockModel 
    private DebugBlockData GetDebugBlockData(BlockModel block)
    {
        if (block == null) return null;
        return new DebugBlockData
        {
            BlockId = block.ID,
            Type = block.Type,
            XY_X = block.XY.x,
            XY_Y = block.XY.y,
          
        };
    }

    // Método público para exportar el estado de las DBs a un archivo JSON
    public void ExportConnectionDBState(string filename = "ConnectionDB_Debug_State")
    {
        //Debug.LogError($"HASHCODE_CHECK - ExportConnectionDBState - Using WorkspaceModel Instance HashCode: {m_WorkspaceModel?.GetHashCode()}");

        if (m_WorkspaceModel?.ConnectionDBList == null)
        {
            Debug.LogWarning("Connection DBs are not initialized. Cannot export.");
            return;
        }

        DebugConnectionDBsState debugState = new DebugConnectionDBsState();

        Debug.Log("<color=grey>ExportConnectionDBState: Preparing export...</color>");

        // Convierto la BlockConnectionDB a un array de DebugConnectionData
        Func<BlockConnectionDB, DebugConnectionData[]> convertDBToArray = (db) =>
        {
            if (db == null) return new DebugConnectionData[0];
            DebugConnectionData[] array = new DebugConnectionData[db.Count];
            for (int i = 0; i < db.Count; i++)
            {
                array[i] = GetDebugConnectionData(db[i]);
            }
            return array;
        };

        // Lleno las listas de conexiones por tipo de DB
        Debug.Log($" - InputValuesDB Count (in Model): {m_WorkspaceModel.ConnectionDBList.GetValueOrDefault(EConnection.InputValue)?.Count ?? 0}");

        debugState.InputValuesDB = convertDBToArray(m_WorkspaceModel.ConnectionDBList.ContainsKey(EConnection.InputValue) ? m_WorkspaceModel.ConnectionDBList[EConnection.InputValue] : null);

        Debug.Log($" - OutputValuesDB Count (in Model): {m_WorkspaceModel.ConnectionDBList.GetValueOrDefault(EConnection.OutputValue)?.Count ?? 0}");

        debugState.OutputValuesDB = convertDBToArray(m_WorkspaceModel.ConnectionDBList.ContainsKey(EConnection.OutputValue) ? m_WorkspaceModel.ConnectionDBList[EConnection.OutputValue] : null);

        Debug.Log($" - NextStatementsDB Count (in Model): {m_WorkspaceModel.ConnectionDBList.GetValueOrDefault(EConnection.NextStatement)?.Count ?? 0}");

        debugState.NextStatementsDB = convertDBToArray(m_WorkspaceModel.ConnectionDBList.ContainsKey(EConnection.NextStatement) ? m_WorkspaceModel.ConnectionDBList[EConnection.NextStatement] : null);
       
        Debug.Log($" - PrevStatementsDB Count (in Model): {m_WorkspaceModel.ConnectionDBList.GetValueOrDefault(EConnection.PrevStatement)?.Count ?? 0}");

        debugState.PrevStatementsDB = convertDBToArray(m_WorkspaceModel.ConnectionDBList.ContainsKey(EConnection.PrevStatement) ? m_WorkspaceModel.ConnectionDBList[EConnection.PrevStatement] : null);

        //Guardo también todos los BlockModels
        var allBlocks = m_WorkspaceModel.GetAllBlocks();

        Debug.Log($" - Found {allBlocks.Count} blocks via m_WorkspaceModel.GetAllBlocks()");

        if (allBlocks.Count > 0)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("   - Blocks found for export:");
            foreach (var b in allBlocks) { sb.Append($" {b.ID}({b.Type}),"); }
            Debug.Log(sb.ToString());
        }

        debugState.AllBlocks = new DebugBlockData[allBlocks.Count];
        for (int i = 0; i < allBlocks.Count; i++)
        {
            debugState.AllBlocks[i] = GetDebugBlockData(allBlocks[i]);
        }

        string jsonData = JsonConvert.SerializeObject(debugState, Formatting.Indented);

        // Guardo el string JSON en un archivo
        string path = System.IO.Path.Combine(Application.persistentDataPath, filename + ".json");
        try
        {
            System.IO.File.WriteAllText(path, jsonData);
            Debug.Log($"Successfully exported Connection DB state to: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to write debug file to {path}: {e.Message}");
        }
    }

    #endregion

}//fin WorkSpaceController