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
        var call = callstack.Pop();
        if (call is CmdEnumerator)
        {
            Debug.Log(">>>>>exit + " + ((CmdEnumerator)call).Block.Type);
            CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.FinishBlock, ((CmdEnumerator)call).Block));
        }
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
                break;
            }

            if (curStatus == Runner.Status.Pause) // Manejar la pausa
            {
                // Espera en el modo Pausa hasta que el estado cambie a Running o Stop
                while (curStatus == Runner.Status.Pause)
                    yield return null; // Pausa efectiva.
                if (curStatus == Runner.Status.Stop) break; // Si se paró durante la pausa.
            }

            IEnumerator itor = callstack.Peek();


            bool moveResult = itor.MoveNext(); // Intenta mover la corrutina actual.

            // Si el paso actual de la corrutina ha rendido un sub-IEnumerator
            if (itor.Current is IEnumerator currentNestedCall && currentNestedCall != null)
            {
                PushCall(currentNestedCall); // Empuja el sub-IEnumerator a la pila.

                // Si es un paso en modo "Step" y acabamos de entrar a un nuevo bloque, detenemos y esperamos `Step()`.
                if (RunMode == Runner.Mode.Step && currentNestedCall is CmdEnumerator)
                {
                    yield break; // Esperar al próximo Step() o Resume().
                }
            }
            // Si el paso actual rindió un `yield return null` o `yield return new WaitForSeconds()`
            // El `itor.MoveNext()` devuelve true y el `Current` no es otro `IEnumerator`.
            else if (moveResult)
            {
                yield return itor.Current; // Realizar la espera de Unity (un frame, o tiempo de espera, etc.).
            }
            // Si la corrutina actual ha terminado (itor.MoveNext() devolvió false)
            else
            {
                // Se quita el elemento que acaba de terminar.
                PopCall();

                // Aquí manejamos la cadena de bloques si el que acaba de terminar era un CmdEnumerator
                // Y si es el final de una pila de bloques.
                if (itor is CmdEnumerator finishedCmdEnumerator) 
                {
                   
                }

                if (RunMode == Runner.Mode.Step)
                {
                    // Si estamos en modo Step y acabamos de Pop (terminar un bloque), pausamos hasta el siguiente Step.
                    yield break;
                }
            }
        } // Fin del while (callstack.Count > 0)

        // Una vez que la pila de llamadas esté vacía o la ejecución se haya detenido.
        if (callstack.Count == 0 && curStatus != Runner.Status.Stop) // Se terminó naturalmente
        {
            Debug.LogFormat("<color=green>[CodeRunner - {0}]: end - time: {1}.</color>",
                            (gameObject != null ? gameObject.name : "DESTROYED"), Time.time); // Manejo para GC.

            finishCb?.Invoke(); // Llama al callback de finalización.
            curStatus = Runner.Status.Stop; // Asegura que el estado sea detenido.
        }
        else if (curStatus == Runner.Status.Stop)
        { // Se detuvo forzosamente.
            Debug.LogFormat("<color=red>[CodeRunner - {0}]: forcibly stopped - time: {1}.</color>",
                            (gameObject != null ? gameObject.name : "DESTROYED"), Time.time);
            // El Stop() ya limpió la pila y el CSharpRunner limpiará el CmdRunner.
        }

        /*
        bool finished = true;
        while (itor.MoveNext())
        {
            if (itor.Current is IEnumerator)
            {
                IEnumerator current = itor.Current as IEnumerator;
                PushCall(current);
                if (RunMode == Runner.Mode.Step && (current is CmdEnumerator))
                {
                    yield break;
                }

                finished = false;
                break;
            }

            yield return itor.Current;
        }

        if (!finished) continue;
        PopCall();

        if (itor is CmdEnumerator)
        {
            //exit point of block

            //push next block
            CmdEnumerator next = ((CmdEnumerator)itor).GetNextCmd();
            if (next != null)
            {
                PushCall(next);
            }

            if (RunMode == Runner.Mode.Step)
            {
                break;
            }
        }

        if (curStatus == Runner.Status.Pause || curStatus == Runner.Status.Stop)
            break;
    }

    if (curStatus == Runner.Status.Stop)
    {
        callstack.Clear();
    }

    if (callstack.Count == 0)
    {
        Debug.LogFormat("<color=green>[CodeRunner - {0}]: end - time: {1}.</color>", gameObject.name, Time.time);
        if (curStatus != Runner.Status.Stop)
        {
            finishCb?.Invoke();
        }
        curStatus = Runner.Status.Stop;
    }*/
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