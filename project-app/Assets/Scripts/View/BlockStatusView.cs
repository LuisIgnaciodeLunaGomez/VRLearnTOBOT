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

    private void Awake()
    {
        mRunningBlocks = new Stack<BlockModel>();
        mObserver = new RunnerUpdateStateObserver(this);
        CSharp.Runner.AddObserver(mObserver);
    }

    private void Show()
    {
        if (mStatusObj == null)
        {
            mStatusObj = GameObject.Instantiate(BlockViewSettings.Get().PrefabStatusLight, WorkSpaceView.Active.CodingArea, false);
            RectTransform statusRect = mStatusObj.GetComponent<RectTransform>();
            statusRect.anchorMin = statusRect.anchorMax = new Vector2(0, 1);
            statusRect.pivot = 0.5f * Vector2.one;
        }
        if (!mStatusObj.activeInHierarchy)
            mStatusObj.SetActive(true);
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