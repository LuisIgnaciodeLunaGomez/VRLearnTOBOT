/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Clase que se encarga de la creación de los bloques para cada categoría
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;


/***************************
 hierarchy of block:
 - BlockModel(Topmost in workspace)
   - ConnectionOutput
   - ConnectionPrev  
   - ConnectionNext
     - BlockModel(Next)

   - Input
     - Field 
     - Field 
     ...
     - ConnectionInput
       - BlockModel(Input)
   - Input
     ...

 - BlockModel
   ...             
***************************/

/// <summary>
/// Block core model class
/// inherit from Observable, where int is the UpdateState mask
/// </summary>
public class BlockModel : Observable<int>
{
   
    public string Type { get; protected set; }
    public string ID { get; protected set; }
    public WorkSpaceModel Workspace { get; set; }
    public ConnectionModel OutputConnection { get; set; }
    public ConnectionModel NextConnection { get; set; }
    public ConnectionModel PreviousConnection { get; set; }
    public List<InputModel> InputList { get; protected set; }
    public Mutator Mutator { get; protected set; }

    public BlockModel ParentBlock { get; protected set; }
    public List<BlockModel> ChildBlocks = new List<BlockModel>();

    /// <summary>
    /// The block's position in workspace units.  (0, 0) is at the workspace's origin; scale does not change this value.
    /// </summary>
    public Vector2 XY { get; set; }

    public string Data = null;
    public BlockModel() 
    {
        InputList = new List<InputModel>(); 
    }

    /// <summary>
    /// Class for one block.
    /// Not normally called directly,workspace.newBlock() is preferred.
    /// </summary>
    /// <param name="workspace"> The BlockModel's workspace</param>
    /// <param name="prototypeName"> Name of the language object containing
    /// type-specific functions for this block. </param>
    /// <param name="opt_id">Use this ID if provided,otherwise create a new id</param>
    public BlockModel(WorkSpaceModel workspace, string prototypeName = null, string opt_id = null)
    {
        if (workspace == null) 
        {
            throw new ArgumentNullException(nameof(workspace), "Workspace cannot be null when creating a non-template block.");
        }

        Type = prototypeName;
        ID = !string.IsNullOrEmpty(opt_id) && workspace.GetBlockById(opt_id) == null
              ? opt_id
              : Utilidades.GenUid();

        workspace.BlockDB.Add(ID, this);
        Workspace = workspace;

        OutputConnection = null;
        NextConnection = null;
        PreviousConnection = null;
        InputList = new List<InputModel>();

        workspace.AddTopBlock(this);
    }

    private BlockModel(string prototypeName, string uid) 
    {
        Type = prototypeName;
        ID = uid; 
        Workspace = null; 
        InputList = new List<InputModel>();
    }

