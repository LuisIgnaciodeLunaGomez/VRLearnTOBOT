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
        
        if (Instance == null) Instance = this; else Destroy(gameObject);

    }

    public void InitializeController(WorkSpaceModel workspaceModel)
    {
        m_WorkspaceModel = workspaceModel;
        if (m_WorkspaceModel == null)
            Debug.LogError("ExecutionController: WorkspaceModel reference is missing!");
        else
            Debug.Log("ExecutionController Initialized.");
    }

    public void StartExecution()
    {
        if (CurrentStatus == ExecutionStatus.Running) { Debug.LogWarning("ExecutionController: Already running."); return; }
        if (m_WorkspaceModel == null) { Debug.LogError("ExecutionController: Cannot start, Workspace is null."); return; }

        Debug.Log("ExecutionController: Requesting CSharp.Runner to start...");

        OnExecutionStart?.Invoke(); 

        CSharp.Runner.Run(m_WorkspaceModel);
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
            if (m_RunnerObserver != null)
            {
                CSharp.Runner.RemoveObserver(m_RunnerObserver);
                m_RunnerObserver = null;
            }
       
            Instance = null;
        }
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