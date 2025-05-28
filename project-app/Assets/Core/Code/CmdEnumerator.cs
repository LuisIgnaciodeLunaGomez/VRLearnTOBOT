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

using System.Collections;
using UnityEngine;


/// <summary>
/// IEnumerator wrapper for running Cmdtor, block code
/// </summary>
public class CmdEnumerator : IEnumerator
{
    private readonly BlockModel m_Block;
    private readonly Cmdtor m_CmdtorInstance;
    private IEnumerator mItor;
    private bool m_FinishedEarly; // Flag para indicar si la ejecución se "saltó" o no tiene Cmdtor
    public BlockModel Block
    {
        get { return m_Block; }
    }

    public Cmdtor CmdtorInstance
    {
        get { return m_CmdtorInstance; }
    }

    public DataStruct Data
    {
        get {
            
            return m_CmdtorInstance.Data; 
        
        }
    }

    public CmdEnumerator(BlockModel block)
    {
        m_Block = block;
        m_CmdtorInstance = CSharp.Interpreter.GetBlockInterpreter(block);
        // mItor = mCmdtor.Run(block);

        mItor = null; // Inicializar a null

        if (m_CmdtorInstance == null) 
        {
            Debug.LogWarning($"CmdEnumerator: No interpreter (Cmdtor) found for block type '{block.Type}'. This block or its interpretation will be skipped. (Block ID: {block.ID})");
            mItor = EmptyEnumerator(); // Asigna un enumerador que no hace nada.
            m_FinishedEarly = true; // Marca que está "terminado" porque no hay Cmdtor.
        }
        else
        {
            // Asumiendo que Run(block) en Cmdtor devuelve un IEnumerator.
            mItor = m_CmdtorInstance.Run(block);
            m_FinishedEarly = false;
        }
    }

    // Un enumerador vacío para bloques sin intérprete
    private IEnumerator EmptyEnumerator()
    {
        yield break; // No rinde nada, se completa al instante.
    }


    public bool MoveNext()
    {
        if (m_FinishedEarly) // Si no hay intérprete, o fue saltado
        {
            Debug.LogError($"CmdEnumerator: MoveNext called but not initialized or mItor is null for block '{m_Block?.Type}' (ID: {m_Block?.ID}). Returning false.");
            return false;
        }

        // Lógica de deshabilitad
        if (m_Block.Disabled || m_Block.GetInheritedDisabled())
        {
            return false; // Este enumerador ha terminado porque el bloque está deshabilitado.
        }

        // Mueve el enumerador del intérprete real (Cmdtor.Run(block))
        return mItor.MoveNext();
    }

    public void Reset()
    {
        // Resetea el enumerador del intérprete subyacente.
        if (mItor != null)
        {
            mItor.Reset(); 
        }
        m_FinishedEarly = false; // Restablecer la bandera si Reset se llama
    }

    public object Current
    {
        get
        {
            if (mItor == null)
            {
                // Devolver null o una señal de que no hay un valor actual.
                return null;
            }
            return mItor.Current;
        }
    }

    /// <summary>
    /// get the next block's running code, connected with previous - next connection
    /// </summary>
    public CmdEnumerator GetNextCmd()
    {
        var nextblock = m_Block.NextBlock;
        if (nextblock == null || nextblock.Disabled)
            return null;

        //parent loop was break or continue, move out. 
        if (LoopCmdtor.SkipRunByControlFlow(nextblock))
        {
            Debug.Log($"CmdEnumerator: Skipping next block '{nextblock.Type}' due to control flow.");

            return null;
        }
        return new CmdEnumerator(nextblock);
    }
}