    /// <summary>
    /// Clone a block from this block
    /// </summary>
    public BlockModel Clone()
    {
        XmlNode xmlNode = Xml.BlockToDomWithXY(this, true);
        BlockModel newBlock = Xml.DomToBlock(xmlNode, Workspace);
        Workspace.AddTopBlock(newBlock);
        return newBlock;
    }
    public void Dispose(bool healStack = false)
    {
        if (null == this.Workspace)
        {
            Debug.LogWarning($"[Block Dispose {this.ID}] Dispose called but Workspace is already null. Skipping.");

            return;
        }

        //Para saber quien llama a Dispose
        try { Debug.LogError($"BLOCK DISPOSE ENTERED for Block ID: {this.ID}. HealStack={healStack}\n" + Environment.StackTrace); } catch { }

        UnPlug(healStack);

        Workspace.RemoveTopBlock(this);
        if (Workspace != null && Workspace.BlockDB != null && !string.IsNullOrEmpty(this.ID)) 
        {
            bool removedFromDB = Workspace.BlockDB.Remove(this.ID);
            Debug.Log($"[Block Dispose {this.ID}] Removed from Workspace.BlockDB? {removedFromDB}");
        }
        Workspace = null;

       
        if (this.ChildBlocks != null)
        {
            Debug.Log($"[Block Dispose {this.ID}] Disposing {this.ChildBlocks.Count} child blocks...");
            // Itero desde el final - es más seguro si la lista se modifica durante la iteración
            for (int i = this.ChildBlocks.Count - 1; i >= 0; i--)
            {
                BlockModel child = this.ChildBlocks[i]; // Guardar referencia
                if (child != null)
                {
                    // Debug.Log($"  - Disposing Child {i}: {child.ID}");
                    child.Dispose(false); 
                }
                else
                {
                    Debug.LogWarning($"  - Found NULL child block at index {i}!");
                }
            }
        }

        Debug.Log($"[Block Dispose {this.ID}] Disposing InputModels...");
        if (this.InputList != null)
        { // Check si la lista existe
            foreach (var input in this.InputList)
            {
                if (input != null)
                {
                    // Debug.Log($"  - Disposing Input: {input.Name}");
                    input.Dispose(); 
                }
                else
                {
                    Debug.LogWarning("  - Found NULL InputModel in InputList!");
                }
            }
        }

        // Obtengo las conexiones que son propiedad DIRECTA del bloque, NO las de los inputs.
        List<ConnectionModel> directConnections = new List<ConnectionModel>();
        if (this.OutputConnection != null) directConnections.Add(this.OutputConnection);
        if (this.PreviousConnection != null) directConnections.Add(this.PreviousConnection);
        if (this.NextConnection != null) directConnections.Add(this.NextConnection);

        Debug.Log($"[Block Dispose {this.ID}] Disposing {directConnections.Count} DIRECT connections (Output/Prev/Next). Checking state BEFORE dispose:");
        foreach (var c in directConnections)
        {
            if (c != null)
            {
                Debug.Log($"   - Direct Conn: {ConnectionModel.GetConnectionModelID(c)}, Has SourceBlock? {c.SourceBlock != null} (ID: {c.SourceBlock?.ID ?? "NULL"}), InDB? {c.InDB}, DB Ref Null? {c.DB == null}");
            }
            else
            {
                Debug.LogWarning("   - Found a NULL direct connection reference (Output/Prev/Next)!");
            }
        }

        // Disponer las conexiones directas
        foreach (var connection in directConnections)
        {
            if (connection != null) // Siempre seguro hacer null check
            {
                // Debug.Log($"  - Disposing Direct Connection: {ConnectionModel.GetConnectionModelID(connection)}");
                // connection.Disconnect(); 
                connection.Dispose();
            }
        }

    }

    // Sets the mutator for this block.  Called from BlockFractory, and can only be called once (for now).

    public void SetMutator(Mutator mutator)
    {
        if (this.Mutator != null)
            throw new Exception("Cannot change mutators on a block.");
        this.Mutator = mutator;
        mutator.AttachToBlock(this);
    }

    // updates the inputs and all connections with potentially new values,
    // changing the shape of the block. This method should only be called by the constructor, or Mutators.
    
