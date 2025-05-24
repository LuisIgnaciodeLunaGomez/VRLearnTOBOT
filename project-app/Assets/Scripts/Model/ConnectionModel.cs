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
 * Versión: 1.0.3
 * 
 * Descripción:  Define los eventos que puede disparar una conexión
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;
using static UnityEditor.FilePathAttribute;
public class ConnectionModel : Observable<UpdateState>
{
    private UpdateState m_UpdateState;
    private BlockModel mSourceBlock;
   

    public BlockModel SourceBlock
    {
        get { return mSourceBlock; }
        set
        {
            if (mSourceBlock == value) return;

            // Remueve la conexión de la DBs antiguas antes de cambiar el SB si es necesario
            if (mSourceBlock != null && mSourceBlock.Workspace != null && DB != null)
            {
                DB.RemoveConnection(this); 
            }

            mSourceBlock = value;

            // Si el nuevo SourceBlock tiene un WS, asignamos las DBs relevantes.
            if (mSourceBlock != null && mSourceBlock.Workspace != null && mSourceBlock.Workspace.ConnectionDBList != null)
            {
                BlockConnectionDB dbRef;
                mSourceBlock.Workspace.ConnectionDBList.TryGetValue(this.Type, out dbRef);
                this.DB = dbRef; // Asigna la referencia a la DB del nuevo WS

                BlockConnectionDB dbOppositeRef;
                mSourceBlock.Workspace.ConnectionDBList.TryGetValue(this.OppositeType, out dbOppositeRef);
                this.DBOpposite = dbOppositeRef;

                // Determina el estado Hidden (si es de tipo InputValue y ya está conectado)
              
                this.Hidden = (this.IsConnected || this.SourceBlock.Collapsed ); // Si es un InputValue/StatementInput y ya esta conectado a algo, o el bloque colapsa
                if (this.DB == null) Hidden = true; // Si la DB no existe, está oculta de hecho.

                            }
            else // Si el nuevo SB es nulo o no tiene un Workspace, limpia las referencias de DB
            {
                this.DB = null;
                this.DBOpposite = null;
                this.Hidden = true; // Siempre oculto si no tiene WS/DBs válidos.
            }

            }
    }

    public InputModel Input { get; internal set; }

    /// <summary>
    /// The type of the connection.
    /// </summary>
    public EConnection Type { get; private set; }

    private Vector2 m_location;


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
        if (source == null && type != EConnection.None) // Permitir None para inputs dummy sin bloque o  requerirlo siempre
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
   

    public Vector2 Location 
    {
        get { return m_location; }
        set { SetLocationInternal(value); } 
    }

    /*
    public float X
    {
        get { return Location.x; }
        set { Location.x = value; }
    }
    public float Y
    {
        get { return Location.y; }
        set { Location.y = value; }
    }*/


