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
using UnityEngine.UI;

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
         //   Debug.LogError($"  -> Current Value HashCode: {_workspace?.GetHashCode()}");
          //  Debug.LogError($"  -> New Value HashCode Attempting to Set: {value?.GetHashCode()}");

            _workspace = value; 
        }
    }
    private WorkSpaceView m_WorkspaceView;
    private BlockDragController m_BlockDragController;
    private BlockConnectionController m_WorkspaceController;
    private ConnectionModel m_CurrentBestTargetConnection = null;
    private ConnectionModel m_CurrentSourceCandidate = null;

    private ConnectionModel m_FinalizedBestTargetConnection = null;
    private ConnectionModel m_FinalizedSourceCandidate = null;

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

    public void ProcessDrag(BlockModel draggingBlock)//, List<ConnectionModel> draggingConnections, Vector2 dragginblockBaseLogicalPosition)
    {
        //Debugging

        if (!_hasLoggedDbSortThisDrag)
        {
           // Debug.Log($"===== DEBUGGING DB SORT (Once per Drag for Workspace: {m_Workspace?.Id}) =====");
            if (m_Workspace != null && m_Workspace.ConnectionDBList != null)
            {
                // Comprueba las DBs relevantes para conexiones de Statement
                if (m_Workspace.ConnectionDBList.TryGetValue(EConnection.PrevStatement, out var prevDb))
                {
                   // Debug.Log($"Initial Check - PrevStatement DB Count: {prevDb.Count}");
                    prevDb.Debug_LogSortOrder("PrevStatement");
                }
                else { Debug.LogWarning("PrevStatement DB not found."); }

                if (m_Workspace.ConnectionDBList.TryGetValue(EConnection.NextStatement, out var nextDb))
                {
                   // Debug.Log($"Initial Check - NextStatement DB Count: {nextDb.Count}");
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
        if (draggingBlock == null /*|| draggingConnections == null*/ || m_Workspace == null)
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
      /*  for (int i = 0; i < draggingConnections.Count; i++)
        {
            //Debug.Log($"  - Conn[{i}]: {ConnectionModel.GetConnectionModelID(draggingConnections[i])}, Has DBOpposite: {draggingConnections[i]?.DBOpposite != null}");
        }*/
      /*
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
            myConn.DBOpposite.SearchForClosest(myConn, m_ConnectionSnapDistance, Vector2.zero /*dxy era Vector2.zero*///, out neighbour, out neighbourRadius);

           // Debug.Log($"    <- Search Result: Neighbour={ConnectionModel.GetConnectionModelID(neighbour)}, Radius={neighbourRadius}");

          /*  if ((neighbour != null)) //Encontramos un vecino
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
        */
        //Actualizo el estado del contenido para realizar el drop
       // Debug.Log($"[ProcessDrag EXIT] Loop Finished. Final Best Target: {ConnectionModel.GetConnectionModelID(bestTarget)} (Previous: {ConnectionModel.GetConnectionModelID(oldBestTarget)})");
        m_CurrentBestTargetConnection = bestTarget;
        m_CurrentSourceCandidate = sourceCandidate;

        //Actualizao el resaltado visual a través de la vista correspondiente
        UpdateVisualHighlighting(oldBestTarget, m_CurrentBestTargetConnection);

        //Informo si he encontrado una conexión que se pueda llevar a cabo "snapable"
        if (m_CurrentBestTargetConnection != null)
        {
           // Debug.Log($"<color=green>[ProcessDrag STATUS] Potential snap FOUND:</color> Source {ConnectionModel.GetConnectionModelID(m_CurrentSourceCandidate)} -> Target {ConnectionModel.GetConnectionModelID(m_CurrentBestTargetConnection)}");
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
    private void UpdateVisualHighlighting(ConnectionModel oldBestTarget, ConnectionModel newBestTarget)
    {

        string prefix = $"{System.DateTime.Now:HH:mm:ss.fff} [BCC.UpdateVisHighlight]";
        string oldId = ConnectionModel.GetConnectionModelID(oldBestTarget);
        string newId = ConnectionModel.GetConnectionModelID(newBestTarget);

       // Debug.Log($"{prefix} CALLED. OldTarget: '{oldId}', NewTarget: '{newId}'.");

        if (oldBestTarget != newBestTarget)
        {
            //Quitar el resaltado en la antiuga conexión
            if (oldBestTarget != null)
            {
                Debug.Log($"{prefix} Targets DIFFER. Updating highlights.");

                ConnectionView oldView = m_WorkspaceView.GetConnectionView(oldBestTarget);

                Debug.Log($"{prefix} Attempting to get OldView for '{oldId}'. Found: {oldView != null}. Highlight(false).");

                oldView?.Highlight(false);
            }

            //Resalto la nueva conexión
            if (newBestTarget != null)
            {
                ConnectionView newView = m_WorkspaceView.GetConnectionView(newBestTarget);
                Debug.Log($"{prefix} Attempting to get NewView for '{newId}'. Found: {newView != null}. Highlight(true).");

                if (newView == null)
                {
                    Debug.LogError($"{prefix} CRITICAL: GetConnectionView returned NULL for NewTarget: '{newId}'. Cannot highlight.", this.gameObject);
                }

                newView?.Highlight(true);
            }
        }
        else
        {
          //  Debug.Log($"{prefix} Targets are SAME ({newId}). No visual highlight state change needed by this method call directly.");
        }

    }

    /// <summary>
    /// Prepara las conexiones finales que se usarían si el arrastre termina ahora.
    /// No las ejecuta, solo las identifica.
    /// Esto se llamaría ANTES de TryConnectAndPlace en el nuevo flujo, o como parte de él.
    /// </summary>
    public void FinalizePotentialConnection()
    {
        m_FinalizedBestTargetConnection = m_CurrentBestTargetConnection;
        m_FinalizedSourceCandidate = m_CurrentSourceCandidate;

        // Quitar el resaltado porque la fase de 'buscar' ha terminado.
        // El resaltado definitivo o el snap visual se maneja post-conexión o por la animación de snap.
        UpdateVisualHighlighting(m_CurrentBestTargetConnection, null);

        // Limpiar los candidatos actuales de 'ProcessDrag' para el próximo ciclo de drag si lo hubiera
        // o para evitar confusión si el bloque no se conecta.
        m_CurrentBestTargetConnection = null;
        m_CurrentSourceCandidate = null;
        _hasLoggedDbSortThisDrag = false; // Resetea para el próximo drag
    }

    /// <summary>
    /// Obtiene las conexiones que fueron identificadas como la mejor pareja al finalizar el drag.
    /// Devuelve true si hay una pareja válida, false si no.
    /// </summary>
    public bool GetFinalizedConnections(out ConnectionModel targetConnection, out ConnectionModel sourceConnection)
    {
        targetConnection = m_FinalizedBestTargetConnection;
        sourceConnection = m_FinalizedSourceCandidate;

        if (targetConnection != null && sourceConnection != null)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Esta función ahora solo verifica si el bloque fue soltado en un lugar válido (CodingArea o Basura)
    /// y maneja la confirmación de clones si no hay una conexión directa.
    /// LA CONEXIÓN REAL YA NO OCURRE AQUÍ.
    /// Devuelve true si la acción de "drop" (no necesariamente conexión) fue válida.
    /// </summary>
    public bool HandleDropPlacement(BlockModel draggingBlock, bool isTemplateClone, Vector2 pointerScreenPosition, BlockDragController dragController)
    {
    

        // Verificamos si el puntero está sobre el área de codificación válida.
        bool isOverCodingArea = dragController.IsPointerOverArea(pointerScreenPosition, m_WorkspaceView.CodingArea);

        if (!isOverCodingArea)
        {
            // Si NO está sobre el área de codificación, consideramos que es un drop inválido 
            //  Debug.LogWarning($"HandleDropPlacement: Invalid drop location (outside CodingArea) for block {draggingBlock.ID}. Not placing or confirming clone.");

            // NO disponemos el bloque aquí. BlockDragController.HandleEndDrag decidirá qué hacer con un drop inválido.
            return false; // Indica que el drop no fue en un área de colocación válida (sin conexión).
        }
        else
        {
            // Se soltó en el área de codificación (pero no hubo conexión SNAP)
            // Debug.Log($"HandleDropPlacement: Block {draggingBlock.ID} dropped in valid free space in CodingArea.");
            if (isTemplateClone)
            {
                // Era un clon de plantilla soltado libremente -> añadirlo al modelo del workspace.
                //  Debug.Log($"HandleDropPlacement: Confirming add for template clone {draggingBlock.ID}.");
                m_Workspace.AddBlock(draggingBlock); // Confirmar el clon
            }
            else
            {
                // Era un bloque existente movido libremente. Su modelo ya está en el workspace.
                //  Debug.Log($" - Existing block {draggingBlock.ID} moved freely. Ensuring it's registered if needed.");
                // Asegurarse de que si se desenganchó, sigue siendo un bloque 'top' en el workspace.
                if (draggingBlock.ParentBlock == null && !m_Workspace.BlockDB.ContainsKey(draggingBlock.ID))
                {
                    // Esto puede pasar si fue desenganchado y no estaba como top-level block previamente
                    m_Workspace.AddBlock(draggingBlock);
                }
            }
            return true; // Indica que el drop fue en un área válida para colocación.
        }
    }

    public void ResetPotentialConnection()
    {
        _hasLoggedDbSortThisDrag = false;
    
        if (m_CurrentBestTargetConnection != null)
        {
            UpdateVisualHighlighting(m_CurrentBestTargetConnection, null);
            m_CurrentBestTargetConnection = null;
            m_CurrentSourceCandidate = null;
        }
        m_FinalizedBestTargetConnection = null; 
        m_FinalizedSourceCandidate = null;
    }

    /// <summary>
    /// Intenta conectar el candidato final que se encontró durante el drag.
    /// Llama a la lógica de conexión del modelo y maneja errores.
    /// </summary>
    /// <returns>True si la conexión fue exitosa, de lo contrario false.</returns>
    public bool TryFinalizeConnection()
    {
        // Usamos el método que ya tenías para obtener los candidatos finales
        GetFinalizedConnections(out ConnectionModel targetConn, out ConnectionModel sourceConn);

        if (targetConn != null && sourceConn != null)
        {
            try
            {
                // La lógica para determinar quién es el superior se mueve aquí
                // para mantener la encapsulación.
                if (targetConn.IsSuperior)
                {
                    targetConn.Connect(sourceConn);
                }
                else if (sourceConn.IsSuperior)
                {
                    sourceConn.Connect(targetConn);
                }
                else
                {
                    // Este caso no debería ocurrir si CanConnectWithReason funciona bien.
                    Debug.LogError("Error de lógica: Ninguna de las conexiones es Superior. No se pudo conectar.");
                    return false;
                }

                Debug.Log($"<color=green>BlockConnectionController: Conexión exitosa entre {ConnectionModel.GetConnectionModelID(sourceConn)} y {ConnectionModel.GetConnectionModelID(targetConn)}</color>");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error durante ConnectionModel.Connect: {e.Message}");
                return false;
            }
        }

        return false; // No había ninguna conexión válida para finalizar.
    }
}//fin clase BlockController