    public void Reshape(List<InputModel> newInputList, ConnectionModel updatedOutput, ConnectionModel updatedPrev, ConnectionModel updatedNext)
    {
        if (updatedOutput != null)
        {
            if (updatedPrev != null)
                throw new Exception("A block cannot have both an output connection and a previous connection.");
            if (updatedOutput.Type != EConnection.OutputValue)
                throw new Exception("updatedOutput Connection type is not OUTPUT_VALUE");
        }
        if (updatedPrev != null && updatedPrev.Type != EConnection.PrevStatement)
        {
            throw new Exception("updatedPrev Connection type is not PREVIOUS_STATEMENT");
        }
        if (updatedNext != null && updatedNext.Type != EConnection.NextStatement)
        {
            throw new Exception("updatedNext Connection type is not CONNECTION_TYPE_NEXT");
        }

        bool updateInputs = false;
        bool updateConnection = false;

        List<InputModel> oldInputs = InputList;
        foreach (InputModel input in oldInputs)
        {
            if (!newInputList.Contains(input))
            {
                input.Dispose();
                updateInputs = true;
            }
        }
        foreach (InputModel input in newInputList)
        {
            if (!oldInputs.Contains(input))
            {
                input.SourceBlock = this;
                updateInputs = true;
            }
        }
        InputList = newInputList;

        updateConnection = OutputConnection != updatedOutput ||
                           PreviousConnection != updatedPrev ||
                           NextConnection != updatedNext;

        if (updatedOutput != null)
            updatedOutput.SourceBlock = this;
        if (OutputConnection != null && OutputConnection != updatedOutput)
        {
            OutputConnection.Disconnect();
            OutputConnection.Dispose();
        }
        OutputConnection = updatedOutput;

        if (updatedPrev != null)
            updatedPrev.SourceBlock = this;
        if (PreviousConnection != null && PreviousConnection != updatedPrev)
        {
            PreviousConnection.Disconnect();
            PreviousConnection.Dispose();
        }
        PreviousConnection = updatedPrev;

        if (updatedNext != null)
            updatedNext.SourceBlock = this;
        if (NextConnection != null && NextConnection != updatedNext)
        {
            NextConnection.Disconnect();
            NextConnection.Dispose();
        }
        NextConnection = updatedNext;

        if (updateInputs && updateConnection) FireUpdate(1 << (int)UpdateStates.Inputs | 1 << (int)UpdateStates.Connections);
        else if (updateInputs) FireUpdate(1 << (int)UpdateStates.Inputs);
        else if (updateConnection) FireUpdate(1 << (int)UpdateStates.Connections);
    }

    /// <summary>
    /// updates the inputs
    /// </summary>
    /// <param name="newInputList"></param>
    public void Reshape(List<InputModel> newInputList)
    {
        Reshape(newInputList, OutputConnection, PreviousConnection, NextConnection);
    }

    /// <summary>
    /// Unplug this block from its superior block.  If this block is a statement,
    /// optionally reconnect the block underneath with the block on top.
    /// </summary>
    /// <param name="optHealStack">Disconnect child statement and reconnect stack</param>
    public void UnPlug(bool optHealStack = false)
    {
        if (this.OutputConnection != null)
        {
            if (this.OutputConnection.IsConnected)
                this.OutputConnection.Disconnect();
        }
        else if (this.PreviousConnection != null)
        {
            ConnectionModel previousTarget = null;
            if (this.PreviousConnection.IsConnected)
            {
                previousTarget = PreviousConnection.TargetConnection;
                PreviousConnection.Disconnect();
            }
            BlockModel nextBlock = this.NextBlock;
            if (optHealStack && nextBlock != null)
            {
                var nextTarget = this.NextConnection.TargetConnection;
                nextTarget.Disconnect();
                if (previousTarget != null && previousTarget.CheckType(nextTarget))
                {
                    previousTarget.Connect(nextTarget);
                }
            }
        }

        Debug.LogWarning($"[Unplug Block:{ID}] START. Prev Conn Source Before: {this.PreviousConnection?.SourceBlock?.ID ?? "NULL"}");
        if (this.ParentBlock != null) { /* ... desconectar de padre ... */ }
        if (optHealStack && this.PreviousConnection?.TargetConnection != null)
        {
            Debug.LogWarning($" - Healing stack, disconnecting Prev from {ConnectionModel.GetConnectionModelID(this.PreviousConnection.TargetConnection)}");
            this.PreviousConnection.Disconnect(); // ¿Esto modifica SourceBlock?
        }
        if (this.NextConnection?.TargetConnection != null)
        {
            Debug.LogWarning($" - Disconnecting Next from {ConnectionModel.GetConnectionModelID(this.NextConnection.TargetConnection)}");
            this.NextConnection.Disconnect(); // ¿Esto modifica SourceBlock?
                                              // ... conectar sucesor ...
        }
        Debug.LogWarning($"[Unplug Block:{ID}] END. Prev Conn Source After: {this.PreviousConnection?.SourceBlock?.ID ?? "NULL"}"); // Verificar si cambió
    }

