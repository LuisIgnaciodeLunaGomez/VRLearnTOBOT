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
 * Descripción:  Define los eventos que puede disparar una conexión

 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEngine;

public class ConnectionModel : Observable<UpdateState>
{

    private BlockModel mSourceBlock;
    public BlockModel SourceBlock
    {
        get { return mSourceBlock; }
        set
        {
            Debug.Log($"[Setter ENTER] Conn Hash: {this.GetHashCode()}, " +
                      $"Current mSourceBlock ID: {mSourceBlock?.ID ?? "NULL"} (Hash: {mSourceBlock?.GetHashCode() ?? -1}), " +
                      $"Value to assign ID: {value?.ID ?? "NULL"} (Hash: {value?.GetHashCode() ?? -1})");

            if (mSourceBlock == value)
            {
                Debug.Log($"[Setter SKIP] Value is the same as mSourceBlock. Exiting.");
                return; 
            }

            Debug.Log($"[Setter PRE-INTERNAL ASSIGN] Conn Hash: {this.GetHashCode()}, " +
                      $"About to set mSourceBlock. Current value: {mSourceBlock?.ID ?? "NULL"}");

            // asignación real
            mSourceBlock = value;

            Debug.Log($"[Setter POST-INTERNAL ASSIGN] Conn Hash: {this.GetHashCode()}, " +
                      $"mSourceBlock should NOW be ID: {mSourceBlock?.ID ?? "NULL"} (Hash: {mSourceBlock?.GetHashCode() ?? -1}). " +
                      $"Was it set to 'value'?: {(System.Object.ReferenceEquals(mSourceBlock, value) ? "YES" : "NO!!!")}");

        string finalState = (mSourceBlock == null) ? "NULL" : "NOT NULL";
        Debug.Log($"[Setter EXIT SIMPLIFIED] Conn Hash: {this.GetHashCode()}, Final mSourceBlock is: {finalState}");
        
        }
    }  
   
    public InputModel Input { get; internal set; }

    /// <summary>
    /// The type of the connection.
    /// </summary>
    public EConnection Type { get; private set; }

    /// <summary>
    /// Does the connection belong to a superior block (higher in the source stack)?
    /// </summary>
    public bool IsSuperior
    {
        get { return this.Type == EConnection.InputValue || this.Type == EConnection.NextStatement; }
    }

    /// <summary>
    /// Class for a connection between blocks.
    /// </summary>
    /// <param name="source">The block establishing this connection.</param>
    /// <param name="type">The type of the connection.</param>
    public ConnectionModel(BlockModel source, EConnection type)
    {
        if (source == null && type != EConnection.None) // Permitir None para inputs dummy sin bloqu o  requerirlo siempre
            Debug.LogWarning($"Creating connection of type {type} with a NULL source block!");
        Type = type; 
        SourceBlock = source;
        InDB = false;
    }

    /// <summary>
    /// Class for a connection between blocks.
    /// </summary>
    /// <param name="type"></param>
    private ConnectionModel(EConnection type) : this(null, type)
    {
    }

    public const int CAN_CONNECT = 0;
    public const int REASON_SELF_CONNECTION = 1;
    public const int REASON_WRONG_TYPE = 2;
    public const int REASON_TARGET_NULL = 3;
    public const int REASON_CHECKS_FAILED = 4;
    public const int REASON_DIFFERENT_WORKSPACES = 5;
    public const int REASON_SHADOW_PARENT = 6;

    /// <summary>
    /// Connection this connection connects to.  Null if not connected.
    /// </summary>
    public ConnectionModel TargetConnection;

    /// <summary>
    /// Is the connection connected?
    /// </summary>
    public bool IsConnected
    {
        get { return TargetConnection != null; }
    }

    /// <summary>
    /// Returns the block that this connection connects to.
    /// </summary>
    public BlockModel TargetBlock
    {
        get
        {
            if (this.IsConnected)
            {
                return this.TargetConnection.SourceBlock; //inferior block
            }
            return null;
        }
    }

    /// <summary>
    /// List of compatible value types.  Null if all types are compatible.
    /// </summary>
    public List<string> Check { get; protected set; }

    /// <summary>
    /// Horizontal and Vertical location of this connection.
    /// </summary>
    public Vector2 Location;
    public float X
    {
        get { return Location.x; }
        set { Location.x = value; }
    }
    public float Y
    {
        get { return Location.y; }
        set { Location.y = value; }
    }

