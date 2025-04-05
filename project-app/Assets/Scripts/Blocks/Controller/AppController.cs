/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 28/03/2025
 * 
 * Versión: 1.0.1
 * 
 * Descripción: Gestor central de la aplicación (Singleton), coordinando diferentes partes del sistema que no son estrictamente UI o Modelo/Vista de bloques.
 */

using System.Collections;
using UnityEngine;

public class AppController : MonoBehaviour
{
    public static AppController Instance { get; private set; }
    // Referencias obtenidas, no asignadas en Inspector
    private UICanvasView m_uiManager;
    private WorkSpaceModel m_workspaceModel;
    private WorkSpaceView m_workspaceView;
    private ExecutionController m_executionController;
    private InputController m_inputController; // Si aún lo necesitas
    // TODO otras referencias

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator Start()
    {
        Debug.Log("<color=orange>AppController: Start - Finding components...</color>");

        // Esperar a que UICanvasManager termine su Awake
        yield return null; // Espera un frame

        m_uiManager = FindFirstObjectByType<UICanvasView>();
        if (m_uiManager == null)
        {
            Debug.LogError("AppController: UICanvasManager not found!");
            yield break;
        }

        // Obtener referencias clave desde UICanvasManager
        m_workspaceModel = m_uiManager.Workspace;
        m_workspaceView = m_uiManager.WorkSpaceView;

        if (m_workspaceModel == null || m_workspaceView == null)
        {
            Debug.LogError("AppController: Failed to get Workspace Model or View from UICanvasManager!");
            yield break;
        }

        // Encontrar o inicializar otros controladores
        m_executionController = FindFirstObjectByType<ExecutionController>();
        if (m_executionController == null) m_executionController = gameObject.AddComponent<ExecutionController>();

        m_inputController = FindFirstObjectByType<InputController>();
        if (m_inputController == null) m_inputController = gameObject.AddComponent<InputController>();
        m_executionController = FindFirstObjectByType<ExecutionController>();
        if (m_executionController == null) m_executionController = gameObject.AddComponent<ExecutionController>();
        m_executionController.InitializeController(m_workspaceModel);

        m_inputController = FindFirstObjectByType<InputController>();
        if (m_inputController == null) m_inputController = gameObject.AddComponent<InputController>();

        Debug.Log("<color=green>AppController: Initialization of dependent controllers complete.</color>");

    }

   
    public void TriggerExecution()
    {
        m_executionController?.StartExecution();
    }

    public void TriggerStop()
    {
        m_executionController?.StopExecution();
    }
    public void TriggerSave()
    {
        m_uiManager?.SaveWorkspace();
    }

    public void TriggerLoad()
    {
        m_uiManager?.LoadWorkspace();
    }

}//fin clase AppController