    /// <summary>
    /// Return s all connections orgination from this block.
    /// </summary>
    public List<ConnectionModel> GetConnections()
    {
        var myConnections = new List<ConnectionModel>();
        if (null != OutputConnection)
        {
            myConnections.Add(OutputConnection);
        }
        if (null != PreviousConnection)
        {
            myConnections.Add(PreviousConnection);
        }
        if (null != NextConnection)
        {
            myConnections.Add(NextConnection);
        }

        for (int i = 0; i < InputList.Count; i++)
        {
            var input = InputList[i];
            if (null != input.Connection)
            {
                myConnections.Add(input.Connection);
            }
        }

        return myConnections;
    }

    /// <summary>
    /// Static factory method to create a disconnected template block.
    /// Called when workspace is null in BlockFactory.
    /// </summary>
    public static BlockModel CreateTemplate(string prototypeName, string uid)
    {
        return new BlockModel(prototypeName, uid); 
    }

    /// <summary>
    /// Walks down a stack of blocks and finds the last next connection on the stack.
    /// </summary>
    /// <returns> The last next connection on the stack,or null.</returns>
    public ConnectionModel LastConnectionInStack()
    {
        var nextConnection = NextConnection;
        while (null != nextConnection)
        {
            var nextBlock = nextConnection.TargetBlock;
            if (nextBlock == null)
            {
                // Found a next connection with nothing on the other side.
                return nextConnection;
            }
            nextConnection = nextBlock.NextConnection;
        }
        // Ran out of next connections.
        return null;
    }

    /// <summary>
    /// Get output, previous, next connection by connection type
    /// </summary>
    public ConnectionModel GetFirstClassConnection(EConnection connectionType)
    {
        switch (connectionType)
        {
            case EConnection.OutputValue: return OutputConnection;
            case EConnection.PrevStatement: return PreviousConnection;
            case EConnection.NextStatement: return NextConnection;
        }
        throw new Exception("BlockModel GetFirstClassConnection: Only get output, previous, next connection");
    }

    /// <summary>
    /// add a new input
    /// </summary>
    /// <param name="index">insert the new input at the index</param>
    public void AppendInput(InputModel input, int index = -1)
    {
        if (!InputList.Contains(input))
        {
            input.SourceBlock = this;
            if (index > 0) InputList.Insert(index, input);
            else InputList.Add(input);

            FireUpdate(1 << (int)UpdateStates.Inputs);
        }
    }

    /// <summary>
    /// Remove an input from this block.
    /// </summary>
    public void RemoveInput(InputModel input)
    {
        if (InputList.Contains(input))
        {
            input.Dispose();
            InputList.Remove(input);

            FireUpdate(1 << (int)UpdateStates.Inputs);
        }
    }

    /// <summary>
    /// Check exist a named input object
    /// </summary>
    public bool HasInput(string name)
    {
        return InputList.Any(t => name.Equals(t.Name));
    }

    /// <summary>
    /// Fetches the named input object.
    /// </summary>
    public InputModel GetInput(string name)
    {
        for (int i = 0; i < InputList.Count; i++)
        {
            if (name.Equals(InputList[i].Name))
                return InputList[i];
        }
        return null;
    }

    /// <summary>
    /// Return the input that connects to the specified block.
    /// @param {!ScratchBlocks.BlockModel} block A block connected to an input on this block.
    /// @return {ScratchBlocks.Input} The input that connects to the specified block.
    /// </summary>
    /// <param name="block"></param>
    /// <returns></returns>
    public InputModel GetInputWithBlock(BlockModel block)
    {
        for (int i = 0; i < InputList.Count; i++)
        {
            var input = InputList[i];
            if (null != input.Connection && input.Connection.TargetBlock == block)
            {
                return input;
            }
        }
        return null;
    }

