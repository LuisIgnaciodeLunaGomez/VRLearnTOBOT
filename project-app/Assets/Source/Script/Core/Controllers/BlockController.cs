/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 21/06/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Controlador principal de bloques en el workspace.
 * 
 */
using UnityEngine;
using UnityEngine.EventSystems;

namespace UBlockly.UGUI
{

    public class BlockController
    {
        // Referencia al controlador principal para acceder a otras partes como la Toolbox
        private readonly WorkspaceController mWorkspaceController;
        
        private ConnectionModel mClosestConnection;
        private ConnectionModel mAttachingConnection;
        private Vector2 mTouchOffset;

        public BlockController(WorkspaceController workspaceController)
        {
            mWorkspaceController = workspaceController;
        }

        /// <summary>
        /// Inicia el proceso de arrastrar un bloque.
        /// Contiene la lógica que estaba en BlockView.OnBeginDrag()
        /// </summary>
        public void BeginDrag(BlockView blockView, PointerEventData eventData)
        {
            blockView.Block.UnPlug();
            blockView.SetOrphan();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)blockView.ViewTransform.parent,
                eventData.position,
                BlocklyUI.UICanvas.worldCamera,
                out Vector2 localPos);

            mTouchOffset = blockView.XY - localPos;
        }

        /// <summary>
        /// Se ejecuta mientras el bloque se está arrastrando.
        /// Contiene la lógica que estaba en BlockView.OnDrag()
        /// </summary>
        public void Drag(BlockView blockView, PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)blockView.ViewTransform.parent,
                 eventData.position,
                 BlocklyUI.UICanvas.worldCamera,
                 out Vector2 localPos);
            Vector2 newPosition = localPos + mTouchOffset;

           // Logger.Log($"<color=blue><b>[BlockController.Drag]</b>:</color> Calculada nueva posición: {newPosition}. Asignando a BlockModel...", blockView.gameObject);

            // 1. El controlador actualiza la posición EN EL MODELO.
            blockView.XY = newPosition; /* localPos + mTouchOffset;*/
// La vista se actualizará automáticamente gracias a su observador del modelo.

// 2. El controlador se encarga de buscar conexiones.
FindClosestConnection(blockView);

            // 3. El controlador notifica al controlador principal que verifique la papelera.
            mWorkspaceController.Toolbox.CheckBin(blockView);
        }

        /// <summary>
        /// Finaliza el proceso de arrastrar un bloque.
        /// Contiene la lógica que estaba en BlockView.OnEndDrag()
        /// </summary>
        public void EndDrag(BlockView blockView, PointerEventData eventData)
        {
            if (mClosestConnection != null)
            {
                // El controlador actualiza el modelo para realizar la conexión.
                mClosestConnection.Connect(mAttachingConnection);
                mClosestConnection.FireUpdate(ConnectionModel.UpdateState.UnHighlight);
            }

           
            // Le indicamos al controlador principal que finalice la acción de la papelera
            mWorkspaceController.Toolbox.FinishCheckBin(blockView);
            // Limpiamos el estado temporal de la operación de arrastre
            mClosestConnection = null;
            mAttachingConnection = null;
        }

        /// <summary>
        /// Busca la conexión más cercana para el bloque que se está arrastrando.
        /// Lógica extraída de BlockView.OnDrag()
        /// </summary>
        private void FindClosestConnection(BlockView blockView)
        {
            var oldClosest = mClosestConnection;
            mClosestConnection = null;
            mAttachingConnection = null;
            int minRadius = BlockViewSettings.Get().ConnectSearchRange;

            for (int i = 0; i < blockView.Childs.Count; i++)
            {
                if (blockView.Childs[i].Type != ViewType.Connection)
                    break;

                ConnectionView conView = (ConnectionView)blockView.Childs[i];
                if (conView.SearchClosest(minRadius, ref mClosestConnection, ref minRadius))
                {
                    mAttachingConnection = conView.Connection;
                }
            }

            if (oldClosest != mClosestConnection)
            {
                if (oldClosest != null)
                    oldClosest.FireUpdate(ConnectionModel.UpdateState.UnHighlight);
                if (mClosestConnection != null)
                    mClosestConnection.FireUpdate(ConnectionModel.UpdateState.Highlight);
            }
        }
    }

}