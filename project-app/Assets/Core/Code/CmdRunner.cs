using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CmdRunner : MonoBehaviour
{
    public static CmdRunner Create(string runnerName, bool dontDestroyOnLoad = false)
    {
        GameObject parentObj = GameObject.Find("CodeRunners");
        if (parentObj == null)
        {
            parentObj = new GameObject("CodeRunners");
            GameObject.DontDestroyOnLoad(parentObj);
        }
        GameObject runnerObj = new GameObject(runnerName);
        if (dontDestroyOnLoad)
        {
            GameObject.DontDestroyOnLoad(runnerObj);
        }
        runnerObj.transform.parent = parentObj.transform;
        return runnerObj.AddComponent<CmdRunner>();
    }

    public Runner.Mode RunMode = Runner.Mode.Normal;

    private Runner.Status curStatus = Runner.Status.Stop;
    public Runner.Status CurStatus { get { return curStatus; } }

    private Stack<IEnumerator> callstack = new Stack<IEnumerator>();

    public CmdEnumerator CurrentCmdEnumerator
    {
        get
        {
            if (callstack.Count > 0 && callstack.Peek() is CmdEnumerator cmd)
            {
                return cmd;
            }
            return null; // Devuelve null si no hay un CmdEnumerator en la cima de la pila.
        }
    }

    private void PushCall(IEnumerator call)
    {
        callstack.Push(call);
        if (call is CmdEnumerator)
        {
            Debug.Log(">>>>>enter + " + ((CmdEnumerator)call).Block.Type);
            CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.RunBlock, ((CmdEnumerator)call).Block));
        }
    }

    private void PopCall()
    {
        /*var call =*/ callstack.Pop();
       /* if (call is CmdEnumerator)
        {
            Debug.Log(">>>>>exit + " + ((CmdEnumerator)call).Block.Type);
           // CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.FinishBlock, ((CmdEnumerator)call).Block));
        }*/
    }

    private Action finishCb = null;

    public void SetFinishCallback(Action callback)
    {
        finishCb = callback;
    }

    /// <summary>
    /// api - start running code
    /// </summary>
    public void StartRun(CmdEnumerator entryCall)
    {
        curStatus = Runner.Status.Running;

        callstack.Clear();
        PushCall(entryCall);

        Debug.LogFormat("<color=green>[CodeRunner - {0}]: begin - time: {1}.</color>", gameObject.name, Time.time);

        //step mode: wait until Step() calls
        if (RunMode != Runner.Mode.Step)
            StartCoroutine(Run());
    }

    /// <summary>
    /// api - step over to next block in debug mode
    /// </summary>
    public void Step()
    {
        if (RunMode == Runner.Mode.Step)
            StartCoroutine(Run());
    }

    /// <summary>
    /// api - pause running code
    /// </summary>
    public void Pause()
    {
        if (RunMode == Runner.Mode.Step)
            return;

        curStatus = Runner.Status.Pause;
    }

    /// <summary>
    /// api - resume running code
    /// </summary>
    public void Resume()
    {
        if (RunMode == Runner.Mode.Step)
            return;

        curStatus = Runner.Status.Running;
        StartCoroutine(Run());
    }

    /// <summary>
    /// api - stop running code
    /// </summary>
    public void Stop()
    {
        if (curStatus == Runner.Status.Running)
        {
            curStatus = Runner.Status.Stop;
        }
        else if (curStatus == Runner.Status.Pause)
        {
            callstack.Clear();
            curStatus = Runner.Status.Stop;
        }
    }

    /// <summary>
    /// Simulate coroutine execution, replacing Unity's,
    /// in case that nestes IEnumerator call brings one more frame delay.
    /// </summary>
    IEnumerator Run()
    {
        while (callstack.Count > 0)
        {
            if (curStatus == Runner.Status.Stop) // Salir si se ha solicitado detener.
            {
                Logger.Log($"<color=red>[CmdRunner-{gameObject.name}] Stop signal detected in Run() loop. Breaking.</color>");
                break;
            }

            if (curStatus == Runner.Status.Pause) // Manejar la pausa
            {
                Logger.Log($"<color=yellow>[CmdRunner-{gameObject.name}] Paused in Run() loop. Waiting for resume.</color>");
                while (curStatus == Runner.Status.Pause)
                    yield return null; // Pausa efectiva, espera que se reanude.
                Logger.Log($"<color=green>[CmdRunner-{gameObject.name}] Resumed from Run() loop.</color>");
                if (curStatus == Runner.Status.Stop) break; // Si se paró durante la pausa.
            }


            IEnumerator itor = callstack.Peek(); // El IEnumerator actual en la cima de la pila.

            // Intenta avanzar la corrutina actual 
            bool canContinue = itor.MoveNext();

            //Comprobar el resultado de MoveNext() y el valor Current:

            // Si el paso actual rindió un SUB-IEnumerator 
            if (canContinue && itor.Current is IEnumerator currentNestedCall && currentNestedCall != null)
            {
                // Este `currentNestedCall` es otra corrutina que debe ejecutarse antes que la actual.
                PushCall(currentNestedCall); // Empuja la sub-corrutina a la pila.
                Logger.Log($"<color=blue>[CmdRunner-{gameObject.name}] Pushing nested coroutine onto stack. Stack depth: {callstack.Count}.</color>");

                // En modo "Step", después de empujar un nuevo elemento (especialmente un CmdEnumerator para un nuevo bloque)
                // debemos ceder el control y esperar a la próxima llamada a Step().
                if (RunMode == Runner.Mode.Step && currentNestedCall is CmdEnumerator)
                {
                       yield break; // Si está en Step, termina la iteración actual para esperar el siguiente Step()
                }
            }
            // Si el paso actual de la corrutina es un `yield return` normal 
            else if (canContinue)
            {
                // Se realiza la espera solicitada por el itor actual.
               // Logger.Log($"<color=green>[CmdRunner-{gameObject.name}] Yielding {itor.Current?.GetType().Name ?? "null"} for a frame or seconds.</color>");
                yield return itor.Current;
            }
            // Si la corrutina actual HA TERMINADO su ejecución (canContinue es false, significa que MoveNext() devolvió false).
            else
            {
                IEnumerator completedCall = callstack.Pop(); // Saca el IEnumerator que acaba de terminar.
                Logger.Log($"<color=blue>[CmdRunner-{gameObject.name}] Pop-ing completed call: {completedCall.GetType().Name}. Stack depth: {callstack.Count}.</color>");

                // Si el elemento que terminó era un CmdEnumerator significa que un BLOQUE COMPLETO de Scratch terminó su ejecución
                if (completedCall is CmdEnumerator finishedBlockCmd)
                {
                    // NOTIFICAR que este bloque ha TERMINADO su ejecución.
                    Debug.Log($">>>>>exit CmdRunner for block: {finishedBlockCmd.Block.Type} (ID: {finishedBlockCmd.Block.ID})");
                    CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.FinishBlock, finishedBlockCmd.Block));

                    // LÓGICA CRÍTICA: Obtener y Pushear el siguiente bloque en la cadena de Scratch.
                    CmdEnumerator nextCmd = finishedBlockCmd.GetNextCmd();
                    if (nextCmd != null)
                    {
                        PushCall(nextCmd); // Empuja el próximo bloque (CmdEnumerator) a la pila para ejecutarlo.
                        Logger.Log($"<color=blue>[CmdRunner-{gameObject.name}] Pushing next block in chain: '{nextCmd.Block.Type}' (ID: {nextCmd.Block.ID}).</color>");
                    }
                    else
                    {
                        // No hay más bloques en esta cadena. La pila puede quedar vacía o no.
                        Logger.Log($"<color=blue>[CmdRunner-{gameObject.name}] End of block chain for '{finishedBlockCmd.Block.Type}'. No more next blocks.</color>");
                    }
                }
                // Si lo que terminó fue una sub-corrutina 
                // Su padre (`MotionBlockInterpreter.OnRun`) sigue en la pila y continuará.
                else
                {
                    Logger.Log($"<color=blue>[CmdRunner-{gameObject.name}] Nested coroutine finished. Returning control to parent CmdEnumerator. Stack depth: {callstack.Count}.</color>");
                }

                // En modo "Step", después de que cualquier comando o bloque haya terminado, se pausa.
                if (RunMode == Runner.Mode.Step)
                {
                    yield break; // Pausa el Run() coroutine hasta el próximo Step() o Resume().
                }
            }
        } // Fin del while (callstack.Count > 0)

        // Una vez que la pila de llamadas esté vacía o la ejecución se haya detenido.
        if (callstack.Count == 0 && curStatus != Runner.Status.Stop) // La pila terminó de forma natural
        {
            Logger.LogFormat("<color=green>[CmdRunner - {0}]: Finished execution for this stack naturally. Total elapsed: {1}.</color>",
                            gameObject.name, Time.time);
            finishCb?.Invoke(); // Llama al callback de finalización al CSharpRunner.
            curStatus = Runner.Status.Stop; // Asegura que el estado sea detenido.
        }
        else if (curStatus == Runner.Status.Stop)
        { // Se detuvo forzosamente.
            Logger.LogFormat("<color=red>[CmdRunner - {0}]: Forcibly stopped execution for this stack. Total elapsed: {1}.</color>",
                            gameObject.name, Time.time);
        }
    }


    /// <summary>
    /// get current callstack
    /// </summary>
    public List<string> GetCallStack()
    {
        List<string> blocks = new List<string>();
        IEnumerator[] calls = callstack.ToArray();
        for (int i = calls.Length - 1; i >= 0; i--)
        {
            if (calls[i] is CmdEnumerator)
                blocks.Add(((CmdEnumerator)calls[i]).Block.Type);
        }
        return blocks;
    }
}