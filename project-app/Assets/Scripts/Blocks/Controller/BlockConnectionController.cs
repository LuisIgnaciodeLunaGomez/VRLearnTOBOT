/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 25/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Coordina la detección y validación de conexiones entre vistas y modelos durante el arrastre.
 * 
 * TODO: REVISAR si la disposición del drag and drop generado guarda las conexiones en la base de datos ya que se trabaja con dos paneles y practicamente estamos en el DragLayer
 * aunque si deposito el bloque en el CodingAreaPanel tendrá otra localización por lo que habrá que revisar que se guardan dos conexiones pero claramente identificada para que no se
 * mezclen entre los paneles. Hay que revisar como se cogen las conexiones y almacenar cada conexíón en la base de datos y depurarlas para verlas en los logs panel y location.
 */

using System;
using UnityEngine;
using System.Collections.Generic;

public class BlockConnectionController : MonoBehaviour
{
    public static BlockConnectionController Instance { get; private set; }

    private WorkSpaceModel _workspace;
    private WorkSpaceModel m_Workspace
    {
        get { return _workspace; }
        set
        {
           
           // Debug.LogError($"<color=red>HASHCODE_CHECK - BlockConnectionController - m_Workspace Setter Called!");
            Debug.LogError($"  -> Current Value HashCode: {_workspace?.GetHashCode()}");
            Debug.LogError($"  -> New Value HashCode Attempting to Set: {value?.GetHashCode()}");

            _workspace = value; 
        }
    }
    private WorkSpaceView m_WorkspaceView;
    private BlockDragController m_BlockDragController;

    private ConnectionModel m_CurrentBestTargetConnection = null;
    private ConnectionModel m_CurrentSourceCandidate = null;


    //Para depuración

    private bool _hasLoggedDbSortThisDrag = false;


