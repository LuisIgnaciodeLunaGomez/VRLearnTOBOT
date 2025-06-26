using System.Collections;
using UnityEngine;

namespace UBlockly
{
    public class SceneRunner : MonoBehaviour
    {
        private ExecutionTimerView m_timerView;
        private Coroutine m_timerCoroutine;
        private float m_executionTime;
        private bool m_isTimerRunning;


        void Start()
        {
            // 1. Comprobar si hay un script guardado para ejecutar.
            if (string.IsNullOrEmpty(ScriptManager.WorkspaceXml))
            {
                Debug.LogWarning("SceneRunner: No hay ningún script guardado en ScriptManager para ejecutar.");
                return;
            }

            Debug.Log($"<color=orange><b>SceneRunner.Start:</b></color> Time.timeScale ANTES de forzarlo: {Time.timeScale}");
            Time.timeScale = 1f;
            Debug.Log($"<color=orange><b>SceneRunner.Start:</b></color> Time.timeScale DESPUÉS de forzarlo: {Time.timeScale}");

            Debug.Log("SceneRunner: Se encontró un script guardado. Creando un workspace temporal para ejecutarlo.");

            // 2. Crear un workspace temporal EN MEMORIA para ejecutar el script.
         
            Workspace executionWorkspace = new Workspace();

            // 3. Cargar el XML en este workspace temporal.
            var dom = Xml.TextToDom(ScriptManager.WorkspaceXml);
            Xml.DomToWorkspace(dom, executionWorkspace);

            // 4. ¡Ejecutar los bloques del workspace cargado!
            CSharp.Runner.Run(executionWorkspace);

            // 5. Limpiar el script para que no se vuelva a ejecutar
           
            ScriptManager.ClearStoredWorkspace();

            // Busca la vista del cronómetro en esta escena
            m_timerView = FindFirstObjectByType<ExecutionTimerView>();
            if (m_timerView != null) m_timerView.ResetDisplay();

            if (string.IsNullOrEmpty(ScriptManager.WorkspaceXml))
            {
                Debug.LogWarning("SceneRunner: No hay script guardado para ejecutar.");
                return;
            }

            // Iniciar el cronómetro y la ejecución de bloques.
            StartExecution();
        }

        public void StartExecution()
        {
            // Resetear e iniciar el cronómetro
            m_executionTime = 0f;
            if (m_timerView != null) m_timerView.ResetDisplay();

            m_isTimerRunning = true;
            m_timerCoroutine = StartCoroutine(TimerCoroutine()); // ¡AHORA SÍ PUEDE LLAMAR A StartCoroutine!

            // Crear el workspace y ejecutarlo
            Workspace executionWorkspace = new Workspace();
            var dom = Xml.TextToDom(ScriptManager.WorkspaceXml);
            Xml.DomToWorkspace(dom, executionWorkspace);

            CSharp.Runner.Run(executionWorkspace);

            // Suscribirse a los eventos del Runner para detener el cronómetro.
            CSharp.Runner.AddObserver(new RunnerUpdateStateObserver(this));

            ScriptManager.ClearStoredWorkspace();
        }
        private IEnumerator TimerCoroutine()
        {
            while (m_isTimerRunning)
            {
                m_executionTime += Time.deltaTime;
                if (m_timerView != null)
                {
                    m_timerView.UpdateTimerDisplay(m_executionTime);
                }
                yield return null;
            }
        }

        public void StopTimer()
        {
            m_isTimerRunning = false;
            if (m_timerCoroutine != null)
            {
                StopCoroutine(m_timerCoroutine);
                m_timerCoroutine = null;
            }
            Debug.Log("SceneRunner: Cronómetro detenido.");
        }

        private class RunnerUpdateStateObserver : IObserver<RunnerUpdateState>
        {
            private SceneRunner m_sceneRunner;
            public RunnerUpdateStateObserver(SceneRunner runner) { m_sceneRunner = runner; }

            public void OnUpdated(object subject, RunnerUpdateState args)
            {
                if (args.Type == RunnerUpdateState.Stop || args.Type == RunnerUpdateState.Error)
                {
                    m_sceneRunner.StopTimer();
                    CSharp.Runner.RemoveObserver(this);
                }
            }
        }
    }
}