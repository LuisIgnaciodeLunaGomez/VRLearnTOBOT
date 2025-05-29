/****************************************************************************

Copyright 2016 sophieml1989@gmail.com

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CSharpInterpreter : Interpreter
{
    public override CodeName Name
    {
        get { return CodeName.CSharp; }
    }

    public CSharpInterpreter()
    {
        //AÑADE directamente los intérpretes al diccionario 'mCmdMap'
        // (este diccionario es un campo heredado de la clase base Interpreter).
        

        if (BlockObserver.Instance == null)
        {
            Debug.LogError("CSharpInterpreter: BlockObserver.Instance es NULL cuando se intentan registrar intérpretes. " +
                           "Asegúrate de que el GameObject con el script BlockObserver esté activo en la escena " +
                           "y que su método Awake() se ejecute antes de que CSharp.Runner (y por ende CSharp.Interpreter) sea instanciado.");
          
        }


        // TODO: AñadI aquí todos LOS bloques "ejecutables" y sus intérpretes.
        // Los strings ("motion_movesteps", etc.) DEBEN coincidir exactamente con el Type del BlockModel.
        try
        {
            mCmdMap.Add("motion_movesteps", new MotionBlockInterpreter(BlockObserver.Instance));
            //mCmdMap.Add("looks_say", new LooksBlockInterpreter(BlockObserver.Instance));
           // mCmdMap.Add("control_wait", new ControlWaitInterpreter(BlockObserver.Instance)); 
            // ... (añadir todos los demás intérpretes que se creen) ...
        }
        catch (System.ArgumentException ex)
        {
            Debug.LogError($"CSharpInterpreter: Failed to add interpreter to mCmdMap. Key already exists or another error. {ex.Message}");
        }

        Debug.Log("CSharpInterpreter: Inicializado. Intérpretes de bloques cargados en mCmdMap.");
    }

    /// <summary>
    /// run code representing the specified value input.
    /// should return a DataStruct
    /// </summary>
    public CmdEnumerator ValueReturn(BlockModel block, string name)
    {
        var targetBlock = block.GetInputTargetBlock(name);
        if (targetBlock == null)
        {
            Debug.Log(string.Format("Value input block of {0} is null", block.Type));
            return null;
        }
        if (targetBlock.OutputConnection == null)
        {
            Debug.Log(string.Format("Value input block of {0} must have an output connection", block.Type));
            return null;
        }
        return new CmdEnumerator(targetBlock);
    }

    /// <summary>
    /// run code representing the specified value input. WITH a default DataStruct
    /// </summary>
    public CmdEnumerator ValueReturn(BlockModel block, string name, DataStruct defaultData)
    {
        CmdEnumerator etor = ValueReturn(block, name);
        etor.CmdtorInstance.DefaultData = defaultData;
        return etor;
    }

    /// <summary>
    /// Run code representing the statement.
    /// </summary>
    public CmdEnumerator StatementRun(BlockModel block, string name)
    {
        var targetBlock = block.GetInputTargetBlock(name);
        if (targetBlock == null)
        {
            Debug.Log(string.Format("Statement input block of {0} is null", block.Type));
            return null;
        }
        if (targetBlock.PreviousConnection == null)
        {
            Debug.Log(string.Format("Statement input block of {0} must have a previous connection", block.Type));
            return null;
        }

        return new CmdEnumerator(targetBlock);
    }

    public Cmdtor GetBlockInterpreter(BlockModel block)
    {
        // function definition doesn't need interpreter. 
        if (ProcedureDB.IsDefinition(block))
            return null;

        if (block.Type == "event_whenflagclicked")
        {
            return null; // NO se necesita un intérprete para este, lo maneja el CSharpRunner.
        }

        Cmdtor cmdtor;
        if (!mCmdMap.TryGetValue(block.Type, out cmdtor))
            Debug.Log(string.Format(
                "<color='orange'>Language {0} does not know how to interprete code for block type {1}. If this block type doesn't need to be interpreted, please ignore this message.</color>",
                Name, block.Type));
        return cmdtor;
    }
}

