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
 * Versión: 2.0.0
 * 
 * Descripción: Clase que crea las vistas de los bloques y las plantillas de bloques.
 */

using UnityEngine;
public static class BlockViewFactory
{
    public static BlockView CreateView(BlockModel block, BaseToolbox sourceToolbox)
    {
        BlockScrollListView sourceBlockScrollListView = sourceToolbox as BlockScrollListView;

        BlockListView sourceBlockListView = sourceToolbox as BlockListView; //Por si es necesario más adelante

        WorkSpaceView workspaceView = null;

        /*if (sourceBlockListView == null)
        {
            Debug.LogError($"BlockViewFactory: SourceToolbox is not a BlockListView! Cannot get WorkspaceView ref.");
            return null;
        }
        if (sourceToolbox == null)
        {
            Debug.LogError("BlockViewFactory.CreateView called with a null sourceToolbox!");
            return null;
        }
        
        workspaceView = sourceBlockListView.WorkspaceViewForFactory;
        
        if (workspaceView == null)
        {
            Debug.LogError($"BlockViewFactory: Source BlockListView '{sourceToolbox.name}' has null WorkspaceViewForFactory!", sourceToolbox);
            return null;
        }*/

        if (sourceBlockScrollListView != null)
        {
            workspaceView = sourceBlockScrollListView.WorkspaceViewForFactory;
            if (workspaceView == null)
            {
                Debug.LogError($"BlockViewFactory: Source BlockScrollListView '{sourceToolbox.name}' has null WorkspaceViewForFactory!", sourceToolbox);
                return null;
            }
        }
        else if (sourceBlockListView != null) 
        {
            workspaceView = sourceBlockListView.WorkspaceViewForFactory;
            if (workspaceView == null)
            {
                Debug.LogError($"BlockViewFactory: Source BlockListView '{sourceToolbox.name}' has null WorkspaceViewForFactory!", sourceToolbox);
                return null;
            }
        }
        else 
        {
            Debug.LogError($"BlockViewFactory: SourceToolbox ('{sourceToolbox.name}') is neither a BlockScrollListView nor a BlockListView! Cannot get WorkspaceView ref.");
            return null;
        }


        BlockView blockView=null;

        GameObject blockPrefab = BlockResMgr.Get().LoadBlockViewPrefab(block.Type);
        GameObject blockInstance = null;

        if (blockPrefab != null)
        {
            blockInstance = GameObject.Instantiate(blockPrefab);
            blockInstance.name = $"{block.Type}_View";
            blockView = blockInstance.GetComponent<BlockView>();

            //BlockScrollListView bslv = sourceToolbox as BlockScrollListView;
        }
        else
        {
            Debug.LogWarning($"Prefab for {block.Type} not found. Using BlockViewBuilder.");
            blockInstance = BlockViewBuilder.BuildBlockView(block);
            if (blockInstance != null)
            {
                blockView = blockInstance.GetComponent<BlockView>();
             
            }
        
               }

        /*if (blockView == null)
        {
            Debug.LogError($"Failed to get or create BlockView component for {block.Type}. Cleaning up instantiated object.");
            if (blockInstance != null) GameObject.Destroy(blockInstance);
            return null;
        }*/

        //blockView.ChangeBgColor(WorkSpaceView.Active.Toolbox.GetColorOfBlockView(blockView));
        //Color blockColor = sourceToolbox.GetColorOfBlock(block.Type);
        //blockView.ChangeBgColor(blockColor);
        if (blockView != null)
        {
            blockView.workSpaceView  = workspaceView; 
            blockView.BindModel(block, workspaceView);             

            if (blockPrefab != null) 
            {
                if (block.Mutator != null) 
                    BlockViewBuilder.BuildInputViews(block, blockView); 
                blockView.BuildLayout(); 
            }
        }
        else
        {
            Debug.LogError($"Failed to get or create BlockView component for {block.Type}. Cleaning up instantiated object.");
            if (blockInstance != null) GameObject.Destroy(blockInstance);
            return null;
        }
        blockView.ChangeBgColor(sourceToolbox.GetColorOfBlock(block.Type));
        blockView.BuildLayout(); 
        blockView.QueueForceLayoutUpdate();

        return blockView;
    }
}
    