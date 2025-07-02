using UnityEngine;
using UnityEngine.EventSystems;

namespace UBlockly.UGUI
{
    public class ToolboxController
    {
        private readonly WorkspaceController mWorkspaceController;

        public ToolboxController(WorkspaceController workspaceController)
        {
            mWorkspaceController = workspaceController;
        }

        /// <summary>
        /// Gestiona la creación y el inicio del arrastre de un bloque desde la toolbox.
        /// </summary>
        public BlockView HandlePickFromToolbox(BlockView originalBlockView, PointerEventData eventData)
        {
            // Calcular posición inicial
            Vector3 localPos = mWorkspaceController.WorkspaceView.CodingArea.InverseTransformPoint(originalBlockView.ViewTransform.position);

            //Clonar el bloque
            BlockView newBlockView = mWorkspaceController.WorkspaceView.CloneBlockView(originalBlockView, new Vector2(localPos.x, localPos.y));

            if (newBlockView == null) return null;

            Logger.Log($"<color=green><b>[ToolboxController]</b>:</color> Clonado '{originalBlockView.name}' a '{newBlockView.name}'", newBlockView.gameObject);

            // Iniciar el arrastre en el NUEVO bloque a través de su controlador.
            // Esto simula que el usuario hizo clic directamente en el nuevo bloque.
            mWorkspaceController.BlockController.BeginDrag(newBlockView, eventData);

         
            return newBlockView;
        }
    }
}