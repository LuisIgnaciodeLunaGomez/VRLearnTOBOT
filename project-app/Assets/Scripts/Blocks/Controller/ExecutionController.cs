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
 * Versión: 1.0.0
 * 
 * Descripción:
 */

using UnityEngine;
using System.Collections.Generic; 
using System;
public class ExecutionEventArgs : EventArgs
{
    public BlockModel Block { get; private set; } 
    public string ErrorMessage { get; private set; } 

    public ExecutionEventArgs(BlockModel block = null, string errorMessage = null)
    {
        Block = block;
        ErrorMessage = errorMessage;
    }
}

public class ExecutionController : MonoBehaviour
{
   
    public static ExecutionController Instance { get; private set; }

    private WorkSpaceModel m_WorkspaceModel;

    private RunnerUpdateStateObserver m_RunnerUpdateStateObserver;

    public BlockModel Block { get; private set; } 
    public string ErrorMessage { get; private set; }

    public ExecutionStatus CurrentStatus
    {
        get
        {
            switch (CSharp.Runner.CurStatus)
            {
                case Runner.Status.Running: return ExecutionStatus.Running;
                case Runner.Status.Pause: return ExecutionStatus.Paused;
                case Runner.Status.Stop: return ExecutionStatus.Idle; 
                default: return ExecutionStatus.Idle;
            }
        }
    }

    private bool mIsInitialized = false; 

    private bool mIsRunning = false;

    private Coroutine m_ExecutionCoroutine = null;
    private Stack<BlockModel> m_ExecutionStack; 
    private Stack<object> m_ReporterStack = new Stack<object>();

  
    public event Action<BlockModel> OnExecutionStartBlock;    
    public event Action<BlockModel> OnExecutionFinishBlock;   
    public event Action OnExecutionStart;           
    public event Action OnExecutionFinish;         
    public event Action OnExecutionStop;           
    public event Action<BlockModel, string> OnExecutionError; 

    private RunnerUpdateStateObserver m_RunnerObserver;

