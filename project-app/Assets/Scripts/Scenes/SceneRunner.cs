/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 22/06/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */
using System.Collections;
using UnityEngine;

namespace UBlockly
{
    public class SceneRunner : MonoBehaviour
    {
        private ExecutionTimerView m_timerView;

        // El observer necesita ser una variable de miembro para poder des-registrarlo.
        private RunnerUpdateStateObserver m_runnerObserver;

        // Start se ejecuta una sola vez al cargar la escena
        void Start()
        {
            // Forzamos el tiempo, la solución clave que encontramos antes.
            Time.timeScale = 1f;

            // Encontramos la vista del cronómetro en la escena
            m_timerView = FindObjectOfType<ExecutionTimerView>();
            if (m_timerView == null)
            {
                Debug.LogError("FATAL: SceneRunner no pudo encontrar un GameObject con el script 'ExecutionTimerView' en la escena.");
                return;
            }

            // Comprobar si hay un script guardado para ejecutar
            if (string.IsNullOrEmpty(ScriptManager.WorkspaceXml))
            {
                Debug.LogWarning("SceneRunner: No hay ningún script guardado en ScriptManager para ejecutar.");
                m_timerView.ResetDisplay();
                return;
            }

            // Si hay un script, iniciamos la única corrutina que gestionará todo
            StartCoroutine(RunWorkspaceScript());
        }

        // Esta es NUESTRA corrutina principal. Gestiona todo el ciclo de vida de la ejecución.
        private IEnumerator RunWorkspaceScript()
        {
            Debug.Log("<color=orange><b>RunWorkspaceScript:</b></color> Iniciando la ejecución del script y el cronómetro.");

            // 1. Resetear y preparar la UI del cronómetro
            float executionTime = 0f;
            m_timerView.ResetDisplay();

            // 2. Crear y cargar el workspace desde el XML guardado
            Workspace executionWorkspace = new Workspace();
            var dom = Xml.TextToDom(ScriptManager.WorkspaceXml);
            Xml.DomToWorkspace(dom, executionWorkspace);
            ScriptManager.ClearStoredWorkspace();

            // 3. Suscribirse a los eventos del Runner para saber cuándo termina
            m_runnerObserver = new RunnerUpdateStateObserver();
            CSharp.Runner.AddObserver(m_runnerObserver);

            // 4. Iniciar la ejecución de los bloques (esto es una llamada rápida, no bloqueante)
            CSharp.Runner.Run(executionWorkspace);

            // 5. El bucle principal de esta corrutina, se ejecuta cada frame MIENTRAS los bloques corren
            while (CSharp.Runner.CurStatus == Runner.Status.Running || CSharp.Runner.CurStatus == Runner.Status.Pause)
            {
                // Solo contamos el tiempo si no está en pausa
                if (CSharp.Runner.CurStatus == Runner.Status.Running)
                {
                    executionTime += Time.deltaTime;
                    m_timerView.UpdateTimerDisplay(executionTime);
                }

                yield return null; // Esperar al siguiente frame
            }

            // 6. El bucle ha terminado. La ejecución de los bloques ha finalizado.
            CSharp.Runner.RemoveObserver(m_runnerObserver); // Limpiar el observador
            Debug.Log($"<color=orange><b>RunWorkspaceScript:</b></color> La ejecución de bloques ha terminado. Tiempo final: {executionTime}");
        }

        // El observer ahora es mucho más simple. Ya no necesita una referencia a SceneRunner.
        private class RunnerUpdateStateObserver : IObserver<RunnerUpdateState>
        {
            public void OnUpdated(object subject, RunnerUpdateState args)
            {
                // Este observador en realidad ya no necesita hacer nada, porque el bucle `while`
                // en `RunWorkspaceScript` se encarga de todo.
                // Lo mantenemos por si en el futuro queremos añadir lógica extra
                // al recibir un evento de STOP (ej. mostrar un panel de "Misión Cumplida").
                if (args.Type == RunnerUpdateState.Stop || args.Type == RunnerUpdateState.Error)
                {
                    Debug.Log("Observer: Se ha detectado el fin de la ejecución.");
                }
            }
        }
    }
}