    private void SetLocationInternal(Vector2 newLocation) 
    {
        if (m_location == newLocation && InDB) // si la ubicacion es la misma y ya está en DB
            return; 

        bool wasInDB = this.InDB;
        BlockConnectionDB currentDB = this.DB; // Se cachea referencia antes de usarla

        //Quitamos la conexión de la DB si ya estaba en ella.
        if (wasInDB && currentDB != null)
        {
            //Debug.Log($"[CM.SetLocInternal] Conn '{GetConnectionModelID(this)}' was in DB. Removing from old location ({m_location:F2}).");
            currentDB.RemoveConnection(this); // InDB = false.
        }

        //Actualizar la ubicación en el modelo.
        this.m_location = newLocation;

        //Volver a añadir a la DB si debe estar ahí (no oculta y DB válida).
        if (!this.Hidden && currentDB != null)
        {
           // Debug.Log($"[CM.SetLocInternal] Conn '{GetConnectionModelID(this)}' is not hidden. Adding to DB at new location ({m_location:F2}).");
            currentDB.AddConnection(this); // InDB = true.
        }
        else if (wasInDB && currentDB == null)
        {
            // Inconsistencia grave. Si InDB era true pero DB es null.
          //  Debug.LogError($"[CM.SetLocInternal] Conn '{GetConnectionModelID(this)}' had InDB=true but DB was NULL during location update. Forcing InDB=false. This indicates an issue with DB assignment!");
            this.InDB = false;
        }
        //Si no estaba en DB o está oculta, no se hace nada más.
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
    /// Connects this connection to another connection.
    /// This method should robustly handle all valid connection scenarios,
    /// including bumping existing connections.
    /// </summary>
    /// <param name="otherConnection">The connection to connect to.</param>
    public void Connect(ConnectionModel otherConnection) 
    {
        if (otherConnection == null)
        {
            Debug.LogError($"[CM.Connect] ABORTED: otherConnection is null. Attempting to connect: {GetConnectionModelID(this)}");
            return;
        }

        // 'this' la conexión del bloque que se está arrastrando
        // 'otherConnection' es la conexión candidata estacionaria del workspace 

        ConnectionModel actualSuperiorConn = null; // Será la conexión del bloque que está esperando.
        ConnectionModel actualInferiorConn = null; // Será la conexión del bloque que se enchufa.

        // Log de entrada para depuración
        //Logger.Debug($"[CM.Connect ENTRY] Attempting to connect: THIS (Dragged): {GetConnectionModelID(this)} ({this.Type}) TO otherConnection (Stationary): {GetConnectionModelID(otherConnection)} ({otherConnection.Type})");

        // CASO 1: Se arrastra un PrevStatement (this) para conectar a un NextStatement (otherConnection)
        // Ejemplo: [Bloque A]<NEXT> <-- [Bloque B]<PREV arrastrado>
        // El bloque arrastrado (B) se conecta debajo del estacionario (A).
        if (this.Type == EConnection.PrevStatement && otherConnection.Type == EConnection.NextStatement)
        {
            actualSuperiorConn = otherConnection; // otherConnection (A.Next) es el socket superior.
            actualInferiorConn = this;            // this (B.Prev) es el enchufe inferior.
        }
        // CASO 2: Se arrastra un NextStatement (this) para conectar a un PrevStatement (otherConnection)
        // Ejemplo: [Bloque B]<PREV arrastrado> --> [Bloque A]<NEXT> <--NO, ESTO ES EL CASO 1
        // Ejemplo Correcto: [Bloque B]<NEXT arrastrado> --> [Bloque A]<PREV>
        // El bloque arrastrado (B) se conecta encima del estacionario (A).
        else if (this.Type == EConnection.NextStatement && otherConnection.Type == EConnection.PrevStatement)
        {
            actualSuperiorConn = otherConnection; // otherConnection (A.Prev) es el socket superior.
            actualInferiorConn = this;            // this (B.Next) es el enchufe inferior.
        }
        // CASO 3: Se arrastra un OutputValue (this) para conectar a un InputValue (otherConnection)
        // Ejemplo: [Input A]<INPUT> <-- [Output B]<OUTPUT arrastrado>
        else if (this.Type == EConnection.OutputValue && otherConnection.Type == EConnection.InputValue)
        {
            actualSuperiorConn = otherConnection; // otherConnection (InputA.Input) es el socket.
            actualInferiorConn = this;            // this (OutputB.Output) es el enchufe.
        }
        // CASO 4: Se arrastra un InputValue (this) para conectar a un OutputValue (otherConnection)
        // Ejemplo: [Output A]<OUTPUT> <-- [Input B]<INPUT arrastrado>
        
        else if (this.Type == EConnection.InputValue && otherConnection.Type == EConnection.OutputValue)
        {
            // En este escenario, 'this' (el Input arrastrado) es el "socket" que quiere recibir.
            // 'otherConnection' (el Output estacionario) es el "enchufe" que se conectará.
         
            actualSuperiorConn = this;                // El InputValue arrastrado es el que define el slot.
            actualInferiorConn = otherConnection;     // El OutputValue estacionario es el que se enchufa.
                                                     
        }
        else
        {
            Debug.LogError($"[CM.Connect] ABORTED: Incompatible or unhandled connection types for establishing superior/inferior. Self(Arrastrado): {GetConnectionModelID(this)} ({this.Type}), Other(Estacionario): {GetConnectionModelID(otherConnection)} ({otherConnection.Type})");
            return;
        }

        if (actualSuperiorConn == null || actualInferiorConn == null)
        {
            Debug.LogError($"[CM.Connect] ABORTED: Failed to assign actualSuperiorConn or actualInferiorConn. THIS(Dragged): {GetConnectionModelID(this)}, OTHER(Stationary): {GetConnectionModelID(otherConnection)}");
            return;
        }

        //Logger.Debug($"[CM.Connect] Roles Resolved -> Superior (Socket/Stationary in most cases): {GetConnectionModelID(actualSuperiorConn)}, Inferior (Plug/Dragged in most cases): {GetConnectionModelID(actualInferiorConn)}");
        ActualConnect(actualSuperiorConn, actualInferiorConn);
    }


    private static void ActualConnect(ConnectionModel superiorConn, ConnectionModel inferiorConn)
    {
        if (superiorConn == null || inferiorConn == null)
        {
            Debug.LogError($"[CM.ActualConnect] ABORTED: superiorConn or inferiorConn is null. Sup: {GetConnectionModelID(superiorConn)}, Inf: {GetConnectionModelID(inferiorConn)}");
            return;
        }

        BlockModel superiorBlock = superiorConn.SourceBlock;
        BlockModel inferiorBlock = inferiorConn.SourceBlock;

        if (superiorBlock == null || inferiorBlock == null)
        {
            Debug.LogError($"[CM.ActualConnect] ABORTED: SuperiorBlock ({superiorBlock?.ID ?? "NULL"}) or InferiorBlock ({inferiorBlock?.ID ?? "NULL"}) is null.");
            return;
        }

        // --- PRE-CONNECTION CLEANUP & ORPHAN/SHADOW MANAGEMENT ---

        if (inferiorConn.IsConnected)
        {
            Debug.LogWarning($"[CM.ActualConnect] Inferior connection ({GetConnectionModelID(inferiorConn)}) is already connected to ({GetConnectionModelID(inferiorConn.TargetConnection)}). Disconnecting it first. This might indicate complex reconnection.");
            inferiorConn.Disconnect();
        }

        BlockModel oldBlockConnectedToSuperior = null;
        ConnectionModel connectionOnOldBlock = null;
        XmlNode shadowDomFromSuperiorSlot = superiorConn.ShadowDom;

        if (superiorConn.IsConnected)
        {
            oldBlockConnectedToSuperior = superiorConn.TargetBlock;
            connectionOnOldBlock = superiorConn.TargetConnection;

            Debug.Log($"[CM.ActualConnect] Superior connection ({GetConnectionModelID(superiorConn)}) is ALREADY CONNECTED to ({GetConnectionModelID(connectionOnOldBlock)} on block '{oldBlockConnectedToSuperior?.ID}'). Disconnecting them first.");

            superiorConn.Disconnect();

            if (oldBlockConnectedToSuperior != null && oldBlockConnectedToSuperior.IsShadow)
            {
                Debug.Log($"  [CM.ActualConnect] The 'oldBlockConnectedToSuperior' ({oldBlockConnectedToSuperior.ID}) was a SHADOW. Disposing it.");
                oldBlockConnectedToSuperior.Dispose();
                oldBlockConnectedToSuperior = null;
                connectionOnOldBlock = null;
            }
        }
        else if (superiorConn.ShadowDom != null && inferiorBlock != null && !inferiorBlock.IsShadow)
        {
            Debug.Log($"[CM.ActualConnect] Superior slot ({GetConnectionModelID(superiorConn)}) was empty but had a ShadowDOM. Connecting a NON-SHADOW block ({inferiorBlock.ID}). Removing original shadow from slot.");
            superiorConn.ShadowDom = null;
        }

        // --- REALIZAR LA CONEXIÓN PRINCIPAL ---
        Debug.Log($"[CM.ActualConnect] Connecting Superior: {GetConnectionModelID(superiorConn)} WITH Inferior: {GetConnectionModelID(inferiorConn)}");
        ConnectionModel.ConnectReciprocally(superiorConn, inferiorConn);

        if (superiorConn.Type == EConnection.InputValue || superiorConn.Type == EConnection.NextStatement)
        {
            if (inferiorBlock.IsShadow)
            {
                Debug.Log($"  [CM.ActualConnect] Inferior block ({inferiorBlock.ID}) is a SHADOW. Assigning its XML to Superior Connection's ShadowDOM ({GetConnectionModelID(superiorConn)}).");
                superiorConn.ShadowDom = Xml.BlockToDom(inferiorBlock);
            }
            else
            {
                superiorConn.ShadowDom = null;
            }
        }

        // --- MANEJO POST-CONEXIÓN: BUMP (ORPHAN RE-ATTACH) Y JERARQUÍA ---

        if (oldBlockConnectedToSuperior != null && connectionOnOldBlock != null)
        {
            Debug.Log($"  [CM.ActualConnect] Managing 'oldBlockConnectedToSuperior' ({oldBlockConnectedToSuperior.ID}) of type {connectionOnOldBlock.Type}.");

            // CASO A: BUMP PARA STATEMENTS (HACIA ABAJO)
            // A.Next -> C.Prev (recién hecho), intentamos conectar C_final.Next -> B.Prev
            if (superiorConn.Type == EConnection.NextStatement &&   // Conexión de A (slot estacionario) es Next
                inferiorConn.Type == EConnection.PrevStatement &&   // Conexión de C (nuevo, arrastrado) es Prev
                connectionOnOldBlock.Type == EConnection.PrevStatement) // Conexión de B (viejo, que estaba debajo de A) era Prev
            {
                BlockModel lastInNewInferiorChain = inferiorBlock;
                while (lastInNewInferiorChain.NextConnection != null &&
                       lastInNewInferiorChain.NextBlock != null &&
                       !lastInNewInferiorChain.NextBlock.IsShadow)
                {
                    lastInNewInferiorChain = lastInNewInferiorChain.NextBlock;
                }

                ConnectionModel nextSlotOnNewChain = lastInNewInferiorChain.NextConnection;
                ConnectionModel prevToBump = connectionOnOldBlock;

                if (nextSlotOnNewChain != null && prevToBump != null &&
                    nextSlotOnNewChain.CheckType(prevToBump) &&
                    nextSlotOnNewChain.CanConnectWithReason(prevToBump) == ConnectionModel.CAN_CONNECT)
                {
                    Debug.Log($"    [CM.ActualConnect] BUMP (DOWNWARDS Statement): Reconnecting old block '{oldBlockConnectedToSuperior.ID}' (via Prev: {GetConnectionModelID(prevToBump)}) to Next of new chain ({GetConnectionModelID(nextSlotOnNewChain)} on '{lastInNewInferiorChain.ID}').");
                    nextSlotOnNewChain.Connect(prevToBump);
                }
                else
                {
                    Debug.LogWarning($"    [CM.ActualConnect] BUMP FAILED (DOWNWARDS Statement): Could not reconnect old block '{oldBlockConnectedToSuperior.ID}'. Conditions: nextSlotOnNewChain Null? {nextSlotOnNewChain == null}, prevToBump Null? {prevToBump == null}, CheckType: {(nextSlotOnNewChain != null && prevToBump != null ? nextSlotOnNewChain.CheckType(prevToBump).ToString() : "N/A")}, CanConnectReason: {(nextSlotOnNewChain != null && prevToBump != null ? nextSlotOnNewChain.CanConnectWithReason(prevToBump).ToString() : "N/A")}. Old block is displaced.");
                    prevToBump.FireUpdate(UpdateState.BumpedAway);
                }
            }
            // ***** NUEVO BLOQUE INSERTADO *****
            // CASO B: BUMP PARA STATEMENTS (HACIA ARRIBA)
            // El slot superior estacionario era A.Prev (superiorConn), se insertó C.Next (inferiorConn).
            // El bloque B con B.Next (connectionOnOldBlock) estaba previamente conectado A.Prev (era el bloque encima de A).
            // Conexión principal ACTUAL: C.Next -> A.Prev.
            // Queremos conectar: B.Next -> C_inicial.Prev (donde C_inicial es inferiorConn.SourceBlock).
            else if (superiorConn.Type == EConnection.PrevStatement &&   // Conexión de A (slot estacionario) es Prev
                     inferiorConn.Type == EConnection.NextStatement &&   // Conexión de C (nuevo, arrastrado) es Next
                     /*connectionOnOldBlock != null && */ connectionOnOldBlock.Type == EConnection.NextStatement) // Conexión de B (viejo, el que estaba encima de A) era Next
                                                                                                                  // Nota: connectionOnOldBlock != null ya está cubierto por el if externo.
            {
                BlockModel blockThatWasAbove = oldBlockConnectedToSuperior; // B
                ConnectionModel nextOfBlockThatWasAbove = connectionOnOldBlock; // B.Next

                // 'inferiorBlock' (C) es el bloque superior de la nueva cadena que se está insertando.
                // Necesitamos conectar el PreviousConnection del inferiorBlock (C.Prev) con el nextOfBlockThatWasAbove (B.Next).
                // La conexión a establecer es B.Next -> C.Prev
                ConnectionModel prevSlotOnNewInsertedBlock = inferiorConn.SourceBlock.PreviousConnection; // C.Prev

                if (prevSlotOnNewInsertedBlock != null && nextOfBlockThatWasAbove != null &&
                    nextOfBlockThatWasAbove.CheckType(prevSlotOnNewInsertedBlock) && // Valida compatibilidad de tipos
                    nextOfBlockThatWasAbove.CanConnectWithReason(prevSlotOnNewInsertedBlock) == ConnectionModel.CAN_CONNECT) // Valida compatibilidad lógica
                {
                    Debug.Log($"    [CM.ActualConnect] BUMP (UPWARDS Statement): Reconnecting old block '{blockThatWasAbove.ID}' (via Next: {GetConnectionModelID(nextOfBlockThatWasAbove)}) to Previous of new block ({GetConnectionModelID(prevSlotOnNewInsertedBlock)} on '{inferiorConn.SourceBlock.ID}').");
                    // (B.Next).Connect(C.Prev)
                    nextOfBlockThatWasAbove.Connect(prevSlotOnNewInsertedBlock);
                }
                else
                {
                    Debug.LogWarning($"    [CM.ActualConnect] BUMP FAILED (UPWARDS Statement): Could not reconnect old block '{blockThatWasAbove.ID}'. Conditions: prevSlotOnNewInsertedBlock Null? {prevSlotOnNewInsertedBlock == null}, nextOfBlockThatWasAbove Null? {nextOfBlockThatWasAbove == null}, CheckType: {(nextOfBlockThatWasAbove != null && prevSlotOnNewInsertedBlock != null ? nextOfBlockThatWasAbove.CheckType(prevSlotOnNewInsertedBlock).ToString() : "N/A")}, CanConnectReason: {(nextOfBlockThatWasAbove != null && prevSlotOnNewInsertedBlock != null ? nextOfBlockThatWasAbove.CanConnectWithReason(prevSlotOnNewInsertedBlock).ToString() : "N/A")}. Old block is displaced.");
                    if (nextOfBlockThatWasAbove != null) nextOfBlockThatWasAbove.FireUpdate(UpdateState.BumpedAway); else Debug.LogError("[CM.ActualConnect] BUMP UPWARDS FAILED: nextOfBlockThatWasAbove was null, this should not happen if connectionOnOldBlock was not null and of type NextStatement.");
                }
            }
            // ***** FIN DEL NUEVO BLOQUE INSERTADO *****

            // CASO C: RE-ATTACH PARA VALUE INPUTS (Anteriormente CASO B)
            // A.Input -> C.Output (recién hecho). El B.Output (connectionOnOldBlock) estaba antes en A.Input.
            // Intentamos conectar B.Output a algún Input disponible en la cadena de C (inferiorBlock).
            else if (superiorConn.Type == EConnection.InputValue &&     // Conexión de A (slot estacionario)
                     inferiorConn.Type == EConnection.OutputValue &&   // Conexión de C (nuevo, arrastrado)
                     connectionOnOldBlock.Type == EConnection.OutputValue) // Conexión de B (viejo)
            {
                ConnectionModel connectionTargetForOrphan = ConnectionModel.LastConnectionInRow(inferiorBlock, oldBlockConnectedToSuperior);
                if (connectionTargetForOrphan != null &&
                    connectionTargetForOrphan.SourceBlock != superiorBlock &&
                    connectionOnOldBlock.CanConnectWithReason(connectionTargetForOrphan) == ConnectionModel.CAN_CONNECT)
                {
                    Debug.Log($"    [CM.ActualConnect] RE-ATTACH (Value): Reconnecting old block '{oldBlockConnectedToSuperior.ID}' (Output: {GetConnectionModelID(connectionOnOldBlock)}) to slot found by LastConnectionInRow ({GetConnectionModelID(connectionTargetForOrphan)} on '{connectionTargetForOrphan.SourceBlock?.ID}').");
                    connectionOnOldBlock.Connect(connectionTargetForOrphan);
                }
                else
                {
                    Debug.LogWarning($"    [CM.ActualConnect] RE-ATTACH FAILED (Value): Could not find slot for old block '{oldBlockConnectedToSuperior.ID}' via LastConnectionInRow. Old block is displaced.");
                    connectionOnOldBlock.FireUpdate(UpdateState.BumpedAway);
                }
            }
            // OTROS CASOS: El bloque "huérfano" simplemente se desplaza.
            else
            {
                Debug.LogWarning($"  [CM.ActualConnect] Old block '{oldBlockConnectedToSuperior.ID}' was connected, but no specific bump/reattach logic for combination SupType: {superiorConn.Type} / InfType: {inferiorConn.Type} / OldBlockConnType: {connectionOnOldBlock.Type}. Old block is displaced.");
                connectionOnOldBlock.FireUpdate(UpdateState.BumpedAway);
            }
        }

        // 2. Configurar el parentesco jerárquico.
        BlockModel parentToSetForInferior = null;
        bool setHierarchicalParent = false;

        if (superiorConn.Type == EConnection.InputValue && inferiorConn.Type == EConnection.OutputValue)
        {
            parentToSetForInferior = superiorBlock;
            setHierarchicalParent = true;
            Debug.Log($"  [CM.ActualConnect] HIERARCHY (Value): Child Output '{inferiorBlock.ID}' into Parent Input '{superiorConn.Input?.Name ?? "N/A"}' of '{superiorBlock.ID}'. Attempting to set parent.");
        }
        else if (superiorConn.Type == EConnection.NextStatement && inferiorConn.Type == EConnection.PrevStatement)
        {
            parentToSetForInferior = superiorBlock.ParentBlock;

            if (superiorConn.Input != null)
            {
                if (superiorBlock.ParentBlock != superiorConn.Input.SourceBlock)
                {
                    Debug.LogWarning($"  [CM.ActualConnect] HIERARCHY (Stack): Parent of superiorBlock ('{superiorBlock.ID}' -> parent '{superiorBlock.ParentBlock?.ID}') " +
                                     $"is NOT the source of its input ('{superiorConn.Input.SourceBlock?.ID}'). This might be an issue. Using Input.SourceBlock as target parent.");
                }
                parentToSetForInferior = superiorConn.Input.SourceBlock;
            }

            setHierarchicalParent = true;
            Debug.Log($"  [CM.ActualConnect] HIERARCHY (Stack): Child '{inferiorBlock.ID}' (Prev) after Parent '{superiorBlock.ID}' (Next). Attempting to set parent of child to '{parentToSetForInferior?.ID ?? "NULL (TopLevel)"}'.");
        }

        if (setHierarchicalParent)
        {
            if (inferiorBlock.ParentBlock != parentToSetForInferior)
            {
                try
                {
                    inferiorBlock.SetParent(parentToSetForInferior);
                }
                catch (Exception e)
                {
                    Debug.LogError($"  [CM.ActualConnect] ERROR during inferiorBlock.SetParent! Child: {inferiorBlock.ID}, TargetParent: {parentToSetForInferior?.ID ?? "NULL"}. Exception: {e.Message}\n  REVERTING PRIMARY CONNECTION.");
                    ConnectionModel.DisconnectReciprocally(superiorConn, inferiorConn);
                    superiorConn.FireUpdate(UpdateState.ConnectionFailed);
                    inferiorConn.FireUpdate(UpdateState.ConnectionFailed);
                    throw;
                }
            }
            else
            {
                Debug.Log($"  [CM.ActualConnect] HIERARCHY: Parent of inferiorBlock ({inferiorBlock.ID}) is already {parentToSetForInferior?.ID ?? "NULL"}. No change needed.");
            }
        }

        // --- ACTUALIZAR ESTADO Y DISPARAR EVENTOS ---
        Debug.Log($"[CM.ActualConnect] === Connection SUCCESSFUL. Firing Connection Updates for Sup: {GetConnectionModelID(superiorConn)}, Inf: {GetConnectionModelID(inferiorConn)} ===");

        superiorConn.m_UpdateState = UpdateState.Connected;
        inferiorConn.m_UpdateState = UpdateState.Connected;

        superiorConn.FireUpdate(UpdateState.Connected);
        inferiorConn.FireUpdate(UpdateState.Connected);
    }


    public static void DisconnectReciprocally(ConnectionModel c1, ConnectionModel c2)
    {
        if (c1 != null && c1.TargetConnection == c2)
        { 
            c1.TargetConnection = null; 
        }
        if (c2 != null && c2.TargetConnection == c1) 
        { 
            c2.TargetConnection = null; 
        }
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
        string connIdForLog = GetConnectionModelID(this); // Genera ID antes de que SB pueda cambiar

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
        // Logger.Log($"[CM.IsConnectionAllowed] Checking Self: {GetConnectionModelID(this)} ({this.Type}) VS Candidate: {GetConnectionModelID(candidate)} ({candidate?.Type}), MaxRadius: {maxRadius}");

        if (candidate == null)
        {
            Logger.Log("        -> FAILED: Candidate is NULL.");
            return false;
        }

        //Chequeo de compatibilidad fundamental (tipos, checks, etc.)
        int reason = this.CanConnectWithReason(candidate); 
        if (reason != ConnectionModel.CAN_CONNECT)
        {
            string reasonStr = "Unknown (" + reason + ")"; // Proporciona el número si no está en el switch
            switch (reason)
            {
                case REASON_SELF_CONNECTION: reasonStr = "Self Connection"; break;
                case REASON_WRONG_TYPE: reasonStr = "Wrong Type"; break;
                // REASON_TARGET_NULL ya no aplica aquí directamente, se maneja con candidate == null.
                case REASON_CHECKS_FAILED: reasonStr = "Checks Failed"; break;
                case REASON_DIFFERENT_WORKSPACES: reasonStr = "Different Workspaces"; break;
                case REASON_SHADOW_PARENT: reasonStr = "Shadow Parent Issue"; break; 
            }
            Logger.Log($"        -> FAILED (CanConnectWithReasonTo): {reasonStr}. Self: {this.Type}, Candidate: {candidate.Type}");
            return false;
        }
        // Logger.Log("        - Passed: Basic connection reasons (Type, Workspace, Checks, etc.) OK.");

        // Chequeo de bucles (conectar un bloque a uno de sus propios hijos)
        //    SB es el bloque al que pertenece esta conexión (arrastrada)
        //    candidate.SB es el bloque al que pertenece la conexión candidata (esta fija o estacionaria)
        if (this.SourceBlock != null && candidate.SourceBlock != null) // Evitar NullRef si alguna conexión no tiene SB
        {
            BlockModel parentOfCandidate = candidate.SourceBlock.ParentBlock;
            while (parentOfCandidate != null)
            {
                if (parentOfCandidate == this.SourceBlock)
                {
                    Logger.Log("        -> FAILED: Connection would create a loop (candidate is child of self's source block).");
                    return false;
                }
                parentOfCandidate = parentOfCandidate.ParentBlock;
            }
            // También verificar si el bloque que se arrastra es padre del candidato
            BlockModel parentOfSelf = this.SourceBlock.ParentBlock;
            while (parentOfSelf != null)
            {
                if (parentOfSelf == candidate.SourceBlock)
                {
                    Logger.Log("        -> FAILED: Connection would create a loop (self's source block is child of candidate's).");
                    return false;
                }
                parentOfSelf = parentOfSelf.ParentBlock;
            }
        }
        // Logger.Log("        - Passed: No loop detected.");

        // Lógica para manejar si la conexión candidata esta ocupada.
        //    'this' es la conexión del bloque que se arrastra.
        //    'candidate' es la conexión del bloque estacionario.
        if (candidate.IsConnected)
        {
            // CASO A: Conexiones de Statement (Previous/Next) -> permitir bump
            // Si candidate.Next está ocupado, se empuja.
            if (this.Type == EConnection.PrevStatement && candidate.Type == EConnection.NextStatement)
            {
                // candidate.SB es el bloque superior de la pila existente.
                // candidate (NextStatement) ya está conectado a candidate.TargetConnection (PrevStatement de otro bloque).
                // Es correcto empujar el bloque de abajo.
               // Logger.Log($"        - Candidate '{GetConnectionModelID(candidate)}' ({candidate.Type}) is occupied. BUT this is '{GetConnectionModelID(this)}' ({this.Type}). Allowing potential BUMP downwards.", this.SourceBlock?.BlockView?.gameObject);
                
            }
            // Intentamos conectar el NEXT de nuestro bloque arrastrado (this) al PREVIOUS del bloque estacionario (candidate).
            // //Si candidate.Previous está ocupado, se empuja.
            else if (this.Type == EConnection.NextStatement && candidate.Type == EConnection.PrevStatement)
            {
                // candidate.SB es el bloque inferior de la pila existente.
                // candidate (PrevStatement) ya está conectado a candidate.TargetConnection (NextStatement de otro bloque).
                // Es correcto empujar el bloque de arriba.
              //  Logger.Log($"        - Candidate '{GetConnectionModelID(candidate)}' ({candidate.Type}) is occupied. BUT this is '{GetConnectionModelID(this)}' ({this.Type}). Allowing potential BUMP upwards.", this.SourceBlock?.BlockView?.gameObject);
                
            }

            // CASO B: Conexiones de Valor (Input/Output) -> PERMITIR REEMPLAZO (con condiciones)
            // Intentando conectar nuestro OUTPUT (this) a un INPUT (candidate) que ya tiene algo.
            else if (this.Type == EConnection.OutputValue && candidate.Type == EConnection.InputValue)
            {
                // candidate (InputValue) ya está conectado a candidate.TargetConnection (otro OutputValue).
                // Podemos reemplazarlo excepto que el bloque conectado sea inmóvil.
                if (candidate.TargetBlock != null && !candidate.TargetBlock.Movable && !candidate.TargetBlock.IsShadow)
                {
                   Logger.Log($"        -> FAILED: Candidate (InputValue) '{GetConnectionModelID(candidate)}' is connected to an IMMOVABLE, non-shadow block '{candidate.TargetBlock.ID}'. Cannot replace.");
                    return false;
                }
               // Logger.Log($"        - Candidate (InputValue) '{GetConnectionModelID(candidate)}' is occupied. Allowing potential REPLACEMENT of '{GetConnectionModelID(candidate.TargetConnection)}'.", this.SourceBlock?.BlockView?.gameObject);
            }
            // Intentando conectar nuestro INPUT (this) a un OUTPUT (candidate).
            else if (this.Type == EConnection.InputValue && candidate.Type == EConnection.OutputValue)
            {
                if (this.IsConnected) // Si el input del bloque que estoy arrastrando ya está ocupado
                {
                    Logger.Log($"        -> FAILED: Self (InputValue) '{GetConnectionModelID(this)}' is already connected to '{GetConnectionModelID(this.TargetConnection)}'. Cannot connect new OutputValue '{GetConnectionModelID(candidate)}'.");
                    return false;
                }
 
                Logger.Log($"        - Candidate (OutputValue) '{GetConnectionModelID(candidate)}' might be connected, but self InputValue '{GetConnectionModelID(this)}' is free. Allowing.", this.SourceBlock?.BlockView?.gameObject);
            }
            // CASO C: Cualquier otro tipo de conexión donde el candidato esté ocupado y no sea bump/reemplazo manejado.
            else
            {
                Logger.Log($"        -> FAILED: Candidate '{GetConnectionModelID(candidate)}' ({candidate.Type}) is occupied by '{GetConnectionModelID(candidate.TargetConnection)}', and no bump/replace logic for this combination. Self: '{GetConnectionModelID(this)}' ({this.Type}).");
                return false;
            }
        }
        // Logger.Log("        - Passed: Candidate connection state checks (or bump/replace allowed).");

        // Chequeo de Distancia
        if (maxRadius > 0) // Solo chequeamos si maxRadius es un valor significativo
        {
            float dist = this.DistanceFrom(candidate);
            // Logger.Log($"        - Checking Distance: Calculated={dist}, MaxRadius={maxRadius}");
            if (dist > maxRadius)
            {
                // Logger.Log($"        -> FAILED: Distance ({dist}) exceeds MaxRadius ({maxRadius}).");
                return false;
            }
            // Logger.Log("        - Passed: Distance check.");
        }

        Logger.Log($"        -> SUCCESS: Connection Allowed between {GetConnectionModelID(this)} and {GetConnectionModelID(candidate)}.");
        return true;
    }

    public static void ConnectReciprocally(ConnectionModel first, ConnectionModel second)
    {
        Debug.Log($"<color=lightblue>ConnectReciprocally:</color> Setting target for First ({GetConnectionModelID(first)}) to Second ({GetConnectionModelID(second)})");

        if (first == null || second == null)
            throw new Exception("Cannot connect null connections.");
       // Debug.Log($"  - BEFORE: first.TargetConnection = {GetConnectionModelID(first.TargetConnection)}");
        first.TargetConnection = second;
      //  Debug.Log($"  - First's TargetConnection is now: {GetConnectionModelID(first.TargetConnection)}");

       // Debug.Log($"  - BEFORE: second.TargetConnection = {GetConnectionModelID(second.TargetConnection)}");

        second.TargetConnection = first;
     //   Debug.Log($"  - Second's TargetConnection is now: {GetConnectionModelID(second.TargetConnection)}");

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
    
    public  void Disconnect() // Hacerla virtual si aún no lo es
    {
        if (!IsConnected) return;

        ConnectionModel oldTarget = TargetConnection;
        // Logger.Log($"[CM.Disconnect] Disconnecting '{ID}' from '{oldTarget?.ID}'");

        // Romper la conexión en ambos lados ANTES de disparar eventos para evitar recursiones o estados inconsistentes.
        this.TargetConnection = null;
        if (oldTarget != null)
        {
            oldTarget.TargetConnection = null;
        }

        // Notificar a las vistas
        UpdateState oldState = m_UpdateState; // Guarda el estado antes del cambio
        m_UpdateState = UpdateState.Disconnected;
        if (oldState == UpdateState.Connected)
        { // Solo dispara si realmente estaba conectado
            FireUpdate(m_UpdateState); // Notifica a mi vista
        }

        if (oldTarget != null)
        {
            UpdateState oldTargetState = oldTarget.m_UpdateState;
            oldTarget.m_UpdateState = UpdateState.Disconnected;
            if (oldTargetState == UpdateState.Connected)
            {
                oldTarget.FireUpdate(oldTarget.m_UpdateState); // Notifica a la vista del otro
            }
        }
        // Cualquier limpieza adicional de ShadowDOM si se manejaba directamente en Disconnect.
        // if (this.ShadowDom != null && this.TargetConnection == null /*asegurarse de que está desconectado y no reconectando a otro shadow*/)
        // {
        //     Debug.LogWarning($"Disconnecting {this.ID} which has a shadow. Shadow may need explicit disposal if it becomes an orphan.");
        //     // La lógica de eliminar shadows huérfanos podría estar en otro lado (e.g., cuando el bloque dueño se actualiza)
        // }
    }

    private void DisconnectInternal(ConnectionModel childConnection)
    {
        /* var otherConnection = this.TargetConnection;
         otherConnection.TargetConnection = null;
         this.TargetConnection = null;
         childConnection.SourceBlock.SetParent(null);*/
        bool wasHierarchical = false;
        if (this.Input != null && // conexión perteneciente a un InputModel
            this.Input.Type == EConnection.InputValue && // InputModel  para un valor
            this.Type == EConnection.InputValue &&
            childConnection.Type == EConnection.OutputValue)
        {
            wasHierarchical = true;
        }
        else if (this.Input != null && //  conexión perteneciente a un InputModel
                 this.Input.Type == EConnection.NextStatement && //  InputModel  para una pila de sentencias
                 childConnection.Type == EConnection.PrevStatement)
        {
            wasHierarchical = true;
        }

        if (wasHierarchical)
        {
            if (childConnection.SourceBlock != null && childConnection.SourceBlock.ParentBlock == this.SourceBlock)
            { // Solo si este era el padre
                Debug.Log($"DisconnectInternal: Clearing ParentBlock of Child ({childConnection.SourceBlock.ID}) from Parent ({this.SourceBlock?.ID}) because it was hierarchically connected via {this.Input.Name} ({this.Input.Type}).");
                childConnection.SourceBlock.SetParent(null);
            }
            else if (childConnection.SourceBlock != null)
            {
                // Debug.Log($"DisconnectInternal: Hierarchical-like disconnection but ParentBlock of Child ({childConnection.SourceBlock.ID}) was not this.SourceBlock ({this.SourceBlock?.ID}) but '{childConnection.SourceBlock.ParentBlock?.ID}'. No SetParent(null) call.");
            }
        }
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