    /// <summary>
    /// Fetches the block attached to the named input.
    /// </summary>
    /// <returns>The attached value block, or null if the input is either disconnected or if the input does not exist.</returns>
    public BlockModel GetInputTargetBlock(string name)
    {
        InputModel input = this.GetInput(name);
        if (input != null && input.Connection != null && input.Connection.TargetBlock != null)
            return input.Connection.TargetBlock;
        return null;
    }

    /// <summary>
    /// Return the parent block that surrounds the current block,or null if this
    /// block has no surrounding block. A parent block might just be the previous
    /// statement,whereas the surrounding block is an if statement,while loop,etc.
    /// @return {ScratchBlocks.BlockModel} The block that surrounds the current block.
    /// </summary>
    /// <returns></returns>
    public BlockModel GetSurroundParent()
    {
        var block = this;
        var prevBlock = block;
        do
        {
            prevBlock = block;
            block = block.ParentBlock;
            if (null == block)
            {
                // Ran off the top.
                return null;
            }
        } while (block.NextBlock == prevBlock);
        // This block is an enclosing parent,not just a statement in a stack.
        return block;
    }

    /// <summary>
    /// Return the next statement block directly connected to this block.
    /// </summary>
    public BlockModel NextBlock
    {
        get { return null != NextConnection ? NextConnection.TargetBlock : null; }
    }

    /// <summary>
    /// Return the top-most block in this block's tree.
    /// This will return itself if this block is at the top level.
    /// </summary>
    public BlockModel RootBlock
    {
        get
        {
            BlockModel rootBlock;
            var block = this;
            do
            {
                rootBlock = block;
                block = rootBlock.ParentBlock;
            } while (null != block);
            return rootBlock;
        }
    }

    public void SetParent(BlockModel newParent)
    {
        if (newParent == ParentBlock)
        {
            return;
        }
        if (null != ParentBlock)
        {
            // Remove this block from the old parent's child list.
            ParentBlock.ChildBlocks.Remove(this);

            // Disconnect from superior blocks
            if (null != this.PreviousConnection && this.PreviousConnection.IsConnected)
            {
                throw new Exception("Still connected to previous block.");
            }
            if (null != OutputConnection && this.OutputConnection.IsConnected)
            {
                throw new Exception("Still connected to parent block.");
            }
            this.ParentBlock = null;
        
        }
        else
        {
            // Remove this block from the workspace's list of top-most blocks.
            this.Workspace.RemoveTopBlock(this);
        }

        this.ParentBlock = newParent;
        if (newParent != null)
            newParent.ChildBlocks.Add(this);
        else
            this.Workspace.AddTopBlock(this);
    }

    /// <summary>
    /// Find all the blocks that are directly nested inside this one.
    /// Includes value and block inputs,as well as any following statement.
    /// Excludes any connection on an outpu tab or any preceding statement.
    /// </summary>
    public List<BlockModel> GetChildren()
    {
        return ChildBlocks;
    }