    private void Awake()
    {
      //  Debug.LogError("<color=red>HASHCODE_CHECK - MiControlador - AWAKE - HashCode(this): " + this.GetHashCode());
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void InitializeController(WorkSpaceModel workspace, WorkSpaceView workspaceView, BlockDragController blockDragController)
    { 
        m_Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        m_WorkspaceView = workspaceView ?? throw new ArgumentNullException(nameof(workspaceView));
        m_BlockDragController = blockDragController ?? throw new ArgumentNullException(nameof(blockDragController));

        // Debug.Log("ConnectionController Initialized");

       // Debug.LogError($"<color=red>HASHCODE_CHECK - BlockConnectionController Initialize - Received/Stored Workspace HashCode: {m_Workspace?.GetHashCode()}");

    }

    public void ProcessDrag(BlockModel draggingBlock, List<ConnectionModel> draggingConnections, Vector2 dragginblockBaseLogicalPosition)
    {
        //Debugging

        if (!_hasLoggedDbSortThisDrag)
        {
            Debug.Log($"===== DEBUGGING DB SORT (Once per Drag for Workspace: {m_Workspace?.Id}) =====");
            if (m_Workspace != null && m_Workspace.ConnectionDBList != null)
            {
                // Comprueba las DBs relevantes para conexiones de Statement
                if (m_Workspace.ConnectionDBList.TryGetValue(EConnection.PrevStatement, out var prevDb))
                {
                    Debug.Log($"Initial Check - PrevStatement DB Count: {prevDb.Count}");
                    prevDb.Debug_LogSortOrder("PrevStatement");
                }
                else { Debug.LogWarning("PrevStatement DB not found."); }

                if (m_Workspace.ConnectionDBList.TryGetValue(EConnection.NextStatement, out var nextDb))
                {
                    Debug.Log($"Initial Check - NextStatement DB Count: {nextDb.Count}");
                    nextDb.Debug_LogSortOrder("NextStatement");
                }
                else { Debug.LogWarning("NextStatement DB not found."); }

                
                // if (m_Workspace.ConnectionDBList.TryGetValue(EConnection.InputValue, out var inDb)) { inDb.Debug_LogSortOrder("InputValue"); }
                // if (m_Workspace.ConnectionDBList.TryGetValue(EConnection.OutputValue, out var outDb)) { outDb.Debug_LogSortOrder("OutputValue"); }
            }
            else
            {
                Debug.LogWarning("Cannot debug DB sort: Workspace or ConnectionDBList is null.");
            }
            _hasLoggedDbSortThisDrag = true; // Marcar como hecho para este arrastre
            Debug.Log($"===== END DEBUGGING DB SORT =====");
        }

        //Debug.Log($"[ProcessDrag ENTRY] Dragging: {draggingBlock?.ID}, Num Conns: {draggingConnections?.Count}, BaseLogicalPos: {dragginblockBaseLogicalPosition}"); 
        if (draggingBlock == null || draggingConnections == null || m_Workspace == null)
        {
            Debug.LogWarning("[ProcessDrag EXIT] Null argument.");
            return;
        }

        // Guardamos el target actual para saber si cambia y actualizar highlight
        ConnectionModel oldBestTarget = m_CurrentBestTargetConnection;

        //Busco la mejor conexión
        ConnectionModel bestTarget = null;
        ConnectionModel sourceCandidate = null;

        float m_ConnectionSnapDistance = BlockViewSettings.Instance.ConnectionSnapDistance;
        float m_ConnectionSearchRadius = BlockViewSettings.Instance.ConnectionSearchRange;

       // Debug.Log($" - Search Settings: SearchRadius={m_ConnectionSearchRadius}, SnapDistance={m_ConnectionSnapDistance}");

        float closestRadiusSq = m_ConnectionSnapDistance * m_ConnectionSnapDistance; //Calculo la distancia cuadrada

        //Debug.Log($"ConnectionController.ProcessDrag: Dragging block {draggingBlock.ID} ({draggingBlock.Type})");
        //  Debug.Log($"  Dragging Block Model Logical XY: ({draggingBlock.XY.x:F2}, {draggingBlock.XY.y:F2})");
        // Debug.Log($"  Current Best Target (Before Search): {ConnectionModel.GetConnectionModelID(m_CurrentBestTargetConnection)}");
        // Debug.Log($"  Search Radius: {m_ConnectionSearchRadius}, Snap Distance: {m_ConnectionSnapDistance}");

        //Debugging
        //Debug.Log($"[ProcessDrag] Received {draggingConnections.Count} connections to process:");
        for (int i = 0; i < draggingConnections.Count; i++)
        {
            //Debug.Log($"  - Conn[{i}]: {ConnectionModel.GetConnectionModelID(draggingConnections[i])}, Has DBOpposite: {draggingConnections[i]?.DBOpposite != null}");
        }

        foreach (ConnectionModel myConn in draggingConnections)
        {
            //Debug.Log($"--->>> Starting processing loop for: {ConnectionModel.GetConnectionModelID(myConn)}");
            if (myConn == null )
            {
              //  Debug.LogError("XXXXXXXX ERROR: Encountered NULL connection in draggingConnections list.");
               // Debug.Log("  - Skipping NULL connection in draggingConnections list.");
                continue;
            }


            if (myConn.DBOpposite == null)

            {
               // Debug.LogError($"XXXXXXXX ERROR: DBOpposite is NULL for connection {ConnectionModel.GetConnectionModelID(myConn)}! Cannot search.");

               /// Debug.Log($"  - Skipping connection {ConnectionModel.GetConnectionModelID(myConn)}: DBOpposite is NULL.");
                continue;
            }
          //  Debug.Log($"--->>> Finished processing loop for: {ConnectionModel.GetConnectionModelID(myConn)}");
            // Debug.Log($"  Processing DRAGGING Connection: {ConnectionModel.GetConnectionModelID(myConn)} at Location {myConn.Location}");

            // Debug.Log($"    Calling SearchForClosest on DB: {myConn.OppositeType}...");

            ConnectionModel neighbour;

            float neighbourRadius;

            if (myConn.DBOpposite != null)
            {
                myConn.DBOpposite.Debug_LogSortOrder($"DBOpposite for {ConnectionModel.GetConnectionModelID(myConn)} (Type: {myConn.OppositeType})");
            }
            else
            {
                Debug.LogError($"Cannot check sort order, DBOpposite is NULL for {ConnectionModel.GetConnectionModelID(myConn)}");
            }

            //Busco en la BBDD de tipos opuestos conexiones cercanas
            myConn.DBOpposite.SearchForClosest(myConn, m_ConnectionSnapDistance, Vector2.zero /*dxy era Vector2.zero*/, out neighbour, out neighbourRadius);

           // Debug.Log($"    <- Search Result: Neighbour={ConnectionModel.GetConnectionModelID(neighbour)}, Radius={neighbourRadius}");

            if ((neighbour != null)) //Encontramos un vecino
            {
                Debug.Log($"    Found Neighbour: {ConnectionModel.GetConnectionModelID(neighbour)}. Checking IsConnectionAllowed...");
                //  float currentChechRadiusSq = m_ConnectionSearchRadius * m_ConnectionSearchRadius;

                if (myConn.IsConnectionAllowed(neighbour, m_ConnectionSnapDistance))
                {
                    Debug.Log($"      <color=lime>---> ALLOWED! Connection between {ConnectionModel.GetConnectionModelID(myConn)} and {ConnectionModel.GetConnectionModelID(neighbour)} is possible.</color>");

                   
                    float currentDistanceSq = neighbourRadius * neighbourRadius; 
                    if (bestTarget == null || currentDistanceSq < closestRadiusSq)
                    {
                       // Debug.Log($"      <color=yellow>>>>>>>>>> NEW BEST TARGET FOUND! <<<<<<<<<</color> RadiusSq: {currentDistanceSq}");
                        bestTarget = neighbour;       // Guardamos el nuevo mejor destino
                        sourceCandidate = myConn;   // Guardamos nuestra conexión correspondiente
                        closestRadiusSq = currentDistanceSq; // Actualizamos la distancia más cercana encontrada
                    }
                    else
                    {
                        Debug.Log($"      (Not closer than previous best target with RadiusSq {closestRadiusSq})");
                    }
                }

            }
        }

        //Actualizo el estado del contenido para realizar el drop
       // Debug.Log($"[ProcessDrag EXIT] Loop Finished. Final Best Target: {ConnectionModel.GetConnectionModelID(bestTarget)} (Previous: {ConnectionModel.GetConnectionModelID(oldBestTarget)})");
        m_CurrentBestTargetConnection = bestTarget;
        m_CurrentSourceCandidate = sourceCandidate;

        //Actualizao el resaltado visual a través de la vista correspondiente
        UpdateVisualHighlighting(oldBestTarget, m_CurrentBestTargetConnection);

        //Informo si he encontrado una conexión que se pueda llevar a cabo "snapable"
        if (m_CurrentBestTargetConnection != null)
        {
            Debug.Log($"<color=green>[ProcessDrag STATUS] Potential snap FOUND:</color> Source {ConnectionModel.GetConnectionModelID(m_CurrentSourceCandidate)} -> Target {ConnectionModel.GetConnectionModelID(m_CurrentBestTargetConnection)}");
        }
        else
        {
           // Debug.Log("ConnectionController: No potential snap target found.");
        }

    }

    /// <summary>
    /// Actualiza la conexión visualmente resaltando la nueva conexión y quitando el resaltado de la anterior.
    /// </summary>
    /// <param name="newBestTarget">La nueva conexión a resaltar.</param>
    /// <param name="oldBesTarget">La conexión anterior a la que se le quitará el resaltado.</param>
    private void UpdateVisualHighlighting(ConnectionModel oldBesTarget, ConnectionModel newBestTarget)
    {
        if (oldBesTarget != newBestTarget)
        {
            //Quitar el resaltado en la antiuga conexión
            if (oldBesTarget != null)
            {
                ConnectionView oldView = m_WorkspaceView.GetConnectionView(oldBesTarget);
                oldView?.Highlight(false);
            }

            //Resalto la nueva conexión
            if (newBestTarget != null)
            {
                ConnectionView newView = m_WorkspaceView.GetConnectionView(newBestTarget);

                if (newView != null) newView?.Highlight(true);
                else Debug.LogWarning($"ConnectionController: Could not find ConnectionView for new best target: {newBestTarget.SourceBlock.Type}:{newBestTarget.Type}. Cannot highlight.");
            }
        }

    }

    /// <summary>
    /// Devuelve si se realizo una conexión (snap) entre dos conexiones.
    public bool TryConnectAndPlace(BlockModel draggingBlock, bool isTemplateClone,/* bool overTrasBin,*/ Vector2 pointerScreenPosition)
    {
        Debug.Log($"<color=cyan>TryConnectAndPlace ENTERED.</color> Dragging: {draggingBlock?.ID} ({draggingBlock?.Type}). IsClone: {isTemplateClone}. ");
        Debug.Log($" - CurrentBestTarget (at entry): {ConnectionModel.GetConnectionModelID(m_CurrentBestTargetConnection)}"); 
        Debug.Log($" - CurrentSource (at entry): {ConnectionModel.GetConnectionModelID(m_CurrentSourceCandidate)}");

        if (draggingBlock == null || m_WorkspaceView == null || m_Workspace == null) return false;

        ConnectionModel finalTargetConnection = m_CurrentBestTargetConnection;
        ConnectionModel finalSourceConnection = m_CurrentSourceCandidate;

        Debug.Log($" - Using FinalTarget: {ConnectionModel.GetConnectionModelID(finalTargetConnection)}");
        Debug.Log($" - Using FinalSource: {ConnectionModel.GetConnectionModelID(finalSourceConnection)}");

        bool connected = false;

        //Elimino también el resaltado por si estuviera activo aún.
        UpdateVisualHighlighting(finalTargetConnection, null);

        //Limpio el estado del controlador para el siguiente drag 
        m_CurrentBestTargetConnection = null;
        m_CurrentSourceCandidate = null;

        //Si hay una conexión válidad  intento conectarmete
        if (finalTargetConnection != null && finalSourceConnection != null)
        {
           // Debug.Log($"ConnectionController: Attempting connection {finalSourceConnection.SourceBlock.Type}:{finalSourceConnection.Type} -> {finalTargetConnection.SourceBlock.Type}:{finalTargetConnection.Type}");
            Debug.Log($"  <color=lime>Attempting Connection:</color> Source {ConnectionModel.GetConnectionModelID(finalSourceConnection)} -> Target {ConnectionModel.GetConnectionModelID(finalTargetConnection)}");
            try
            {

                //Gestionamos la desconexión automática si el destino ya esta conectado y lo notificará a las vistas
                Debug.Log("    BEFORE Connect Call");
                finalTargetConnection.Connect(finalSourceConnection);
                Debug.Log("    AFTER Connect Call");
                connected = true;
                Debug.Log($"    --> Post-Connect State: Source '{ConnectionModel.GetConnectionModelID(finalSourceConnection)}' IsConnected={finalSourceConnection?.IsConnected}, TargetID='{ConnectionModel.GetConnectionModelID(finalSourceConnection?.TargetConnection)}'");
                Debug.Log($"    --> Post-Connect State: Target '{ConnectionModel.GetConnectionModelID(finalTargetConnection)}' IsConnected={finalTargetConnection?.IsConnected}, TargetID='{ConnectionModel.GetConnectionModelID(finalTargetConnection?.TargetConnection)}'");


                if (isTemplateClone)
                {
                    Debug.Log($"ConnectionController: Confirming add for template clone {draggingBlock.ID} due to successful connection.");
                    m_Workspace.AddBlock(draggingBlock);
                }

                if (draggingBlock.ParentBlock == null)
                {
                    //Debug.LogError($"<color=red>HASHCODE_CHECK - TryConnectAndPlace - Calling AddBlock on Workspace HashCode: {m_Workspace?.GetHashCode()}");
                    m_Workspace.AddBlock(draggingBlock);
                }
            }

            catch (Exception e)
            {

               // Debug.LogWarning($"ConnectionController: Connect failed: {e.Message}" /*, draggingBlock.gameObject)*/);
                Debug.LogError($"<color=red>Connect FAILED:</color> {e.ToString()}");
            }
        }

        //Si no se ha conectado, procedo a gestionar el drop en el basurero o en el espacio libre
        if (!connected)
        {
            // Verifico si el puntero está sobre el área de codificación válida.
            bool isOverCodingArea = m_BlockDragController.IsPointerOverArea(pointerScreenPosition, m_WorkspaceView.CodingArea);

            if (!isOverCodingArea)
            {
                Debug.LogWarning($"ConnectionController: Invalid drop location (outside CodingArea). Disposing block {draggingBlock.ID}");
                // Añadir traza para estar seguros de CÓMO se llegó aquí si da problemas
                try { Debug.LogError("DISPOSE CALLED from !isOverCodingArea path:\n" + Environment.StackTrace); } catch { }
                draggingBlock.Dispose(false); // <<--- El Dispose() que puede estar causando problemas

                Debug.Log($" - Returning FALSE because block {draggingBlock.ID} was disposed (invalid drop).");
                return false; // <<<--- El bloque no se colocó/conectó válidamente.
            }
            else
            {
                Debug.Log($"ConnectionController: Block {draggingBlock.ID} dropped in valid free space.");
                if (isTemplateClone)
                {
                    // Era un clon de plantilla soltado libremente -> añadirlo al modelo del workspace.
                    m_Workspace.AddBlock(draggingBlock);
                }
                else
                {
                    // Era un bloque existente movido libremente.
                   
                    Debug.Log($" - Existing block {draggingBlock.ID} moved freely. No AddBlock needed.");
                }

              
                BlockView finalView = m_WorkspaceView.GetBlockView(draggingBlock);
                if (finalView == null)
                {
                    Debug.LogError($"ConnectionController: After VALID drop, block model {draggingBlock.ID} exists but BlockView is NULL!");
                }
                else if (finalView.Block != draggingBlock)
                {
                    Debug.LogError($"ConnectionController: After VALID drop, BlockView {finalView.name} is bound to a different model {finalView.Block.ID}!");
                }
                // else { Debug.Log("   - Final view found and matches model."); }
            }

        
        }

      
        Debug.Log($"<color=cyan>TryConnectAndPlace EXITING - Returning TRUE (Operation Handled)</color>");
        return true;
    }

}//fin clase BlockController