    /// <summary>
    /// Has this connection been added to the connection database?
    /// </summary>
    public bool InDB { get; set; }

    /// <summary>
    /// Connection database for connections of this type on the current workspace.
    /// </summary>
    public BlockConnectionDB DB { get; internal set; } 

    /// <summary>
    /// Connection database for connections compatible with this type on the current workspace.
    /// </summary>
    public BlockConnectionDB DBOpposite { get; internal set; }

    public EConnection OppositeType
    {
        get { return GetOppositeType(this.Type); }
    }

    public static EConnection GetOppositeType(EConnection type)
    {
        switch (type)
        {
            case EConnection.InputValue: return EConnection.OutputValue;
            case EConnection.OutputValue: return EConnection.InputValue;
            case EConnection.NextStatement: return EConnection.PrevStatement;
            case EConnection.PrevStatement: return EConnection.NextStatement;
            default: throw new System.ArgumentException("Unhandled connection type: " + type);
        }
    }

    /// <summary>
    /// Whether this connections is hidden (not tracked in a database) or not.
    /// </summary>
    public bool Hidden { get; internal set; }

    /// <summary>
    /// DOM representation of a block or null.
    /// </summary>
    public XmlNode ShadowDom { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="otherConnection"></param>
    public void Connect(ConnectionModel otherConnection)
    {
        if (this.TargetConnection == otherConnection)
        {
            return;
        }

        this.CheckConnection(otherConnection);

        if (this.IsSuperior)
            this.ConnectInternal(otherConnection);
        else
            otherConnection.ConnectInternal(this);
    }

    /// <summary>
    /// Connect two connections together.  This is the connection on the superior block.
    /// </summary>
    /// <param name="childConnection"></param>
    private void ConnectInternal(ConnectionModel childConnection)
    {
        var parentConnection = this;
        var parentBlock = parentConnection.SourceBlock; //superior block
        var childBlock = childConnection.SourceBlock; //inferior block

        if (childConnection.IsConnected)
            childConnection.Disconnect();

        
        if (parentConnection.IsConnected)
        {
            BlockModel orphanBlock = parentConnection.TargetBlock;
            XmlNode shadowDom = parentConnection.ShadowDom;
            parentConnection.ShadowDom = null;

            if (orphanBlock.IsShadow)
            {
                shadowDom = Xml.BlockToDom(orphanBlock);
                orphanBlock.Dispose();
                orphanBlock = null;
            }
            else if (parentConnection.Type == EConnection.InputValue)
            {
                if (orphanBlock.OutputConnection == null)
                    throw new Exception("Orphan block does not have an output connection.");

             
                var connection = ConnectionModel.LastConnectionInRow(childBlock, orphanBlock);
                if (connection != null)
                {
                    orphanBlock.OutputConnection.Connect(connection);
                    orphanBlock = null;
                }
            }
            else if (parentConnection.Type == EConnection.NextStatement)
            {
               
                if (orphanBlock.PreviousConnection == null)
                    throw new Exception("Orphan block does not have a previous connection.");

               
                var newBlock = childBlock;
                while (newBlock.NextConnection != null)
                {
                    var nextBlock = newBlock.NextBlock;
                    if (nextBlock != null && !nextBlock.IsShadow)
                    {
                        newBlock = nextBlock;
                    }
                    else
                    {
                        if (orphanBlock.PreviousConnection.CheckType(newBlock.NextConnection))
                        {
                            newBlock.NextConnection.Connect(orphanBlock.PreviousConnection);
                            orphanBlock = null;
                        }
                        break;
                    }
                }
            }

            if (orphanBlock != null)
            {
                // Unable to reattach orphan.
                parentConnection.Disconnect();
                ConnectionModel orphanBlockCon = orphanBlock.OutputConnection != null ? orphanBlock.OutputConnection : orphanBlock.PreviousConnection;
                orphanBlockCon.FireUpdate(UpdateState.BumpedAway);
            }

            // Restore the shadow DOM.
            parentConnection.ShadowDom = shadowDom;
        }

        // Establish the connections.
        ConnectionModel.ConnectReciprocally(parentConnection, childConnection);
        // Demote the inferior block so that one is a child of the superior one.
        childBlock.SetParent(parentBlock);

        FireUpdate(UpdateState.Connected);
    }

    /// <summary>
    /// Sever all links to this connection (not including from the source object).
    /// </summary>
    public void Dispose()
    {
        if (this.IsConnected)
        {
            Debug.LogError($"ConnectionModel.Dispose: Connection {GetConnectionModelID(this)} is still connected! Disconnect first.");

           // throw new Exception("Disconnect connection before disposing of it.");
        }

        bool wasInDB = this.InDB; // Guarda el estado inicial
        BlockConnectionDB dbRef = this.DB; // Guarda la referencia
        string connIdForLog = GetConnectionModelID(this); // Genera ID antes de que SourceBlock pueda cambiar

        GameObject contextObj = null;// this.SourceBlock?.gameObject; // Obtener contexto si es posible

        Debug.Log($"[Conn Dispose ENTER] {connIdForLog}. InDB={wasInDB}. DB Ref Null? {(dbRef == null)}", contextObj);

        if (wasInDB && dbRef != null)
        {
            Debug.Log($"  --> Attempting Remove from DB {dbRef.GetType().Name}. Contains Before? {dbRef.Contains(this)}", contextObj);
            bool removed = false;
            try
            {
                removed = dbRef.Remove(this); 
            }
            catch (Exception e)
            {
                Debug.LogError($"  --> EXCEPTION during DB.Remove! Conn:{connIdForLog}, Err:{e}", contextObj);
            }
            Debug.Log($"  --> Removal Result: removed={removed}. Contains After? {dbRef.Contains(this)}. Forcing InDB=false.", contextObj);
            this.InDB = false; // Asegurar InDB es false después del intento
        }
        else if (wasInDB /*&& dbRef == null*/)
        {
            Debug.LogWarning($"[Conn Dispose] {connIdForLog} had InDB=true BUT DB was NULL! Forcing InDB=false.", contextObj);
            this.InDB = false;
        }
        else
        {
            // Debug.Log($"[Conn Dispose] {connIdForLog} - InDB was false. No removal needed.", contextObj);
            this.InDB = false;
        }

        this.DB = null;
        this.DBOpposite = null;

        Debug.Log($"[Conn Dispose EXIT] {connIdForLog}", contextObj);
    }

    /// <summary>
    /// Checks whether the current connection can connect with the target connection.
    /// </summary>
    public int CanConnectWithReason(ConnectionModel target)
    {
        if (target == null)
            return ConnectionModel.REASON_TARGET_NULL;

        var blockA = this.IsSuperior ? this.mSourceBlock : target.SourceBlock;
        var blockB = this.IsSuperior ? target.SourceBlock : this.mSourceBlock;

        if (blockA != null && blockA == blockB)
        {
            return ConnectionModel.REASON_SELF_CONNECTION;
        }
        if (target.Type != Define.OppositeConnection(this.Type))
        {
            return ConnectionModel.REASON_WRONG_TYPE;
        }
        if (blockA != null && blockB != null && blockA.Workspace != blockB.Workspace)
        {
            return ConnectionModel.REASON_DIFFERENT_WORKSPACES;
        }
        if (!this.CheckType(target))
        {
            return ConnectionModel.REASON_CHECKS_FAILED;
        }
        if (blockA != null && blockB != null && blockA.IsShadow && !blockB.IsShadow)
        {
            return ConnectionModel.REASON_SHADOW_PARENT;
        }
        return ConnectionModel.CAN_CONNECT;
    }

    /// <summary>
    /// Checks whether the current connection and target connection are compatible and throws an exception if they are not.
    /// </summary>
    public void CheckConnection(ConnectionModel target)
    {
        switch (CanConnectWithReason(target))
        {
            case ConnectionModel.CAN_CONNECT:
                break;
            case ConnectionModel.REASON_SELF_CONNECTION:
                throw new Exception("Attempted to connect a block to itself.");
            case ConnectionModel.REASON_DIFFERENT_WORKSPACES:
                // Usually this means one block has been deleted.
                throw new Exception("Blocks not on same workspace.");
            case ConnectionModel.REASON_WRONG_TYPE:
                throw new Exception("Attempt to connect incompatible types.");
            case ConnectionModel.REASON_TARGET_NULL:
                throw new Exception("Target connection is null.");
            case ConnectionModel.REASON_CHECKS_FAILED:
                StringBuilder thisCheckStr = new StringBuilder();
                foreach (var c in Check)
                    thisCheckStr.Append(c + ", ");
                StringBuilder targetCheckStr = new StringBuilder();
                foreach (var c in target.Check)
                    targetCheckStr.Append(c + ", ");
                throw new Exception(string.Format("Connection checks failed. {0} expected {1}, found {2}", this.ToString(), thisCheckStr, targetCheckStr));
            case ConnectionModel.REASON_SHADOW_PARENT:
                throw new Exception("Connecting non-shadow to shadow block.");
            default:
                throw new Exception("Unknown connection failure: this should never happen!");
        }
    }

    /// <summary>
    /// Check if the two connections can be dragged to connect to each other.
    /// </summary>
    public bool IsConnectionAllowed(ConnectionModel candidate, float maxRadius = 0)
    {
       // Debug.Log($"      [IsConnectionAllowed Check] Self: {GetConnectionModelID(this)} VS Candidate: {GetConnectionModelID(candidate)}, MaxRadius: {maxRadius}");
        if (candidate == null)
        {
            Debug.Log("        -> FAILED: Candidate is NULL.");
            return false;
        }
        int reason = this.CanConnectWithReason(candidate);
        if (reason != ConnectionModel.CAN_CONNECT)
        {
            string reasonStr = "Unknown";
            switch (reason)
            {
                case REASON_SELF_CONNECTION: reasonStr = "Self Connection"; break;
                case REASON_WRONG_TYPE: reasonStr = "Wrong Type"; break;
                case REASON_TARGET_NULL: reasonStr = "Target Null (already checked)"; break; // Redundante aquí
                case REASON_CHECKS_FAILED: reasonStr = "Checks Failed"; break;
                case REASON_DIFFERENT_WORKSPACES: reasonStr = "Different Workspaces"; break;
                case REASON_SHADOW_PARENT: reasonStr = "Shadow Parent Issue"; break;
            }
            Debug.Log($"        -> FAILED (CanConnectWithReason): {reasonStr} ({reason})");
            return false;
        }
    //    Debug.Log("        - Passed: Basic connection reasons (Type, Workspace, Checks, etc.) OK.");

        BlockModel candidateParent = candidate.SourceBlock?.ParentBlock; 
        while (candidateParent != null)
        {
            if (candidateParent == this.SourceBlock)
            {
                Debug.Log($"        -> FAILED: Connection would create a loop (candidate is child of self).");
                return false;
            }
            candidateParent = candidateParent.ParentBlock;
        }
        Debug.Log("        - Passed: No loop detected.");

       
        if (candidate.Type == EConnection.OutputValue || candidate.Type == EConnection.PrevStatement)
        {
            if (candidate.IsConnected) // Si el candidato inferior YA está conectado a OTRO
            {
                Debug.Log($"        -> FAILED: Candidate (Inferior: {candidate.Type}) is already connected to {GetConnectionModelID(candidate.TargetConnection)}.");
                return false;
            }
           
        }
      //  Debug.Log($"        - Passed: Candidate ({candidate.Type}) connection state check OK.");

        if (candidate.Type == EConnection.InputValue && candidate.IsConnected &&
            candidate.TargetBlock != null && // Safety check
            !candidate.TargetBlock.Movable && !candidate.TargetBlock.IsShadow)
        {
            Debug.Log("        -> FAILED: Candidate (InputValue) is connected to an immovable, non-shadow block.");
            return false;
        }
       
        float dist = this.DistanceFrom(candidate);
      //  Debug.Log($"        - Checking Distance: Calculated={dist}, MaxRadius={maxRadius}");
        if (maxRadius > 0 && dist > maxRadius)
        {
            Debug.Log($"        -> FAILED: Distance ({dist}) exceeds MaxRadius ({maxRadius}).");
            return false;
        }
     
        return true;
    }

    public static void ConnectReciprocally(ConnectionModel first, ConnectionModel second)
    {
        Debug.Log($"<color=lightblue>ConnectReciprocally:</color> Setting target for First ({GetConnectionModelID(first)}) to Second ({GetConnectionModelID(second)})");

        if (first == null || second == null)
            throw new Exception("Cannot connect null connections.");
        Debug.Log($"  - BEFORE: first.TargetConnection = {GetConnectionModelID(first.TargetConnection)}");
        first.TargetConnection = second;
        Debug.Log($"  - First's TargetConnection is now: {GetConnectionModelID(first.TargetConnection)}");

        Debug.Log($"  - BEFORE: second.TargetConnection = {GetConnectionModelID(second.TargetConnection)}");

        second.TargetConnection = first;
        Debug.Log($"  - Second's TargetConnection is now: {GetConnectionModelID(second.TargetConnection)}");

    }

    /// <summary>
    /// Does the given block have one and only one connection point that will accept an orphaned block?
    /// </summary>
    /// <returns>The suitable connection point on 'block', or null.</returns>
    public static ConnectionModel SingleConnection(BlockModel block, BlockModel orphanBlock)
    {
        ConnectionModel connection = null;
        foreach (var input in block.InputList)
        {
            var thisConnection = input.Connection;
            if (thisConnection != null && thisConnection.Type == EConnection.InputValue
                && orphanBlock.OutputConnection.CheckType(thisConnection))
            {
                if (connection != null)
                {
                    //more than one connection
                    return null;
                }
                connection = thisConnection;
            }
        }
        return connection;
    }

    /// <summary>
    /// Walks down a row a blocks, at each stage checking if there are any connections that will accept the orphaned block.  
    /// If at any point there are zero or multiple eligible connections, returns null.  
    /// Otherwise returns the only input on the last block in the chain.
    /// Terminates early for shadow blocks.
    /// </summary>
    /// <param name="startBlock">The block on which to start the search</param>
    /// <param name="orphanBlock">The block that is looking for a home</param>
    /// <returns>The suitable connection point on the chain of blocks, or null.</returns>
    public static ConnectionModel LastConnectionInRow(BlockModel startBlock, BlockModel orphanBlock)
    {
        var newBlock = startBlock;
        ConnectionModel connection = ConnectionModel.SingleConnection(newBlock, orphanBlock);
        while (connection != null)
        {
            newBlock = connection.TargetBlock;
            if (newBlock == null || newBlock.IsShadow)
                return connection;

            connection = ConnectionModel.SingleConnection(newBlock, orphanBlock);
        }
        return null;
    }

    /// <summary>
    /// Disconnect this connection.
    /// </summary>
    public void Disconnect()
    {
        if (!IsConnected) return;

        var otherConnection = TargetConnection;
        if (otherConnection.TargetConnection != this)
        {
            Debug.LogWarning("Target connection not connected to source connection.");
            return;
        }

        if (this.IsSuperior)
        {
            this.DisconnectInternal(otherConnection);
            this.RespawnShadow();
        }
        else
        {
            otherConnection.DisconnectInternal(this);
            otherConnection.RespawnShadow();
        }
    }

    private void DisconnectInternal(ConnectionModel childConnection)
    {
        var otherConnection = this.TargetConnection;
        otherConnection.TargetConnection = null;
        this.TargetConnection = null;
        childConnection.SourceBlock.SetParent(null);
        FireUpdate(UpdateState.Disconnected);
    }

    /// <summary>
    /// Respawn the shadow block if there was one connected to this connection.
    /// </summary>
    public void RespawnShadow()
    {
        var parentBlock = this.SourceBlock;
        var shadow = this.ShadowDom;
        if (parentBlock.Workspace != null && shadow != null /*&& Events.recordUndo*/)
        {
            var blockShadow = Xml.DomToBlock(shadow, parentBlock.Workspace);
            if (blockShadow.OutputConnection != null)
            {
                this.Connect(blockShadow.OutputConnection);
            }
            else if (blockShadow.PreviousConnection != null)
            {
                this.Connect(blockShadow.PreviousConnection);
            }
            else
            {
                throw new Exception("Child block does not have output or previous statement.");
            }
        }
    }

    /// <summary>
    /// Is this connection compatible with another connection with respect to the
    /// value type system.  E.g. square_root("Hello") is not compatible.
    /// </summary>
    /// <param name="otherConnection"></param>
    /// <returns></returns>
    public bool CheckType(ConnectionModel otherConnection)
    {
        if (this.Check == null || otherConnection.Check == null)
            return true;

        foreach (var i in this.Check)
        {
            if (otherConnection.Check.Contains(i))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Function to be called when this connection's compatible types have changed.
    /// </summary>
    protected virtual void OnCheckChanged()
    {
        // The new value type may not be compatible with the existing connection.
        if (this.IsConnected && !this.CheckType(this.TargetConnection))
        {
            var child = this.IsSuperior ? this.TargetBlock : this.SourceBlock;
            child.UnPlug();
        }
    }

    /// <summary>
    /// Change a connection's compatibility.
    /// </summary>
    /// <param name="check"></param>
    public void SetCheck(List<string> check)
    {
        if (check == null || check.Count == 0)
        {
            this.Check = null;
            return;
        }

        this.Check = check;
        this.OnCheckChanged();
    }



    /// <summary>
    /// Returns the distance between this connection and another connection in workspace units.
    /// </summary>
    public float DistanceFrom(ConnectionModel otherConnection)
    {
        return Vector2.Distance(this.Location, otherConnection.Location);
    }

    public override string ToString()
    {
        string msg = null;
        if (mSourceBlock == null || mSourceBlock.InputList == null)
            return "Orphan Connection";
        if (mSourceBlock.OutputConnection == this)
            msg = "Output Connection of ";
        else if (mSourceBlock.PreviousConnection == this)
            msg = "Previous Connection of ";
        else if (mSourceBlock.NextConnection == this)
            msg = "Next Connection of ";
        else
        {
            InputModel parentInput = mSourceBlock.InputList.Find(i => i.Connection == this);
            if (parentInput == null)
            {
                return "Orphan Connection";
            }
            msg = string.Format("Input \"{0}\" Connection on", parentInput.Name);
        }
        return msg + mSourceBlock.ToDevString();
    }

    /// <summary>
    ///  Find all nearby compatible connections to this connection.
    /// Type checking does not apply, since this function is used for bumping.
    /// </summary>
    /// <returns>List of connections</returns>
    /// 
    //////////////////////////////////////////NO SE USA////////////////////////////////////////
    public virtual List<ConnectionModel> Neighbours(int maxLimit)
    {
        return DBOpposite.GetNeighbours(this, maxLimit);
    }

    /// <summary>
    /// Genera un identificador de depuración para un ConnectionModel.
    /// </summary>
    /// <param name="conn">El ConnectionModel (puede ser null).</param>
    /// <returns>Un string identificador de depuración.</returns>
    public static string GetConnectionModelID(ConnectionModel conn)
    {
        if (conn == null) return "NULL_CONN";
        if (conn.SourceBlock == null)
        {
            // Loguear este error aquí es útil para saber que una conexión zombie fue procesada
            Debug.LogError($"GetConnectionModelID: conn.SourceBlock is NULL for Connection Hash: {conn.GetHashCode()}. This connection should have been removed from its DB!", null); // O pasa un contexto si puedes
            return $"CONN_ZOMBIE_{conn.GetHashCode()}_NO_SOURCE";
        }

        string sourceId = conn.SourceBlock?.ID ?? "NO_BLOCK";

        // Verifico si el Input está asociado directamente a esta conexión
        // Esto suele ser para EConnection.InputValue y EConnection.NextStatement dentro de inputs
        string inputName = conn.Input?.Name;

        if (!string.IsNullOrEmpty(inputName))
        {
            return $"Conn->{sourceId}.Input.{inputName}.{conn.Type}";
        }
        
        else
        {
            return $"Conn->{sourceId}.Direct.{conn.Type}";
        }
    }

    /// <summary>
    /// Asigna las referencias a las bases de datos correctas desde fuera de la clase.
    /// Debe ser llamado por WorkSpaceModel cuando registra la conexión.
    /// </summary>
    /// <param name="theDB">La base de datos para el tipo de esta conexión.</param>
    /// <param name="theDBOpposite">La base de datos para el tipo opuesto.</param>
    public void AssignDBReferences(BlockConnectionDB theDB, BlockConnectionDB theDBOpposite)
    {
      
        this.DB = theDB;
        this.DBOpposite = theDBOpposite;

     
        // Debug.Log($"[AssignDBReferences] Conn: {GetConnectionModelID(this)}, Assigned DB: {(theDB != null)}, Assigned DBOpposite: {(theDBOpposite != null)}");
    }

}//fin clase ConnectionModel