    /// <summary>
    /// Returns the connections on this block that are suitable for initiating
    /// a connection attach operation during block dragging.
    /// Usually includes OutputConnection and PreviousConnection.
    /// </summary>
    /// <returns>A list containing the possible initiating connections.</returns>
    public List<ConnectionModel> GetDraggingConnections() // Nombre como en tu error
    {
        Debug.Log($"<color=orange>GetDraggingConnections() called for Block ID: {ID} ({Type})</color>");
        var draggingConnections = new List<ConnectionModel>();

        // Si el bloque tiene una conexión de salida, es un punto de inicio de drag
        if (OutputConnection != null)
        {
            Debug.Log($"  - Considering Output: {ConnectionModel.GetConnectionModelID(OutputConnection)}");

            if (this.OutputConnection.SourceBlock == this) draggingConnections.Add(OutputConnection);
        }
        // Si el bloque tiene una conexión anterior (puede apilarse encima), es un punto de inicio
        else if (PreviousConnection != null) 
        {
            Debug.Log($"  - Considering Output: {ConnectionModel.GetConnectionModelID(this.OutputConnection)}");
            if (this.PreviousConnection.SourceBlock == this) draggingConnections.Add(PreviousConnection);
        }

       else  if (NextConnection != null)
        {
            Debug.Log($"  - Considering Next: {ConnectionModel.GetConnectionModelID(NextConnection)}");
            if (this.NextConnection.SourceBlock == this) draggingConnections.Add(this.NextConnection);
        }

        foreach (var input in this.InputList)
        {
           
            if (input != null && input.Connection != null)
            {
                if (input.Name == "STEPS") // Filtra solo para el input STEPS
                {
                    // Log existente
                    Debug.Log($"[GetDraggingConn:{this.ID}] Checking SPECIFIC Input 'STEPS'. ConnectionType={input.Connection.Type}. Found SourceBlock ID = {input.Connection.SourceBlock?.ID ?? "NULL"}");

                        Debug.Log($"[GetDraggingConn Check] Block: {this.ID}, Input: '{input.Name}'. Conn Hash: {input.Connection.GetHashCode()}, Found SourceBlock: {input.Connection.SourceBlock?.ID ?? "NULL"}");
                }
                
                // Solo considera conexiones de tipo InputValue (las que aceptan bloques con Output)
                if (input.Connection.Type == EConnection.InputValue)
                {
                    Debug.Log($"  - Considering Input '{input.Name}' (Type:InputValue): {ConnectionModel.GetConnectionModelID(input.Connection)} -> Found SourceBlock: {input.Connection.SourceBlock?.ID ?? "NULL"}");

                    // Añade a la lista SÓLO si el SourceBlock está asignado correctamente a este bloque
                    if (input.Connection.SourceBlock == this)
                    {
                       
                        Debug.LogWarning($"[GetDraggingConn:{this.ID}] ADDING InputValue Connection '{input.Name}' to dragging list because SourceBlock matches. Is this intended?");
                        draggingConnections.Add(input.Connection);
                    }
                    else
                    {
                        // Solo un aviso si el SourceBlock no coincide 
                        Debug.LogWarning($"[GetDraggingConn:{this.ID}] InputValue '{input.Name}' Connection has WRONG SourceBlock ID: {input.Connection.SourceBlock?.ID ?? "NULL"} (Expected: {this.ID}). Not adding.");
                    }
                }
              

            } // Fin if input y connection no son null
        }

        Debug.Log($"<color=orange> GetDraggingConnections() returning LIST with {draggingConnections.Count} connections:</color>");
        for (int i = 0; i < draggingConnections.Count; i++)
        {
            Debug.Log($"   - FinalList[{i}]: {ConnectionModel.GetConnectionModelID(draggingConnections[i])}");
        }

        return draggingConnections;
    }


    /// <summary>
    /// Find all the blocks that are directly or indirectly nested inside this one.
    /// Includes this block in the list.
    /// Includes value and block inputs, as well as any following statements.
    /// Excludes any connection on an output tab or any preceding statements.
    /// </summary>
    public List<BlockModel> GetDescendants()
    {
        var blocks = new List<BlockModel> { this };

        for (int i = 0; i < ChildBlocks.Count; i++)
        {
            blocks.AddRange(ChildBlocks[i].GetDescendants());
        }

        return blocks;
    }

    /// <summary>
    /// Returns the named field from a block.
    /// </summary>
    public FieldModel GetField(string name)
    {
        foreach (var input in InputList)
        {
            foreach (FieldModel field in input.FieldRow)
            {
                if (!string.IsNullOrEmpty(field.Name) && field.Name.Equals(name))
                    return field;
            }
        }
        return null;
    }

    /// <summary>
    /// Return all variables referenced by this block.
    /// </summary>
    /// <returns> List of variable names.</returns>
    public List<string> GetVars()
    {
        var vars = new List<string>();
        foreach (var input in InputList)
        {
            foreach (var field in input.FieldRow)
            {
                if (field is FieldVariableModel)
                    vars.Add(field.GetValue());
            }
        }
        return vars;
    }

