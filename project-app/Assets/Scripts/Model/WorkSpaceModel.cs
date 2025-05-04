/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 27/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:  Es el núcleo del modelo de programación visual y administra todos los datos relacionados con
 * bloques, variables y funciones o procedimientos del entorno.
 */

using System.Collections.Generic;
using UnityEngine; 
using System;
using Newtonsoft.Json.Linq;

public class WorkSpaceModel
{
    
    public string Id;

    public class WorkspaceOptions
    {
        public int MaxBlocks = -1;
        public bool ReadOnly = false;
        public bool Synchronous = false;
    }

    public WorkspaceOptions Options { get; private set; }
    public List<BlockModel> TopBlocks { get; private set; }
    public Dictionary<string, BlockModel> BlockDB { get; private set; }
    public VariableMap VariableMap { get; private set; }
    public Dictionary<EConnection, BlockConnectionDB> ConnectionDBList { get; private set; }
    public ProcedureDB ProcedureDB { get; private set; }
    private const int MAX_UNDO = 1024;
    private const int SCAN_ANGLE = 3;

    private static WorkSpaceModel _instance;
    public static WorkSpaceModel Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("WorkSpaceModel INSTANCE WAS NULL! Creating new default one.");
                _instance = new WorkSpaceModel(); 
            }
            return _instance;
        }
    }

    public WorkSpaceModel(WorkspaceOptions options = null, string optId = null)
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogError("!!! Singleton Pattern Violated! Creating a second WorkSpaceModel instance. !!!");
        }

        if (_instance == null) _instance = this; // Asigno la primera instancia creada

        if (string.IsNullOrEmpty(optId))
        {
            Id = Utilidades.GenUid();
        }
        else
        {
            Id = optId;
        }

        //Debug.LogError($"HASHCODE_CHECK - WorkSpaceModel CONSTRUCTOR - ID: {Id} - Instance HashCode: {this.GetHashCode()}");

        if (mWorkspaceDB.ContainsKey(Id))
        {
            mWorkspaceDB[Id] = this;
            Debug.LogWarning("Already contains workspace id:" + Id);
        }
        else
        {
            mWorkspaceDB.Add(Id, this);
        }

        Options = options ?? new WorkspaceOptions();

        TopBlocks = new List<BlockModel>();
        BlockDB = new Dictionary<string, BlockModel>();
        VariableMap = new VariableMap(this);
        ConnectionDBList = BlockConnectionDB.Build();
        ProcedureDB = new ProcedureDB(this);

       // Debug.LogError($"HASHCODE_CHECK - WorkSpaceModel CONSTRUCTOR - ID: {Id} - Instance HashCode: {this.GetHashCode()}");

    }

    public static void EnsureInitialized(WorkspaceOptions options = null, string optId = null)
    {
        if (_instance == null)
        {
            _instance = new WorkSpaceModel(options, optId);
        }
    }
    public void Dispose()
    {
        Debug.LogError("!!!!!!!! WorkSpaceModel.Dispose() CALLED !!!!!!!!");

        this.Clear();
        mWorkspaceDB.Remove(this.Id);
    }

    public void Clear()
    {
        Debug.LogError("!!!!!!!! WorkSpaceModel.Clear() CALLED !!!!!!!!");
        Debug.LogError("Stack Trace:\n" + Environment.StackTrace);
        while (TopBlocks.Count > 0)
        {
            TopBlocks[TopBlocks.Count - 1].Dispose();
        }

        VariableMap.Clear();
        ConnectionDBList.Clear();
        Debug.LogError("!!!!!!!! dentro de WorkSpaceModel.Clear() ->ConnectionDBList.Clear(); CALLED !!!!!!!!");
        ProcedureDB.Clear();
    }

    #region Blocks
    public BlockModel NewBlock(string prototypeName, string opt_id = null)
    {
        return BlockFactory.Instance.CreateBlock(this, prototypeName, opt_id);
    }
    public BlockModel GetBlockById(string id)
    {
        BlockModel block = null;
        BlockDB.TryGetValue(id, out block);
        return block;
    }
    public void AddTopBlock(BlockModel block)
    {
        if (!TopBlocks.Contains(block))
            TopBlocks.Add(block);

        if (ProcedureDB.IsDefinition(block)) ProcedureDB.AddDefinition(block);
        else if (ProcedureDB.IsCaller(block)) ProcedureDB.AddCaller(block);
    }

    public void RemoveTopBlock(BlockModel block)
    {
        TopBlocks.Remove(block);

        if (ProcedureDB.IsDefinition(block)) ProcedureDB.RemoveDefinition(block);
        else if (ProcedureDB.IsCaller(block)) ProcedureDB.RemoveCaller(block);
    }

    public List<BlockModel> GetTopBlocks(bool ordered)
    {
        var blocks = new List<BlockModel>();
        blocks.AddRange(TopBlocks);
        if (ordered && blocks.Count > 1)
        {
            var offset = Math.Sin(WorkSpaceModel.SCAN_ANGLE * Mathf.Deg2Rad);

            blocks.Sort(delegate (BlockModel a, BlockModel b)
            {
                var aXY = a.XY;
                var bXY = b.XY;
                return (int)((aXY.y + offset * aXY.x) - (bXY.y + offset * bXY.x));
            });
        }
        return blocks;
    }

    public List<BlockModel> GetAllBlocks()
    {
       /* var topBlocks = GetTopBlocks(false);
        List<BlockModel> blocks = new List<BlockModel>();
        foreach (BlockModel topBlock in topBlocks)
        {
            blocks.AddRange(topBlock.GetDescendants());
        }*/
        Debug.Log($"GetAllBlocks(): Returning {BlockDB.Count} blocks directly from BlockDB.Values.");
        return new List<BlockModel>(BlockDB.Values);
    }

    #endregion

    #region Variables
    public VariableModel CreateVariable(string name, string optType = null, string optId = null)
    {
        return this.VariableMap.CreateVariable(name, optType, optId);
    }
    public bool HasVariable(string name)
    {
        return GetVariable(name) != null;
    }

    public VariableModel GetVariable(string name)
    {
        return VariableMap.GetVariable(name);
    }
    public VariableModel GetVariableById(string id)
    {
        return VariableMap.GetVariableById(id);
    }
 
    public List<VariableModel> GetVariablesOfType(string type)
    {
        return this.VariableMap.GetVariablesOfType(type);
    }
    public List<string> GetVariableTypes()
    {
        return VariableMap.GetVariableTypes();
    }

    public List<VariableModel> GetAllVariables()
    {
        return VariableMap.GetAllVariables();
    }
   
    public List<BlockModel> GetVariableUses(string name)
    {
        var uses = new List<BlockModel>();
        var blocks = this.GetAllBlocks();
        foreach (var block in blocks)
        {
            var blockVariables = block.GetVars();
            if (null != blockVariables && blockVariables.Count != 0)
            {
                foreach (var varName in blockVariables)
                {
                    if (null != varName && null != name && Names.Equals(varName, name))
                    {
                        uses.Add(block);
                    }
                }
            }
        }
        return uses;
    }

    public void DeleteVariable(string name)
    {
        var uses = this.GetVariableUses(name);
        foreach (var block in uses)
        {
            if (string.Equals(block.Type, Define.DEFINE_NO_RETURN_BLOCK_TYPE) ||
                string.Equals(block.Type, Define.DEFINE_WITH_RETURN_BLOCK_TYPE))
            {
                var procedureName = block.GetFieldValue("NAME");
                Debug.LogError("Alert:" + I18n.Get(MsgDefine.CANNOT_DELETE_VARIABLE_PROCEDURE).
                                   Replace("%1", name).
                                   Replace("%2", procedureName));
                return;
            }
        }

        var workspace = this;
        var variable = workspace.GetVariable(name);
        if (uses.Count > 1)
        {
            Debug.Log("confirm:" + I18n.Get(MsgDefine.DELETE_VARIABLE_CONFIRMATION)
                          .Replace("%1", uses.Count.ToString()).Replace("%2", name));
            workspace.DeleteVariableInternal(variable);
        }
        else
        {
            this.DeleteVariableInternal(variable);
        }
    }
    public void DeleteVariableById(string id)
    {
        var variable = this.GetVariableById(id);
        if (null != variable)
        {
            this.DeleteVariableInternal(variable);
        }
        else
        {
            Debug.LogError("Can't delete non-existant variable: " + id);
        }
    }

    public void DeleteVariableInternal(VariableModel variable)
    {
        var uses = GetVariableUses(variable.Name);
        foreach (var block in uses)
        {
            block.Dispose(true);
        }
        VariableMap.DeleteVariable(variable);
    }
    public void RenameVariableInternal(VariableModel variable, string newName)
    {
        var newVariable = this.GetVariable(newName);

        if (null != variable && null != newVariable && !string.Equals(variable.Type, newVariable.Type))
        {
            throw new Exception("Variable " + variable.Name + " is type " + variable.Type +
                                " and variable " + newName + " is type " + newVariable.Type +
                                ".Both must be the same type.");
        }

        string oldName = variable != null ? variable.Name : null;
        string oldCase = newVariable != null ? newVariable.Name : null;

        this.VariableMap.RenameVariable(variable, newName);

        var blocks = this.GetAllBlocks();
        foreach (var block in blocks)
        {
            block.RenameVar(oldName, newName);
            if (!string.IsNullOrEmpty(oldCase) && !oldCase.Equals(newName))
            {
                block.RenameVar(oldCase, newName);
            }
        }
    }

    public void RenameVariable(string oldName, string newName)
    {
        var variable = this.GetVariable(oldName);
        this.RenameVariableInternal(variable, newName);
    }

    public void RenameVariableById(string id, string newName)
    {
        var variable = this.GetVariableById(id);
        this.RenameVariableInternal(variable, newName);
    }
    public void UpdateVariableStore(bool clear = false, List<string> unitTestAllUsedVariable = null)
    {
        var variableNames = unitTestAllUsedVariable == null ? VariableUtils.GetAllUsedVariableNames(this) : unitTestAllUsedVariable;
        var varList = new List<JObject>();
        foreach (var name in variableNames)
        {
            var tempVar = GetVariable(name);
            if (null != tempVar)
            {
                JObject jsonData = new JObject();
                jsonData["name"] = tempVar.Name;
                jsonData["type"] = tempVar.Type;
                jsonData["id"] = tempVar.ID;
                varList.Add(jsonData);
            }
            else
            {
                JObject jsonData = new JObject();
                jsonData["name"] = name;
                jsonData["type"] = string.Empty;
                jsonData["id"] = string.Empty;
                varList.Add(jsonData);
            }
        }

        if (clear) VariableMap.Clear();

        foreach (var varDict in varList)
        {
            if (null == this.GetVariable(varDict["name"].ToString()))
            {
                this.CreateVariable(varDict["name"].ToString(), varDict["type"].ToString(), varDict["id"].ToString());
            }
        }
    }

    #endregion

    public void UpdateProcedureDB()
    {
        var allBlocks = GetAllBlocks();
        List<BlockModel> procedureDefs = new List<BlockModel>();
        List<BlockModel> procedureCalls = new List<BlockModel>();
        foreach (var block in allBlocks)
        {
            if (ProcedureDB.IsDefinition(block))
                procedureDefs.Add(block);
            else if (ProcedureDB.IsCaller(block))
                procedureCalls.Add(block);
        }
        foreach (BlockModel block in procedureDefs)
        {
            ProcedureDB.AddDefinition(block);
        }
        foreach (BlockModel block in procedureCalls)
        {
            ProcedureDB.AddCaller(block);
        }
    }

    static Dictionary<string, WorkSpaceModel> mWorkspaceDB = new Dictionary<string, WorkSpaceModel>();
    public static WorkSpaceModel GetByID(string id)
    {
        WorkSpaceModel workspace = null;
        mWorkspaceDB.TryGetValue(id, out workspace);
        return workspace;
    }

    public BlockConnectionDB GetConnectionDB(EConnection type)
    {
        if (ConnectionDBList != null && ConnectionDBList.TryGetValue(type, out BlockConnectionDB db))
        {
            return db;
        }
        else
        {
            Debug.LogError($"ConnectionDB for type {type} not found in Workspace {Id}. Make sure Build() was called correctly.");
            return null;
        }
    }

    public virtual void ShowRenameVariablePrompt(VariableModel variable)
    {
          Debug.Log($"Workspace: Showing rename prompt for variable '{variable.Name}'...");
        string newName = PlayerPrefs.GetString("TMP_RENAME_VAR", variable.Name + "_new"); 
        if (newName != variable.Name)
        {
            try
            {
                this.RenameVariableById(variable.ID, newName);
                Debug.Log($"Variable renamed to {newName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Rename failed: {e.Message}");
            }
        }
        else
        {
            Debug.Log("Rename cancelled or name unchanged.");
        }

    }

    public virtual void ShowDeleteVariablePrompt(VariableModel variable)
    {
           Debug.Log($"Workspace: Showing delete confirmation for variable '{variable.Name}'...");
        bool confirmDelete = true; 
        if (confirmDelete)
        {
            this.DeleteVariableById(variable.ID); 
            Debug.Log($"Variable '{variable.Name}' deleted.");
        }
        else
        {
            Debug.Log("Delete cancelled.");
        }
    }

    public void AddBlock(BlockModel block)
    {
       // Debug.Log($"<color=magenta>WorkspaceModel.AddBlock ENTERED.</color> Block: {block?.Type} (ID: {block?.ID})");

      //  Debug.LogError($"HASHCODE_CHECK - WorkspaceModel AddBlock - Instance HashCode: {this.GetHashCode()} - Adding Block ID: {block?.ID}");

        if (block == null)
        {
            Debug.LogError("WorkSpaceModel.AddBlock: Attempted to add a null block.");
            return;
        }

        if (!BlockDB.ContainsKey(block.ID))
        {
            Debug.LogWarning($"WorkSpaceModel.AddBlock: Block '{block.Type}' (ID: {block.ID}) was not in BlockDB. Adding it now. BlockFactory should ideally handle this upon creation.");
            BlockDB.Add(block.ID, block);
        }
        else
        {
           // Debug.Log($" - Block ID {block.ID} already exists in BlockDB.");
        }

        bool isTopBlock = block.ParentBlock == null;


        if (isTopBlock)
        {
            if (!TopBlocks.Contains(block)) // Solo añade si no está ya
            {
                Debug.Log($" - Adding Block ID {block.ID} to TopBlocks list.");
                AddTopBlock(block); 
            }
            else
            {
                Debug.Log($" - Block ID {block.ID} is already in TopBlocks list.");
            }
        }
        else // No es top block
        {
            if (TopBlocks.Contains(block))
            {
                Debug.LogWarning($" - Block ID {block.ID} is NOT a TopBlock (has parent) but was found in TopBlocks list! Removing.");
                RemoveTopBlock(block); 
            }
            else
            {
                Debug.Log($" - Block ID {block.ID} is not a TopBlock (Parent: {block.ParentBlock?.ID}). Not added to TopBlocks list.");
            }
        }

        List<ConnectionModel> connectionsToRegister = block.GetConnections();

        foreach (ConnectionModel conn in connectionsToRegister)
        {
            if (conn == null || conn.SourceBlock != block) continue;

            if (conn.DB != null)
            {
                if (!conn.DB.Contains(conn)) 
                {
                    Debug.Log($"<color=lime>   - Registering Conn LIST: {ConnectionModel.GetConnectionModelID(conn)} into DB List.</color>");
                    conn.DB.AddConnection(conn); 
                }
                else
                {
                    Debug.LogWarning($"[AddBlock REGISTERING LISTS] Conn {ConnectionModel.GetConnectionModelID(conn)} already in DB list. Ensuring InDB=true.");
                    if (!conn.InDB) conn.InDB = true; 
                }
            }
            else 
            {
                if (conn.InDB) conn.InDB = false;
            }
        }
       // Debug.Log($"<color=yellow> - [AddBlock REGISTERING LISTS] Finished processing connections for Block ID: {block.ID}</color>");
        //Debug.Log($"<color=magenta>WorkspaceModel.AddBlock EXITED.</color> Block ID: {block.ID}. BlockDB count: {BlockDB.Count}, TopBlocks count: {TopBlocks.Count}");
    }
}//fin clase WorkSpaceModel