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

    public  IEnumerator Execute(BlockModel block)
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

         //  Llamar al BlockObserver para mover el robot.
      
         yield return m_BlockObserver.MoveRobot(stepsValue);          //  la ejecución de este intérprete espere hasta que MoveRobot() termine.


        Debug.Log($"<color=purple>[MotionBlockInterpreter.OnRun] Robot finished moving {stepsValue} steps.</color>");
    }
}