    /// <summary>
    /// Notification that a variable is renaming.
    /// If the name matches one of this block's variables,rename it.
    /// </summary>
    /// <param name="oldName"> Previous name of variable</param>
    /// <param name="newName"> Renamed Variable.</param>
    public void RenameVar(string oldName, string newName)
    {
        foreach (var input in InputList)
        {
            foreach (var field in input.FieldRow)
            {
                if (field is FieldVariableModel && Names.Equals(oldName, field.GetValue()))
                {
                    field.SetValue(newName);
                }
            }
        }
    }

    /// <summary>
    /// Returns the langugage-neutral value from the field of a block.
    /// </summary>
    /// <param name="name"> The name of the field.</param>
    /// <returns>Value from the field or null if field does not exist.</returns>
    public string GetFieldValue(string name)
    {
        var field = this.GetField(name);
        if (null != field)
        {
            return field.GetValue();
        }
        return null;
    }

    /// <summary>
    /// Change the field value for a block (e.g. "CHOOSE" or "REMOVE").
    /// </summary>
    /// <param name="name"> The name of the field.</param>
    /// <param name="newValue"> Value to be the new field.</param>
    public void SetFieldValue(string name, string newValue)
    {
        var field = GetField(name);
        if (null == field) Debug.LogError("Field " + name + " not found");
        field.SetValue(newValue);
    }

    #region State Properties

    /// <summary>
    /// if this block is disabled
    /// </summary>
    private bool mDisabled = false;
    public bool Disabled
    {
        get { return mDisabled; }
        set
        {
            if (Disabled != value)
            {
                mDisabled = value;
                FireUpdate(1 << (int)UpdateStates.IsDisabled);
            }
        }
    }

    /// <summary>
    /// whether this block is deletable or not.
    /// </summary>
    private bool mDeletable = true;
    public bool Deletable
    {
        get { return mDeletable && !mIsShadow && !(Workspace != null && Workspace.Options.ReadOnly); }
        set
        {
            if (mDeletable != value)
            {
                mDeletable = value;
                FireUpdate(1 << (int)UpdateStates.IsDeletable);
            }
        }
    }

    /// <summary>
    /// whether this block is movable or not.
    /// </summary>
    private bool mMovable = true;
    public bool Movable
    {
        get { return mMovable && !mIsShadow && !(Workspace != null && Workspace.Options.ReadOnly); }
        set
        {
            if (mMovable != value)
            {
                mMovable = value;
                FireUpdate(1 << (int)UpdateStates.IsMovable);
            }
        }
    }

    /// <summary>
    /// whether this block is a shadow block or not.
    /// </summary>
    private bool mIsShadow = false;
    public bool IsShadow
    {
        get { return mIsShadow; }
        set
        {
            if (mIsShadow != value)
            {
                mIsShadow = value;
                FireUpdate(1 << (int)UpdateStates.IsShadow);
            }
        }
    }

    /// <summary>
    /// whether this block is editable or not.
    /// </summary>
    private bool mEditable = true;
    public bool Editable
    {
        get { return mEditable && !(Workspace != null && Workspace.Options.ReadOnly); }
        set
        {
            if (mEditable != value)
            {
                mEditable = value;
                FireUpdate(1 << (int)UpdateStates.IsEditable);
            }
        }
    }

    /// <summary>
    /// Whether the block is collapsed.
    /// </summary>
    private bool mCollapsed = false;
    public bool Collapsed
    {
        get { return mCollapsed; }
        set
        {
            if (mCollapsed != value)
            {
                mCollapsed = value;
                FireUpdate(1 << (int)UpdateStates.IsCollapsed);
            }
        }
    }

    /// <summary>
    /// -1: not defined; 0: defined false; 1: defined true
    /// </summary>
    private int mInputsInlineState = -1;

    /// <summary>
    /// Set whether value inputs are arranged horizontally or vertically.
    /// </summary>
    /// <param name="value"> Ture if inputs are horizontal.</param>
    public void SetInputsInline(bool value)
    {
        if (value && mInputsInlineState != 1)
        {
            mInputsInlineState = 1;
            FireUpdate(1 << (int)UpdateStates.IsInputInline);
        }
        else if (!value && mInputsInlineState != 0)
        {
            mInputsInlineState = 0;
            FireUpdate(1 << (int)UpdateStates.IsInputInline);
        }
    }

