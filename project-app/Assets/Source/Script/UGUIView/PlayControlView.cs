using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UBlockly.UGUI
{
    public class PlayControlView : MonoBehaviour
    {
        [Header("Controles Principales")]
        [SerializeField] private Button m_BtnExecute;  //  botón de bandera verde
        [SerializeField] private Button m_BtnSave;     // botón de guardar
        [SerializeField] private Button m_BtnOptions;  //  botón de tres puntos

        [Header("Paneles de Diálogo")]
        [SerializeField] private XmlView m_XmlView;     // Referencia a tu script de carga/guardado

        [Header("Navegación")]
        [SerializeField] private Button m_BtnGoBackToMenu; // logo de VR Learn ToBot
        [SerializeField] private string m_IntroSceneName = "IntroScene"; 

         private WorkspaceController mWorkspaceController;
        private WorkspaceView mWorkspaceView;
        void Start()
        {
            Logger.Log("PlayControlView.Start() -- INICIANDO", this.gameObject);

            mWorkspaceController = BlocklyUI.WorkspaceController;
            if (mWorkspaceController == null)
            {
                Debug.LogError("WorkspaceController no está inicializado. Asegúrate de que BlocklyUI se ejecuta primero.");
                return;
            }


            // Botón Ejecutar (Bandera Verde)
            if (m_BtnExecute != null)
            {
                Debug.Log("Asignando listener al botón de ejecutar...");
                m_BtnExecute.onClick.AddListener(OnExecuteClicked);
            }


            // Botón Guardar
            if (m_BtnSave != null)
                m_BtnSave.onClick.AddListener(OnSaveClicked);

            // Botón Opciones/Cargar (tres puntos)
            if (m_BtnOptions != null)
                m_BtnOptions.onClick.AddListener(OnOptionsClicked);

            // Botón del Logo para volver atrás
            if (m_BtnGoBackToMenu != null)
                m_BtnGoBackToMenu.onClick.AddListener(OnGoBackClicked);
        }

        private RunnerUpdateStateObserver mObserver;

        public void Init(WorkspaceView workspaceView)
        {
            mWorkspaceView = workspaceView;
            mObserver = new RunnerUpdateStateObserver(this);
            CSharp.Runner.AddObserver(mObserver);

           /* m_BtnRun.onClick.AddListener(OnRun);
            m_BtnPause.onClick.AddListener(OnPause);
            m_BtnStop.onClick.AddListener(OnStop);
            m_BtnStep.onClick.AddListener(OnStep);

            m_ToggleNormal.isOn = true;
            SetMode(Runner.Mode.Normal);
            m_ToggleNormal.onValueChanged.AddListener(on => SetMode(Runner.Mode.Normal));
            m_ToggleDebug.onValueChanged.AddListener(on => SetMode(Runner.Mode.Step));

            m_ToggleASync.isOn = true;
            m_ToggleASync.onValueChanged.AddListener(on => SwitchSync(false));
            m_ToggleSync.onValueChanged.AddListener(on => SwitchSync(true));

            m_ToggleCallstack.isOn = false;
            HideCallstack();
            m_ToggleCallstack.onValueChanged.AddListener(on =>
            {
                if (on) ShowCallstack();
                else HideCallstack();
            });*/
        }
        private void OnExecuteClicked()
        {
            Debug.Log("BOTÓN EJECUTAR PULSADO. Guardando workspace y cargando escena del robot...");

            // 1. Guardar el workspace actual en nuestro gestor estático.
            ScriptManager.StoreWorkspaceForExecution();

            // 2. Cargar la escena del robot.
            // Asegúrate de que el nombre de la escena sea correcto.
            SceneManager.LoadScene("UIGUIVRLearnToBot");
        }

        private void OnSaveClicked()
        {
            // Le pedimos al gestor de XML que muestre el panel de guardado
            // Esto mantiene la lógica de mostrar/ocultar UI en el XmlView.
            if (m_XmlView != null)
            {
                Debug.Log("Abriendo diálogo para guardar...");
                m_XmlView.ShowSavePanel();
            }
            else
            {
                // Como alternativa, el controlador podría manejar el diálogo, 
                // pero por ahora lo dejamos así por simplicidad.
                Debug.LogWarning("No se ha asignado una referencia a XmlView.");
            }
        }

        private void OnOptionsClicked()
        {
            // Similar a guardar, le pedimos al XmlView que muestre el panel de carga.
            if (m_XmlView != null)
            {
                Debug.Log("Abriendo diálogo para cargar...");
                m_XmlView.ShowLoadPanel();
            }
            else
            {
                Debug.LogWarning("No se ha asignado una referencia a XmlView.");
            }
        }

        private void OnGoBackClicked()
        {
            Debug.Log($"Volviendo a la escena: {m_IntroSceneName}");
            // Usamos el SceneManager de Unity para cambiar de escena.
            SceneManager.LoadScene(m_IntroSceneName);
        }

        public void Reset()
        {
           /* OnStop();

            m_ToggleNormal.onValueChanged.RemoveAllListeners();
            m_ToggleDebug.onValueChanged.RemoveAllListeners();
            m_BtnRun.onClick.RemoveAllListeners();
            m_BtnPause.onClick.RemoveAllListeners();
            m_BtnStop.onClick.RemoveAllListeners();
            m_BtnStep.onClick.RemoveAllListeners();
            m_ToggleCallstack.onValueChanged.RemoveAllListeners();*/

            CSharp.Runner.RemoveObserver(mObserver);
        }

        private void OnStopClicked()
        {
            // Detiene el Runner de UBlockly Y las acciones del robot.
            mWorkspaceController.StopExecution();
            GameAPI.StopRobot();
        }

   

        private void UpdateStatus(RunnerUpdateState args)
        {
            switch (args.Type)
            {
               /* case RunnerUpdateState.Pause:
                    //m_BtnRun.gameObject.SetActive(true);
                   // m_BtnPause.gameObject.SetActive(false);
                    break;*/
                /*case RunnerUpdateState.Resume:
                    m_BtnRun.gameObject.SetActive(false);
                    m_BtnPause.gameObject.SetActive(true);
                    break;*/
                case RunnerUpdateState.Stop:
              /*  case RunnerUpdateState.Error:
                    HideCallstack();
                    EnableSettings(true);
                    SetMode(CSharp.Runner.RunMode);*/
                    break;
                case RunnerUpdateState.RunBlock:
                /*case RunnerUpdateState.FinishBlock:
                    if (m_ToggleCallstack.isOn)
                        ShowCallstack();*/
                    break;
            }
        }

        private class RunnerUpdateStateObserver : IObserver<RunnerUpdateState>
        {
            private PlayControlView mView;

            public RunnerUpdateStateObserver(PlayControlView statusView)
            {
                mView = statusView;
            }

            public void OnUpdated(object subject, RunnerUpdateState args)
            {
                mView.UpdateStatus(args);
            }
        }
    }
}
