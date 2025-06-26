using UBlockly.UGUI;
using UnityEngine;

namespace UBlockly
{
    public static class ScriptManager
    {
        
        public static string WorkspaceXml { get; private set; }

        /// <summary>
        /// Guarda el estado actual del workspace para ser usado en otra escena.
        /// </summary>
        public static void StoreWorkspaceForExecution()
        {
            // Obtenemos el workspace actual desde BlocklyUI.
            Workspace workspace = BlocklyUI.WorkspaceController.Workspace;
            if (workspace != null)
            {
                // Lo convertimos a XML.
                var dom = Xml.WorkspaceToDom(workspace);
                WorkspaceXml = Xml.DomToText(dom);
                Debug.Log("ScriptManager: Workspace guardado para la ejecución en la siguiente escena.");
            }
            else
            {
                Debug.LogError("ScriptManager: No se pudo encontrar el workspace para guardar.");
                WorkspaceXml = null;
            }
        }

        /// <summary>
        /// Limpia el script almacenado. Es una buena práctica hacerlo después de usarlo.
        /// </summary>
        public static void ClearStoredWorkspace()
        {
            WorkspaceXml = null;
        }
    }
}