    /// <summary>
    /// Get whether value inputs are arranged horizontally or vertically.
    /// </summary>
    /// <returns> True if inputs are horizontal.</returns>
    public bool GetInputsInline()
    {
        if (mInputsInlineState >= 0)
        {
            // Set explicitly.
            return mInputsInlineState == 1;
        }

        // Not defined explicitly. Figure out what would look best.
        for (int i = 1; i < InputList.Count; i++)
        {
            if (InputList[i - 1].Type == EConnection.DummyInput &&
                InputList[i].Type == EConnection.DummyInput)
            {
                // Two dummy inputs in a row. Don't inline them.
                return false;
            }
        }
        for (int i = 1; i < InputList.Count; i++)
        {
            if (InputList[i - 1].Type == EConnection.InputValue &&
                InputList[i].Type == EConnection.DummyInput)
            {
                // Dummy input after a value inpput . Inline them.
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get whether the block is disabled or not due to parents.
    /// The block's own disabled property is not considered.
    /// </summary>
    /// <returns> True if disabled.</returns>
    public bool GetInheritedDisabled()
    {
        var ancestor = this.GetSurroundParent();
        while (null != ancestor)
        {
            if (ancestor.Disabled)
            {
                return true;
            }
            ancestor = ancestor.GetSurroundParent();
        }
        // Ran off the top.
        return false;
    }

    /// <summary>
    /// Recursively checks whether all statement and value inputs are filled with
    /// blocks. Also checks all following statement blocks in this stack.
    /// </summary>
    public bool AllInputsFilled(bool optShadowBlocksAreFilled = true)
    {
        // Account for the shadow block filledness toggle.
        if (!optShadowBlocksAreFilled && mIsShadow)
        {
            return false;
        }

        // Recursively check each input block of the current block.
        for (int i = 0; i < InputList.Count; i++)
        {
            var input = InputList[i];
            if (null == input.Connection)
            {
                continue;
            }
            var target = input.Connection.TargetBlock;
            if (null != target || !target.AllInputsFilled(optShadowBlocksAreFilled))
            {
                return false;
            }
        }

        // Recusively check the next block after the current block.
        var next = this.NextBlock;
        if (null != next)
        {
            return next.AllInputsFilled(optShadowBlocksAreFilled);
        }
        return true;
    }

    #endregion

    /// <summary>
    /// This method returns a string describing this BlockModel in developer terms (type
    /// name and ID; English only).
    /// 
    /// Intended to on be used in console logs and errors. If you need a string that
    /// uses the user's native language (including block text, field values, and
    /// child blocks), use [toString()]{@link ScratchBlocks.BlockModel#toString}.
    /// </summary>
    /// <returns></returns>
    public string ToDevString()
    {
        var msg = !string.IsNullOrEmpty(this.Type) ? "\"" + this.Type + "\" block" : "BlockModel";
        if (!string.IsNullOrEmpty(this.ID))
        {
            msg += " (id=\"" + this.ID + "\")";
        }
        return msg;
    }

    /// <summary>
    /// Called by a FieldModel when its value has changed.
    /// This provides a hook for the BlockModel itself (or associated logic like Mutators)
    /// to react to internal changes, beyond just notifying external observers (like the BlockView).
    /// For example, a Mutator might monitor changes in certain fields to reconfigure the block.
    /// The base implementation can be empty or provide basic logging.
    /// </summary>
    /// <param name="field">The specific FieldModel that triggered the change.</param>
    public virtual void OnModelChange(FieldModel field) // Marked virtual for potential future flexibility
    {
      
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Debug.Log($"Block '{this.Type}' ({this.ID}): Field '{field?.Name}' changed. Value: '{field?.GetValue()}'", Workspace?.GetBlockById(this.ID));
        #endif

       
    }

}//Fin BlockModel

