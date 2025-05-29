/****************************************************************************

Copyright 2021 sophieml1989@gmail.com

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

Descripción: CSharpRunner class for executing block code in Unity using C#.
****************************************************************************/


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CSharpRunner : Runner
{
    private readonly Names m_VariableNames;
    private readonly Datas m_VariableDatas;
    //private readonly List<CmdRunner> mCodeRunners;
    private List<CmdRunner> m_CodeRunners = new List<CmdRunner>();
    private Queue<BlockModel> m_ExecutionQueue = new Queue<BlockModel>();

    public Func<IEnumerator, Coroutine> CoroutineStarter { get; set; }
    public Action CoroutineStopper { get; set; }


    public CSharpRunner(Names variableNames, Datas variableDatas)
    {
        m_VariableNames = variableNames;
        m_VariableDatas = variableDatas;
        m_CodeRunners = new List<CmdRunner>();
    }

    public override void Run(WorkSpaceModel workspace)
    {
        m_VariableNames.Reset();
        m_VariableDatas.Reset();

        //start runner from the topmost blocks, exclude the procedure definition blocks

        /*List<BlockModel> blocks = workspace.GetTopBlocks(true);//.FindAll(block => !ProcedureDB.IsDefinition(block));
        if (blocks.Count == 0)
        {
            //CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop));
            FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop, "No top-level blocks to execute."));
            return;
        }*/

        CurStatus = Status.Running;

        if (CoroutineStopper != null)
            CoroutineStopper.Invoke();
        else
            Debug.LogWarning("CSharpRunner: CoroutineStopper is null! Cannot stop existing coroutines gracefully.");

        // Detener y limpiar los corredores de código existentes.
        foreach (CmdRunner runner in m_CodeRunners)
        {
            if (runner != null && runner.gameObject != null)
                GameObject.Destroy(runner.gameObject); // Destruye GameObjects.
        }
        m_CodeRunners.Clear();
        m_ExecutionQueue.Clear(); // Limpieza de la cola para una nueva ejecución

        // Ordenar los top blocks de mayor Y (arriba) a menor.
        // Y por ID
        List<BlockModel> topBlocks = workspace.GetTopBlocks(true); // Solo los top-level blocks visibles (no disabled).

        // Filtra los bloques que no tienen 'NextBlock' conectado o son 'event_whenflagclicked' si deben iniciar.
        foreach (BlockModel block in topBlocks)
        {
            if (block.Type == "event_whenflagclicked")
            {
                // Disparador, añadir su siguiente bloque a la cola de ejecución.
                if (block.NextConnection != null && block.NextConnection.TargetConnection != null)
                {
                    m_ExecutionQueue.Enqueue(block.NextConnection.TargetConnection.SourceBlock);
                }
                // Si no hay más bloques después de 'event_whenflagclicked', no hace nada más.
            }
            else
            {
               
                // mExecutionQueue.Enqueue(block); 
            }
        }

        if (m_ExecutionQueue.Count > 0)
        {
            if (CoroutineStarter != null)
            {
                CoroutineStarter.Invoke(ProcessNextExecutionStack());
            }
            else
            {
                Debug.LogError("CSharpRunner: CoroutineStarter is null! Cannot start execution stack coroutine.");
                FireUpdate(new RunnerUpdateState(RunnerUpdateState.Error, "Execution failed to start: Coroutine runner not set."));
                CurStatus = Status.Stop;
            }
        }
        else
        {
            FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop, "No executable block stacks found."));
            CurStatus = Status.Stop;
            Logger.LogWarning("[CSharpRunner] No executable block stacks were enqueued. Execution halted.");

        }
    }

    public void StopAllExecution()
    {
        CurStatus = Status.Stop; // Marcar el estado global como "Stop"

        foreach (CmdRunner runner in m_CodeRunners.ToArray()) // Itera sobre una copia para evitar modificación mientras itera
        {
            if (runner != null)
            {
                runner.Stop(); // Indica a cada runner individual que se detenga
                if (runner.gameObject != null)
                {
                    GameObject.Destroy(runner.gameObject); // Y limpia sus GO
                }
            }
        }
        m_CodeRunners.Clear();
        m_ExecutionQueue.Clear(); // Limpia la cola de bloques pendientes.
        FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop, "Execution forcibly stopped."));
    }


    private IEnumerator ProcessNextExecutionStack()
    {
        while (m_ExecutionQueue.Count > 0 && CurStatus == Status.Running)
        {
            BlockModel currentRootBlock = m_ExecutionQueue.Dequeue();

            // Omitir bloques si por algún motivo ya están siendo ejecutados o son nulos.
            if (currentRootBlock == null || currentRootBlock.Disabled || currentRootBlock.GetInheritedDisabled())
            {
                continue; // Salta a la siguiente iteración de la cola.
            }

            // Crea el CmdEnumerator para este bloque (motion_movesteps en tu caso)
            CmdEnumerator cmdtorToRun = new CmdEnumerator(currentRootBlock);

            // Un CmdRunner para CADA pila top-level.
            CmdRunner runner = CmdRunner.Create(currentRootBlock.Type); // Crea el CmdRunner (MonoBehaviour)
            m_CodeRunners.Add(runner);


            bool stackFinished = false;
            runner.SetFinishCallback(() =>
            {
                // Cuando *este* CmdRunner ha terminado toda su pila (TODOS los bloques encadenados)
                stackFinished = true; // Señaliza que esta pila de ejecución ha terminado.
                m_CodeRunners.Remove(runner); // Retira este runner una vez ha terminado su tarea.
                GameObject.Destroy(runner.gameObject); // Y destruye el GO
            });

            // Inicia la ejecución de esta pila en el CmdRunner.
            runner.StartRun(cmdtorToRun);

            // Esperar hasta que esta pila de ejecución (controlada por 'runner') finalice.
            while (!stackFinished && CurStatus == Status.Running)
            {
                yield return null; // Espera un frame.
            }
        }

        // Si se salió del bucle, o bien la cola está vacía, o el estado es Stop.
        if (CurStatus != Status.Stop)
        {
            CurStatus = Status.Stop; // Marca como finalizado
            FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop, "All execution stacks processed."));
        }
    }


    private void RunSync(List<BlockModel> topBlocks)
    {
        foreach (BlockModel block in topBlocks)
        {
            FireUpdate(new RunnerUpdateState(RunnerUpdateState.RunBlock, block)); //Notifco el bloque actual

            CmdRunner runner = CmdRunner.Create(block.Type);
            m_CodeRunners.Add(runner);

            runner.RunMode = RunMode;
            runner.SetFinishCallback(() =>
            {
                if (runner.gameObject != null)
                    GameObject.Destroy(runner.gameObject);
                m_CodeRunners.Remove(runner);

                FireUpdate(new RunnerUpdateState(RunnerUpdateState.FinishBlock, block));

                if (m_CodeRunners.Count == 0)
                {
                    CurStatus = Status.Stop;

                    FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop, "All synchronous blocks finished."));

                   // CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop));
                }
            });
            runner.StartRun(new CmdEnumerator(block));
        }
    }

    private void RunAsync(List<BlockModel> topBlocks)
    {
        FireUpdate(new RunnerUpdateState(RunnerUpdateState.RunBlock, topBlocks[0]));

        CmdRunner runner = CmdRunner.Create(topBlocks[0].Type);
        m_CodeRunners.Add(runner);

        runner.RunMode = RunMode;

        int index = 0;
        runner.SetFinishCallback(() =>
        {
            FireUpdate(new RunnerUpdateState(RunnerUpdateState.FinishBlock, topBlocks[index]));

            index++;
            if (index < topBlocks.Count)
            {
                FireUpdate(new RunnerUpdateState(RunnerUpdateState.RunBlock, topBlocks[index]));

                runner.StartRun(new CmdEnumerator(topBlocks[index]));
            }
            else
            {
                if (runner.gameObject != null)
                    GameObject.Destroy(runner.gameObject);
                m_CodeRunners.Clear();
                CurStatus = Status.Stop;
                // CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop));
                FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop, "All asynchronous blocks finished."));

            }
        });
        runner.StartRun(new CmdEnumerator(topBlocks[0]));
    }

    public override void Pause()
    {
        if (RunMode == Mode.Step || CurStatus != Status.Running)
            return;
        CurStatus = Status.Pause;

        foreach (CmdRunner runner in m_CodeRunners)
        {
            if (runner.CurStatus == Runner.Status.Running)
                runner.Pause();
        }
        //CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.Pause));
        FireUpdate(new RunnerUpdateState(RunnerUpdateState.Pause));
        Debug.Log("[CSharpRunner] Execution paused.");
    }

    public override void Resume()
    {
        if (RunMode == Mode.Step || CurStatus != Status.Pause)
            return;
        CurStatus = Status.Running;

        foreach (CmdRunner runner in m_CodeRunners)
        {
            if (runner.CurStatus == Runner.Status.Pause)
                runner.Resume();
        }
        //CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.Resume));
        FireUpdate(new RunnerUpdateState(RunnerUpdateState.Resume));
        Debug.Log("[CSharpRunner] Execution resumed.");
    }

    public override void Stop()
    {
        if (CurStatus == Status.Stop)
            return;
        CurStatus = Status.Stop;

        if (CoroutineStopper != null)
            CoroutineStopper.Invoke();
        else
            Debug.LogWarning("CSharpRunner: CoroutineStopper is null when trying to stop.");


        foreach (CmdRunner runner in m_CodeRunners)
        {
            if (runner != null)
            {
                runner.Stop(); // Indica a cada CmdRunner individual que se detenga
                if (runner.gameObject != null)
                {
                    GameObject.Destroy(runner.gameObject); // Y limpia sus GameObjects
                }
            }
            //runner.Stop();
            //GameObject.Destroy(runner.gameObject);
        }
        m_CodeRunners.Clear();
        m_ExecutionQueue.Clear();

        // CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop));
        FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop, "Execution stopped by request."));
        Debug.Log("[CSharpRunner] Execution stopped.");
    }

    public override void Error(string msg)
    {
        CurStatus = Status.Stop;

        foreach (CmdRunner runner in m_CodeRunners)
        {
            runner.Stop();
        }
        // CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.Error, msg));
        FireUpdate(new RunnerUpdateState(RunnerUpdateState.Error, msg));
        Debug.LogError("[CSharpRunner] Execution error: " + msg);
    }

    public override void Step()
    {
        //fix bug: mCodeRunners can be modified in loop. If runner finishes running, it is removed from the list
        for (int i = m_CodeRunners.Count - 1; i >= 0; i--)
        {
            m_CodeRunners[i].Step();
        }


    }

    public List<string> GetCallStack()
    {
        if (RunMode != Mode.Step || m_CodeRunners.Count == 0)
            return null;

        List<string> callstack = m_CodeRunners[0].GetCallStack();
        for (int i = 1; i < m_CodeRunners.Count; i++)
        {
            callstack.Add("");
            callstack.Concat(m_CodeRunners[i].GetCallStack());
        }
        return callstack;
    }
}