/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 22/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */


using System;
using System.Collections.Generic;
using UnityEngine;

public class WorkSpaceView : MonoBehaviour, IWorkSpaceView
{

    [SerializeField] private RectTransform m_CodingArea; //Panel donde se van a mostrar los bloques
    private Dictionary<string, BlockView> m_blockViews = new Dictionary<string, BlockView>(); // Diccionario de bloques en la vista
    private WorkSpace m_workSpace; // Espacio de trabajo

    RectTransform IWorkSpaceView.CodingArea => m_CodingArea;

    public void BindModel(WorkSpace workSpace)
    {
        if (this.m_workSpace != null)
        {
            UnBindModel(); // Desvincular el modelo antes de vincular uno nuevo
        }

        this.m_workSpace = workSpace;

        GameObject middelPanel = GameObject.Find("MiddlePanel");
        if (middelPanel != null)
        {
            m_CodingArea = middelPanel.GetComponent<RectTransform>();
        }

        if (workSpace.GetAllBlocks().Count > 0)
        {
            BuildViews();
        }
    }

    public void UnBindModel()
    {
        m_workSpace = null;
        m_blockViews.Clear();

    }

    private void BuildViews()
    {
        foreach (Block block in m_workSpace.GetAllBlocks())
        {
            CreateBlockView(block);

        }
    }

    private BlockView CreateBlockView(Block block)
    {
        BlockView view = BlockViewFactory.CreateView(block);

        view.inToolBox = false; //Indica que el bloque no está en el toolbox

        view.UpdatePosition(block.XY);
        m_blockViews[block.ID] = view;
        return view;
    }

    public void CleanViews()
    {
        foreach (BlockView view in m_blockViews.Values)
        {
            Destroy(view.gameObject);
        }
        m_blockViews.Clear();
    }

    void IWorkSpaceView.AddBlockView(IBlockView blockView)
    {
        BlockView view = blockView as BlockView;
        if (view != null && !m_blockViews.ContainsKey(view.Block.ID))
        {
            view.transform.SetParent(m_CodingArea, false);
            m_blockViews[view.Block.ID] = view;
        }
    }

    void IWorkSpaceView.RemoveBlockView(IBlockView blockView)
    {
        BlockView view = blockView as BlockView;
        if (view != null && m_blockViews.ContainsKey(view.Block.ID))
        {
            Destroy(view.gameObject);
            m_blockViews.Remove(view.Block.ID);
        }
    }
    
    public void Dispose()
    {
        UnBindModel();

        BlockViewSettings.Dispose();
        Resources.UnloadUnusedAssets();
    }


}
