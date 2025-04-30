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
            if (mSourceBlock == value) return;

            if (mSourceBlock != null && value != null)
                //throw new Exception("Connection is already a member of another block.");
                Debug.LogWarning($"Connection reassignment detected. Old: {mSourceBlock?.ID}, New: {value?.ID}");

            mSourceBlock = value;

            if (mSourceBlock?.Workspace != null)
            {
                if (mSourceBlock.Workspace.ConnectionDBList == null)
                {
                    Debug.LogError($"Block {mSourceBlock.ID} has a Workspace but its ConnectionDBList is null! Initialization error? {mSourceBlock}");

                    DB = null;
                    DBOpposite = null;
                    Hidden = true;
                    InDB = false; // No puede estar en DB si la lista es null
                    return; // Salir si la lista de DBs no existe
                }

                // Obtener los DB correspondientes del Workspace
                BlockConnectionDB db;
                if (mSourceBlock.Workspace.ConnectionDBList.TryGetValue(Type, out db))
                {
                    DB = db;
                    Hidden = false; // No está oculto si tiene un DB
                }
                else
                {

                    Debug.LogWarning($"ConnectionDB not found for type {Type} in workspace {mSourceBlock.Workspace.Id}");
                    DB = null;
                    Hidden = true; // Oculto si no hay DB
                }
                BlockConnectionDB dbOpposite;
                if (mSourceBlock.Workspace.ConnectionDBList.TryGetValue(Define.OppositeConnection(Type), out dbOpposite))
                {
                    DBOpposite = dbOpposite;
                }
                else
                {
                    // Debug.LogWarning($"Opposite ConnectionDB not found for type {Define.OppositeConnection(Type)}");
                    DBOpposite = null;
                }
            }
            else
            {
                DB = null;
                DBOpposite = null;
                Hidden = true;
                InDB = false; // No puede estar en la DB
                Debug.Log($"Connection {Type} on block {mSourceBlock?.ID ?? "NULL"} has no workspace/DB assigned.");
            }
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
        Type = type; 
        SourceBlock = source;
    }

    /// <summary>
    /// Class for a connection between blocks.
    /// </summary>
    /// <param name="type"></param>
    public ConnectionModel(EConnection type) : this(null, type)
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
    public BlockConnectionDB DB { get; private set; } 

    /// <summary>
    /// Connection database for connections compatible with this type on the current workspace.
    /// </summary>
    public BlockConnectionDB DBOpposite { get; private set; }

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
    public bool Hidden { get; private set; }

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
            throw new Exception("Disconnect connection before disposing of it.");
        }
        if (this.InDB)
        {
            this.DB.RemoveConnection(this);
        }

        this.DB = null;
        this.DBOpposite = null;
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
        int canConnect = this.CanConnectWithReason(candidate);
        if (canConnect != ConnectionModel.CAN_CONNECT)
            return false;

        BlockModel candidateParent = candidate.SourceBlock.ParentBlock;
        while (candidateParent != null)
        {
            if (candidateParent == this.SourceBlock)
                return false;
            candidateParent = candidateParent.ParentBlock;
        }

        if (candidate.Type == EConnection.OutputValue || candidate.Type == EConnection.PrevStatement)
        {
            if (candidate.IsConnected || this.IsConnected)
                return false;
        }

        if (candidate.Type == EConnection.InputValue && candidate.IsConnected
            && !candidate.TargetBlock.Movable && !candidate.TargetBlock.IsShadow)
        {
            return false;
        }

        if (this.Type == EConnection.PrevStatement && candidate.IsConnected
            && this.SourceBlock.NextConnection == null && !candidate.TargetBlock.IsShadow
            && candidate.TargetBlock.NextConnection != null)
        {
            return false;
        }

        if (maxRadius > 0 && this.DistanceFrom(candidate) > maxRadius)
            return false;

        return true;
    }

    public static void ConnectReciprocally(ConnectionModel first, ConnectionModel second)
    {
        if (first == null || second == null)
            throw new Exception("Cannot connect null connections.");
        first.TargetConnection = second;
        second.TargetConnection = first;
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

    // Añade este método estático dentro de la clase ConnectionModel:
    /// <summary>
    /// Genera un identificador de depuración para un ConnectionModel.
    /// </summary>
    /// <param name="conn">El ConnectionModel (puede ser null).</param>
    /// <returns>Un string identificador de depuración.</returns>
    public static string GetConnectionModelID(ConnectionModel conn)
    {
        if (conn == null) return "NULL_CONN";

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


}//fin clase ConnectionModel
