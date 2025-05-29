/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 26/05/2025
 * 
 * Versión: 1.0.
 * 
 * Descripción: 
 */

using System.Collections;
using UnityEngine;

public class MotionBlockInterpreter : Cmdtor
{
    private BlockObserver m_BlockObserver;

    public MotionBlockInterpreter(BlockObserver observer)
    {
        m_BlockObserver = observer;
    }

    public IEnumerator Execute(BlockModel block)
    {
        if (m_BlockObserver == null)
        {
            Debug.LogError("[MotionBlockInterpreter] BlockObserver is null! Cannot execute motion.");
            yield break;
        }

        //Obtener el valor de los pasos del bloque
        float stepsValue = 0f;
        string stepsStr = block.GetFieldValue("STEPS");

        if (string.IsNullOrEmpty(stepsStr))
        {
            Debug.LogWarning($"[MotionBlockInterpreter] 'motion_movesteps' has no STEPS value. Using default of 10.");
            stepsValue = 10f; // Default.
        }
        else
        {
            if (!float.TryParse(stepsStr, out stepsValue))
            {
                Debug.LogError($"[MotionBlockInterpreter] Could not parse STEPS '{stepsStr}'. Using default of 10.");
                stepsValue = 10f; // Error de parseo, usar default.
            }
        }

        Debug.Log($"[MotionBlockInterpreter] Requesting robot to move {stepsValue} steps.");

        // Llama al BlockObserver para mover el robot - REVISAR

        yield return m_BlockObserver.StartCoroutine(m_BlockObserver.MoveRobot(stepsValue));

        Debug.Log($"[MotionBlockInterpreter] Robot finished moving.");


    }

    protected override IEnumerator OnRun(BlockModel block)
    {

        //yield return Execute(block);
        if (m_BlockObserver == null)
        {
            Debug.LogError("[MotionBlockInterpreter] BlockObserver is NULL! Robot cannot move.");
            yield break; // Terminar la corrutina si no hay observador.
        }

        // Obtener el valor de los pasos del bloque
         float stepsValue = 10f;

        // 1. Obtener el InputModel cuyo nombre es "STEPS"
        InputModel stepsInput = block.GetInput("STEPS");

        if (stepsInput != null)
        {
            // 2. Dentro de ese InputModel, buscar el FieldModel llamado "NUM_FIELD"
            // (Este es el campo que visualmente contiene el "10" en tu bloque)
            FieldModel numField = stepsInput.FieldRow.Find(f => f.Name == "NUM_FIELD");

            if (numField != null)
            {
                // 3. Obtener el valor en texto del FieldModel
                string stepsStr = numField.GetValue();

                // 4. Intentar parsear el valor a float
                if (float.TryParse(stepsStr, out stepsValue))
                {
                    Debug.Log($"<color=blue>[MotionBlockInterpreter.OnRun] Extracted {stepsValue} steps from Field 'NUM_FIELD'.</color>");
                }
                else
                {
                    // Fallback si el parseo falla (ej. el usuario escribió texto)
                    Debug.LogWarning($"<color=orange>[MotionBlockInterpreter.OnRun] Failed to parse steps value '{stepsStr}' from Field 'NUM_FIELD'. Defaulting to 10.</color>");
                    stepsValue = 10f; // Valor por defecto en caso de error
                }
            }
            else
            {
                // Este log indica que la estructura del prefab podría haber cambiado
                Debug.LogWarning($"<color=orange>[MotionBlockInterpreter.OnRun] Field 'NUM_FIELD' not found in 'STEPS' input for block '{block.ID}'. Defaulting to 10.</color>");
                stepsValue = 10f;
            }
        }
        else
        {
            // Si por alguna razón, ni siquiera se encuentra el input llamado "STEPS"
            Debug.LogWarning($"<color=orange>[MotionBlockInterpreter.OnRun] Input 'STEPS' not found for block '{block.ID}'. Defaulting to 10.</color>");
            stepsValue = 10f;
        }
        /*
        CmdEnumerator stepsCtor = CSharp.Interpreter.ValueReturn(block, "STEPS", new DataStruct(10));
        //yield return stepsCtor;

        DataStruct stepsData = stepsCtor.Data;

        if (!stepsData.IsUndefined && stepsData.IsNumber)
        {
            stepsValue = stepsData.NumberValue.Value;
            Debug.Log($"<color=blue>[MotionBlockInterpreter.OnRun] Extracted {stepsValue} steps from block '{block.ID}'.</color>");
        }
        else
        {
            Debug.LogWarning($"<color=orange>[MotionBlockInterpreter.OnRun] DataStruct de 'STEPS' no es un número válido (Type: {stepsData.Type}). Defaulting to {stepsValue}.</color>");
        }*/
        /*
         * 
         * 
                float stepsValue = 10f; // Default de emergencia

                if (!stepsData.IsUndefined && stepsData.IsNumber)
                {
                    stepsValue = stepsData.NumberValue.Value;
                    Debug.Log($"<color=blue>[MotionBlockInterpreter.OnRun] Extracted {stepsValue} steps from block '{block.ID}'.</color>");
                }
                else
                {
                    Debug.LogWarning($"<color=orange>[MotionBlockInterpreter.OnRun] 'STEPS' value for block '{block.ID}' is not a valid number (Type: {stepsData.Type}). Defaulting to {stepsValue}.</color>");
                }
                */

        /* if (stepsCtor != null)
         {
             yield return stepsCtor; // Esperar a que el CmdEnumerator de STEPS termine
             stepsData = stepsCtor.Data; // Intentar obtener los datos del resultado
         }
         else
         {
             Debug.LogWarning($"<color=red>[MotionBlockInterpreter.OnRun] CmdEnumerator para input 'STEPS' es NULL. (Input 'STEPS' podría no tener un bloque conectado o estar mal definido). Asumiendo 10 pasos.</color>");
         }
         yield return m_BlockObserver.MoveRobot(stepsValue);
         Debug.Log($"<color=purple>[MotionBlockInterpreter.OnRun] Robot finished moving {stepsValue} steps.</color>");*/
        /* string stepsStr = block.GetFieldValue("STEPS");

         if (string.IsNullOrEmpty(stepsStr))
         {
             Debug.LogWarning($"[MotionBlockInterpreter] 'motion_movesteps' has no STEPS value. Using default of 10.");
             stepsValue = 10f; // Default.
         }
         else
         {
             if (!float.TryParse(stepsStr, out stepsValue))
             {
                 Debug.LogError($"[MotionBlockInterpreter] Could not parse STEPS '{stepsStr}'. Using default of 10.");
                 stepsValue = 10f; // Error de parseo, usar default.
             }
         }

         Debug.Log($"[MotionBlockInterpreter] Requesting robot to move {stepsValue} steps.");*/

        //  Llamar al BlockObserver para mover el robot.

        yield return m_BlockObserver.MoveRobot(stepsValue);          //  la ejecución de este intérprete espere hasta que MoveRobot() termine.


        Debug.Log($"<color=purple>[MotionBlockInterpreter.OnRun] Robot finished moving {stepsValue} steps.</color>");
    }
}