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
 * mezclen entre los paneles.
 */

using System;
using UnityEngine;
using System.Collections.Generic;

public class BlockConnectionController : MonoBehaviour
{
    public static BlockConnectionController Instance { get; private set; }

    private WorkSpaceModel m_Workspace;
    private WorkSpaceView m_WorkspaceView;
    private BlockDragController m_BlockDragController;

    private ConnectionModel m_CurrentBestTargetConnection = null;
    private ConnectionModel m_CurrentSourceCandidate = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void InitializeController(WorkSpaceModel workspace, WorkSpaceView workspaceView, BlockDragController blockDragController)
    {

        m_Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        m_WorkspaceView = workspaceView ?? throw new ArgumentNullException(nameof(workspaceView));
        m_BlockDragController = blockDragController ?? throw new ArgumentNullException(nameof(blockDragController));

        Debug.Log("ConnectionController Initialized");

    }

    public void ProcessDrag(BlockModel draggingBlock, List<ConnectionModel> draggingConnections, Vector2 dragginblockBaseLogicalPosition)
    {

        if (draggingBlock == null || m_Workspace == null) return;

        //Busco la mejor conexión
        ConnectionModel bestTarget = null;
        ConnectionModel sourceCandidate = null;

        float m_ConnectionSnapDistance = BlockViewSettings.Instance.ConnectionSnapDistance;
        float m_ConnectionSearchRadius = BlockViewSettings.Instance.ConnectionSearchRange;

        float closestRadiusSq = m_ConnectionSnapDistance * m_ConnectionSnapDistance; //Calculo la distancia cuadrada

        ConnectionModel oldBestTarget = m_CurrentBestTargetConnection;

        foreach (ConnectionModel myConn in draggingConnections)
        {

            if (myConn == null || myConn.DBOpposite == null) continue;

            ConnectionModel neighbour;

            float radiusS1;

            //Busco en la BBDD de tipos opuestos conexiones cercanas

            Vector2 myConnLogicalPosition = myConn.Location;

            myConn.DBOpposite.SearchForClosest(myConn, m_ConnectionSearchRadius, myConnLogicalPosition, out neighbour, out float distance);

            if ((neighbour != null)) //Encontramos un vecino
            {
                float currentChechRadiusSq = m_ConnectionSearchRadius * m_ConnectionSearchRadius;

                if (distance < Math.Sqrt(closestRadiusSq) && distance < m_ConnectionSearchRadius) //Es la más cercana y esta dentro del radio general
                {
                    if (myConn.IsConnectionAllowed(neighbour, m_ConnectionSnapDistance))  // además esta en del radio definido "snap"
                    {
                        closestRadiusSq = distance * distance; //Actualizo el mejor radio cuadrado que he encontrado

                        bestTarget = neighbour; //Almaceno la conexión encontrada que es compatible 

                        sourceCandidate = myConn; //Almaceno la conexión candidata que es la que estoy arrastrando
                    }
                }
            }
        }

        //Actualizo el estado del contenido para realizar el drop

        m_CurrentBestTargetConnection = bestTarget;
        m_CurrentSourceCandidate = sourceCandidate;

        //Actualizao el resaltado visual a través de la vista correspondiente
        UpdateVisualHighlighting(oldBestTarget, m_CurrentBestTargetConnection);

        //Informo si he encontrado una conexión que se pueda llevar a cabo "snapable"
        if (m_CurrentBestTargetConnection != null)
        {
            Debug.Log($"ConnectionController: Found potential snap target: {m_CurrentBestTargetConnection.SourceBlock.Type}:{m_CurrentBestTargetConnection.Type} for source: {m_CurrentSourceCandidate.SourceBlock.Type}:{m_CurrentSourceCandidate.Type}");
        }
        else
        {
            Debug.Log("ConnectionController: No potential snap target found.");
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
    public bool TryConnectAndPlace(BlockModel draggingBlock, bool isTemplateClone, bool overTrasBin, Vector2 pointerScreenPosition)
    {

        if (draggingBlock == null || m_WorkspaceView == null || m_Workspace == null) return false;

        ConnectionModel finalTargetConnection = m_CurrentBestTargetConnection;
        ConnectionModel finalSourceConnection = m_CurrentSourceCandidate;
        bool connected = false;

        //Limpiamos el estado del controlador para el siguiente drag 
        m_CurrentSourceCandidate = null;
        m_CurrentSourceCandidate = null;

        //Elimino también el resaltado por si estuviera activo aún.
        UpdateVisualHighlighting(finalTargetConnection, null);

        //Si hay una conexión válidad (snap) intento conectarmete
        if (finalTargetConnection != null && finalSourceConnection != null)
        {
            Debug.Log($"ConnectionController: Attempting connection {finalSourceConnection.SourceBlock.Type}:{finalSourceConnection.Type} -> {finalTargetConnection.SourceBlock.Type}:{finalTargetConnection.Type}");

            try
            {

                //Gestionamos la desconexión automática si el destino ya esta conectado y lo notificará a las vistas

                finalTargetConnection.Connect(finalSourceConnection);
                connected = true;

                if (isTemplateClone)
                {
                    Debug.Log($"ConnectionController: Confirming add for template clone {draggingBlock.ID} due to successful connection.");
                    m_Workspace.AddBlock(draggingBlock);
                }

                if (draggingBlock.ParentBlock == null)
                {
                    m_Workspace.AddBlock(draggingBlock);
                }
            }

            catch (Exception e)
            {

                Debug.LogWarning($"ConnectionController: Connect failed: {e.Message}" /*, draggingBlock.gameObject)*/);
            }
        }

        //Si no se ha conectado, procedo a gestionar el drop en el basurero o en el espacio libre

        if (!connected)
        {
            bool isOverCodingArea = m_BlockDragController.IsPointerOverArea(pointerScreenPosition, m_WorkspaceView.CodingArea);

            bool wasDeleted = false;

            if (overTrasBin) //Revisar esta lógica 
            {
                Debug.Log($"ConnectionController: Drop over trash bin. Disposing block {draggingBlock.ID}");
                draggingBlock.Dispose(false);
                wasDeleted = true;

            }
            else if (isOverCodingArea) //Cae fuera del codingArea lógica más sensata con scratch 
            {
                Debug.LogWarning($"ConnectionController: Invalid drop location (outside CodingArea and not in trash). Disposing block {draggingBlock.ID}");

                draggingBlock.Dispose(false);
                wasDeleted = true;

            }
            else
            {

                //Cae en un espacio libre dentro del CodingArea --- OJO a las posiciones en los dos paneles....

                Debug.Log($"ConnectionController: Confirming add for template clone {draggingBlock.ID} dropped in free space.");

                if (isTemplateClone)
                {
                    m_Workspace.AddBlock(draggingBlock);
                }
                else
                {
                    //TODO: si es un bloque Ws y cae libremente, el ParentBlock lo ponemos a null en UnPlug. La vista se tiene que reposicionar según el modelo XY
                }
            }

            connected = true;

            if (wasDeleted) return true;

            //Parte final: Me aseguro de que la vista corresponde al modelo siempre que no se haya eliminado

            BlockView finalView = m_WorkspaceView.GetBlockView(draggingBlock);

            if(finalView == null && connected)
            {
                Debug.LogError($"ConnectionController: After drop, block model {draggingBlock.ID} exists but BlockView is NULL!");

                //draggingBlock.Dispose(false);//Revisar esto

            }

            else if (finalView != null && finalView.Block != draggingBlock)
            {
                Debug.LogError($"ConnectionController: After drop, BlockView {finalView.name} is bound to a different model {finalView.Block.ID}! This indicates a view/model state issue.");

            }

            else if (finalView != null)
            {
                //TODO: revisar si hay que configurar algún trigger.
            }

                
        }

        return connected;
    }


}