    void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Opcional, 
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        //if (Instance == null) Instance = this; else Destroy(gameObject);
        // Se inicializa el observador interno del Runner
        m_RunnerUpdateStateObserver = new RunnerUpdateStateObserver(this);
    }

    public bool IsFullyInitialized() { return mIsInitialized; }

    public void InitializeController(WorkSpaceModel workspaceModel)
    {
        m_WorkspaceModel = workspaceModel;

        if (m_WorkspaceModel == null)
        {
            Debug.LogError("ExecutionController: WorkspaceModel reference is missing!");
            enabled = false; // Desactiva el controlador si no hay WorkspaceModel.
            return;
        }
        /*else
            Debug.Log("ExecutionController Initialized.");*/

        var forceInterpreterLoad = CSharp.Interpreter; // Llama a CSharp.get_Interpreter() y su constructor.
        var forceRunnerLoad = CSharp.Runner;

        // Inicializa la instancia del observador interno.
        m_RunnerObserver = new RunnerUpdateStateObserver(this);

        // Se suscribe al observador CSharp.Runner.
        
        if (CSharp.Runner != null)
        {
            CSharp.Runner.CoroutineStarter = StartCoroutine;
            CSharp.Runner.CoroutineStopper = StopAllCoroutines;

            //Suscribir el observador al CSharp.Runner.
          

            //CSharp.Runner.RemoveObserver(m_RunnerObserver); //<--- esto parece que no es necesario a la vista de los logs obtenidos.
            CSharp.Runner.AddObserver(m_RunnerUpdateStateObserver);

            Debug.Log("<color=green>ExecutionController Initialized and subscribed to CSharp.Runner events.</color>");
        }
    
        else
        {
            Debug.LogError("ExecutionController: CSharp.Runner is null! Cannot subscribe. Check initialization order in AppController.");
            enabled = false;
        }

        mIsInitialized = true; // Marca como inicializado
        Logger.Log("ExecutionController Initialized successfully.", this);
    }

    public void StartExecution()
    {
        if (!mIsInitialized)
        {
            Logger.LogError("ExecutionController.StartExecution: Controller not fully initialized (mIsInitialized is false). Aborting.", this);
            return;
        }

        if (m_WorkspaceModel == null) {

            OnExecutionError?.Invoke(Block, "WorkspaceModel was null."); ///Ver si esto es correcto o no.
            Debug.LogError("ExecutionController: Cannot start, Workspace is null.");
            return;
        }

       // CSharp.Runner.StopAllExecution();

        int topBlockCount = m_WorkspaceModel.TopBlocks.Count; 
        Debug.Log($"ExecutionController: WorkSpaceModel contains {topBlockCount} top-level blocks.");

       if (topBlockCount == 0)
        {
            Debug.LogWarning("ExecutionController: No top-level blocks found in WorkspaceModel. Cannot run anything.");
            
            return;
        }

        else
        {

            Debug.Log("ExecutionController: Requesting CSharp.Runner to start...");
        }
            OnExecutionStart?.Invoke(); 

        CSharp.Runner.Run(m_WorkspaceModel);

        Logger.Log("<color=green>ExecutionController: CSharp.Runner initialized and execution requested. Observer ready to receive updates.");
    }
    
    public void StopExecution()
    {
        Debug.Log("ExecutionController: Requesting CSharp.Runner to stop...");
        CSharp.Runner.Stop();
    }
    
    public void PauseExecution()
    {
        if (CSharp.Runner.RunMode == Runner.Mode.Normal && CurrentStatus == ExecutionStatus.Running)
        {
            Debug.Log("ExecutionController: Requesting CSharp.Runner to pause...");
            CSharp.Runner.Pause();
        }
    }
    public void ResumeExecution()
    {
        if (CSharp.Runner.RunMode == Runner.Mode.Normal && CurrentStatus == ExecutionStatus.Paused)
        {
            Debug.Log("ExecutionController: Requesting CSharp.Runner to resume...");
            CSharp.Runner.Resume();
        }
    }
    public void StepExecution()
    {
        if (CSharp.Runner.RunMode == Runner.Mode.Step)
        {
            Debug.Log("ExecutionController: Requesting CSharp.Runner to step...");
            CSharp.Runner.Step();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            if(m_RunnerObserver != null && CSharp.Runner != null) // Se añade un check extra
            {
                CSharp.Runner.RemoveObserver(m_RunnerObserver);
                m_RunnerObserver = null;
                Logger.Log("<color=green>ExecutionController: Successfully unsubscribed RunnerUpdateStateObserver.</color>");

            }

            Instance = null;
        }

    }

    /// <summary>
    /// Muestra un log detallado para propósitos de depuración.
    /// </summary>
    private void LogExecutionDebugInfo(string message)
    {
        // Debug.Log($"<color=magenta>[ExecutionController Debug]: {message}</color>");
        // Usar tu Logger si es un Wrapper, sino directamente Debug.Log
        Logger.Log($"<color=magenta>[ExecutionController Debug]: {message}</color>");
    }

    // Traduce los eventos de CSharpRunner a los eventos de ExecutionController
    private class RunnerUpdateStateObserver : IObserver<RunnerUpdateState>
    {
        private ExecutionController m_Controller;

        public RunnerUpdateStateObserver(ExecutionController controller)
        {
            m_Controller = controller;
        }

        public void OnUpdated(object subject, RunnerUpdateState args)
        {
            if (m_Controller == null) return;

            switch (args.Type)
            {
                case RunnerUpdateState.RunBlock:
                    m_Controller.OnExecutionStartBlock?.Invoke(args.RunningBlock);
                    break;
                case RunnerUpdateState.FinishBlock:
                    m_Controller.OnExecutionFinishBlock?.Invoke(args.RunningBlock);
                    break;
                case RunnerUpdateState.Pause:
                    Debug.Log("Observer: Execution Paused");
                    break;
                case RunnerUpdateState.Resume:
                    Debug.Log("Observer: Execution Resumed");
                    break;

                case RunnerUpdateState.Stop:
                    Debug.Log("Observer: Execution Stopped / Finished");
                    m_Controller.OnExecutionFinish?.Invoke();
                    m_Controller.OnExecutionStop?.Invoke();
                    break;

                case RunnerUpdateState.Error:
                    Debug.LogError($"Observer: Execution Error - {args.Msg} in block {args.RunningBlock?.ID}");
                    m_Controller.OnExecutionError?.Invoke(args.RunningBlock, args.Msg);
                    m_Controller.OnExecutionStop?.Invoke();
                    break;
            }
        }
    }
}//fin clase ExecutionController