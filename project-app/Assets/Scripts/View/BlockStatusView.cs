/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 11/03/2025
 * 
 * Versión: 2.0.0
 * 
 * Descripción: Esta clase visualizará la ejecucción de código y mostrará el estado de los bloques
 * 
 */

using UnityEngine;
using UnityEngine.UI; 

public class BlockStatusView : MonoBehaviour // Asegúrate que hereda de MonoBehaviour
{
    
    [SerializeField] private GameObject m_HighlightObject; 
    private ExecutionController m_ExecutionController;
    private WorkSpaceView m_WorkspaceView;

    void Awake()
    {
        // Encontrar referencias 
       /* m_ExecutionController = FindFirstObjectByType<ExecutionController>();
        m_WorkspaceView = FindFirstObjectByType<WorkSpaceView>();         
        // Validar referencias
        if (m_ExecutionController == null)
            Debug.LogError("BlockStatusView: ExecutionController not found!");
        if (m_WorkspaceView == null)
            Debug.LogError("BlockStatusView: WorkspaceView not found!");*/

      
        if (m_HighlightObject == null)
        {
            m_HighlightObject = new GameObject("StatusHighlight", typeof(RectTransform), typeof(Image));
            m_HighlightObject.transform.SetParent(this.transform);
            Image img = m_HighlightObject.GetComponent<Image>();
            img.color = new Color(1f, 1f, 0f, 0.5f); // Amarillo semitransparente
            img.raycastTarget = false; 
            RectTransform rt = m_HighlightObject.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
        m_HighlightObject?.SetActive(false); // Oculto al inicio
    }

    void Start()
    {
        m_ExecutionController = FindFirstObjectByType<ExecutionController>(); 
        m_WorkspaceView = FindFirstObjectByType<WorkSpaceView>();

        // Validar referencias
        if (m_ExecutionController == null)
            Debug.LogError("BlockStatusView: ExecutionController not found in Start!", this.gameObject);
        if (m_WorkspaceView == null)
            Debug.LogError("BlockStatusView: WorkspaceView not found in Start!", this.gameObject);

        // Suscribir a eventos del ExecutionController
        if (m_ExecutionController != null)
        {
            m_ExecutionController.OnExecutionStartBlock += HandleExecutionStartBlock;
            m_ExecutionController.OnExecutionFinishBlock += HandleExecutionFinishBlock;
            m_ExecutionController.OnExecutionStop += HandleExecutionStopOrError;
            m_ExecutionController.OnExecutionError += HandleExecutionStopOrError; 
        }
    }

    public void InitializeView(WorkSpaceView workspaceView, ExecutionController executionController)
    {
        m_WorkspaceView = workspaceView;
        m_ExecutionController = executionController;
        if (m_WorkspaceView == null) Debug.LogError("...", this.gameObject);
        if (m_ExecutionController == null) Debug.LogError("...", this.gameObject);
       
        //Subscripción a eventos
        if (m_ExecutionController != null)
        {
            m_ExecutionController.OnExecutionStartBlock += HandleExecutionStartBlock;
            m_ExecutionController.OnExecutionFinishBlock += HandleExecutionFinishBlock;
            m_ExecutionController.OnExecutionStop += HandleExecutionStopOrError;
            m_ExecutionController.OnExecutionError += HandleExecutionStopOrError;
            Debug.Log("BlockStatusView subscribed to ExecutionController events.");
        }
    }
    void OnDestroy()
    {
        //Desubscripción a eventos
        if (m_ExecutionController != null)
        {
            m_ExecutionController.OnExecutionStartBlock -= HandleExecutionStartBlock;
            m_ExecutionController.OnExecutionFinishBlock -= HandleExecutionFinishBlock;
            m_ExecutionController.OnExecutionStop -= HandleExecutionStopOrError;
            m_ExecutionController.OnExecutionError -= HandleExecutionStopOrError;
        }
    }


    //  Manejo de Eventos 

    private BlockView m_CurrentHighlightedView = null;

    private void HandleExecutionStartBlock(BlockModel blockModel)
    {
        if (m_WorkspaceView == null || blockModel == null) return;

        BlockView blockView = m_WorkspaceView.GetBlockView(blockModel); 

        if (blockView != null)
        {
            m_CurrentHighlightedView = blockView; // se Guarda la referencia a la vista actual
            ShowHighlight(blockView);
        }
        else
        {
            Debug.LogWarning($"BlockStatusView: Could not find BlockView for Block ID {blockModel.ID}");
            HideHighlight();
        }
    }

    private void HandleExecutionFinishBlock(BlockModel blockModel)
    {
        
        if (m_CurrentHighlightedView != null && m_CurrentHighlightedView.BlockModel == blockModel)
        {
            HideHighlight();
            m_CurrentHighlightedView = null;
        }
       
    }

    private void HandleExecutionStopOrError(BlockModel block, string message) // Sobrecarga para Error
    {
        HideHighlight();
        m_CurrentHighlightedView = null;
    }
    private void HandleExecutionStopOrError() // Sobrecarga para Stop
    {
        HideHighlight();
        m_CurrentHighlightedView = null;
    }


    private void ShowHighlight(BlockView targetView)
    {
        if (m_HighlightObject == null || targetView == null) return;

        // Mover el objeto de resaltado para ser hijo del BlockView y resetear su posición/escala local
        m_HighlightObject.transform.SetParent(targetView.ViewTransform, false);
        m_HighlightObject.transform.localPosition = Vector3.zero;
        m_HighlightObject.transform.localScale = Vector3.one;
        m_HighlightObject.GetComponent<RectTransform>().anchoredPosition = Vector2.zero; // Resetear anchored

        // Asegurar que su RectTransform cubra el targetView 
        // RectTransform rt = m_HighlightObject.GetComponent<RectTransform>();
        // rt.anchorMin = Vector2.zero;
        // rt.anchorMax = Vector2.one;
        // rt.offsetMin = new Vector2(-2, -2); // Expandir un poco para borde
        // rt.offsetMax = new Vector2(2, 2);

        m_HighlightObject.SetActive(true);
        m_HighlightObject.transform.SetAsLastSibling(); // Asegurar que esté por encima visualmente
    }

    private void HideHighlight()
    {
        if (m_HighlightObject != null)
        {
            m_HighlightObject.SetActive(false);
            // Opcional: Moverlo de vuelta a ser hijo de BlockStatusView para no ensuciar jerarquía
            // m_HighlightObject.transform.SetParent(this.transform);
        }
    }

  
}