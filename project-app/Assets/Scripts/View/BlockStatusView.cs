/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 11/03/2025
 * 
 * Versión: 2.0.1
 * 
 * Descripción: Esta clase visualizará la ejecucción de código y mostrará el estado de los bloques
 * 
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class BlockStatusView : MonoBehaviour 
{

    private RunnerUpdateStateObserver mObserver;
    private GameObject mStatusObj;
    private Stack<BlockModel> mRunningBlocks;
    private BlockView mRunBlockView;

    private GameObject m_StatusInstance = null;
    private BlockView m_TargetBlockViewToAttachTo = null;

    [Header("UI References")]
    [Tooltip("Asigna aquí el Prefab que representa la luz/indicador de estado de ejecución.")]
    [SerializeField] private GameObject m_StatusLightPrefab;

    private void Awake()
    {
        // Validamos si el prefab fue asignado en el Inspector
        if (m_StatusLightPrefab == null)
        {
            Debug.LogError("BlockStatusView: Status Light Prefab (m_StatusLightPrefab) is not assigned in the Inspector!", this);
            this.enabled = false; // Deshabilitamos si falta el prefab esencial
            return;
        }
        mRunningBlocks = new Stack<BlockModel>();
        mObserver = new RunnerUpdateStateObserver(this);
        CSharp.Runner.AddObserver(mObserver);
        // Creamos la instancia inicialmente pero mantenerla oculta
        TryCreateStatusInstance();
        Hide(); // Aseguramos que empieza oculto

    }

    /// <summary>
    /// Intenta crear la instancia de la luz de estado si no existe ya.
    /// </summary>
    private bool TryCreateStatusInstance()
    {
        if (m_StatusInstance == null)
        {
            // Verificamos de nuevo el prefab (por si Awake falló y se re-habilitó)
            if (m_StatusLightPrefab == null)
            {
                Debug.LogError("TryCreateStatusInstance: PrefabStatusLight is missing!", this);
                return false;
            }
            // Instanciamos la luz de estado como hijo de esta vista 
            
            Transform parent = WorkSpaceView.Active?.CodingArea ?? this.transform.parent; // Usamos CodingArea si está disponible
            if (parent == null)
            {
                Debug.LogError("TryCreateStatusInstance: Cannot find a valid parent (CodingArea or this.parent)!", this);
                return false;
            }

            m_StatusInstance = Instantiate(m_StatusLightPrefab, parent, false);
            m_StatusInstance.name = "ExecutionStatusLight";

            // Configuramos RectTransform si es necesario (ej. para Layouts)
            RectTransform statusRect = m_StatusInstance.GetComponent<RectTransform>();
            if (statusRect != null)
            {
                statusRect.anchorMin = statusRect.anchorMax = new Vector2(0, 1); // Top-Left
                statusRect.pivot = new Vector2(0, 1); // Pivot Top-Left (para que anchoredPosition sea relativo a la esquina del bloque)
                                                      // O mantener pivot central si el cálculo de posición lo espera : statusRect.pivot = 0.5f * Vector2.one;
            }
        }
        return m_StatusInstance != null;
    }

    private void Show()
    {
        /* if (mStatusObj == null)
         {
             mStatusObj = GameObject.Instantiate(BlockViewSettings.Instance.PrefabStatusLight, WorkSpaceView.Active.CodingArea, false);
             RectTransform statusRect = mStatusObj.GetComponent<RectTransform>();
             statusRect.anchorMin = statusRect.anchorMax = new Vector2(0, 1);
             statusRect.pivot = 0.5f * Vector2.one;
         }
         if (!mStatusObj.activeInHierarchy)
             mStatusObj.SetActive(true);*/

        if (TryCreateStatusInstance()) // Asegurar que existe
        {
            if (m_StatusInstance != null && !m_StatusInstance.activeSelf) // Usar activeSelf para chequear estado
            {
                m_StatusInstance.SetActive(true);
            }
        }
        else { Debug.LogError("Failed to Show Status Light - Instance creation failed.", this); }
    }

    private void Hide()
    {
        if (mStatusObj != null)
        {
            mStatusObj.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        CSharp.Runner.RemoveObserver(mObserver);
    }

    public void UpdateStatus(RunnerUpdateState args)
    {
        switch (args.Type)
        {
            case RunnerUpdateState.RunBlock:
                {
                    mRunningBlocks.Push(args.RunningBlock);
                    mRunBlockView = WorkSpaceView.Active.GetBlockView(args.RunningBlock);
                    Show();
                    break;
                }
            case RunnerUpdateState.FinishBlock:
                {
                    if (mRunningBlocks.Count > 0 && mRunningBlocks.Peek() == args.RunningBlock)
                    {
                        mRunningBlocks.Pop();
                        if (mRunningBlocks.Count > 0)
                            mRunBlockView = WorkSpaceView.Active.GetBlockView(mRunningBlocks.Peek());
                        else
                            Hide();
                    }
                    break;
                }
            case RunnerUpdateState.Stop:
                {
                    Hide();
                    mRunningBlocks.Clear();
                    mRunBlockView = null;
                    break;
                }
            case RunnerUpdateState.Error:
                {
                    if (!string.IsNullOrEmpty(args.Msg))
                    {
                        MsgDialog dialog = DialogFactory.CreateDialog("message") as MsgDialog;
                        dialog.SetMsg(args.Msg);
                    }
                    Hide();
                    mRunningBlocks.Clear();
                    mRunBlockView = null;
                    break;
                }
        }
    }

    private void LateUpdate()
    {
        if (mRunBlockView != null)
        {
            RectTransform statusRect = mStatusObj.GetComponent<RectTransform>();
            statusRect.SetParent(mRunBlockView.ViewTransform, false);
            statusRect.anchoredPosition = new Vector2(20, -25);
            mRunBlockView = null;
        }
    }
    private class RunnerUpdateStateObserver : IObserver<RunnerUpdateState>
    {
        private BlockStatusView mView;

        public RunnerUpdateStateObserver(BlockStatusView statusView)
        {
            mView = statusView;
        }

        public void OnUpdated(object subject, RunnerUpdateState args)
        {
            mView.UpdateStatus(args);
        }
    }

}//Fin clase BlockStatusView