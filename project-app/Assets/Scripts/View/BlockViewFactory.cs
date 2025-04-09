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
 * Versión: 2.0.1
 * 
 * Descripción: Clase que crea las vistas de los bloques y las plantillas de bloques.
 */

using UnityEngine;
public static class BlockViewFactory
{
    public static BlockView CreateView(BlockModel blockModel, BaseToolbox sourceToolbox)
    {
        if (blockModel == null)
        {
            Debug.LogError("BlockViewFactory.CreateView: BlockModel is null!");
            return null;
        }
        if (sourceToolbox == null)
        {
           
            Debug.LogError($"BlockViewFactory.CreateView: sourceToolbox is null for block {blockModel.Type}! Cannot reliably get WorkspaceView.");
            return null;
        }

        BlockListView sourceBlockListView = sourceToolbox as BlockListView; //Por si es necesario más adelante

        WorkSpaceView workspaceView = null;

        if (sourceToolbox is BlockListView scrollList)
        {
            workspaceView = scrollList.WorkspaceViewForFactory;
            if (workspaceView == null)
            {
                Debug.LogError($"BlockViewFactory: Source BlockScrollListView '{sourceToolbox.name}' has null WorkspaceViewForFactory!", sourceToolbox);
                return null; 
            }
        }

        else
        {
           
            Debug.LogError($"BlockViewFactory: SourceToolbox ('{sourceToolbox.name}') is not a BlockScrollListView! Cannot get WorkspaceView ref.", sourceToolbox);
                 return null;
        }

        BlockView blockView=null;
        GameObject blockPrefab = BlockResMgr.Get().LoadBlockViewPrefab(blockModel.Type);
        GameObject blockInstance = null;

        if (blockPrefab != null)
        {
            try
            {
                blockInstance = GameObject.Instantiate(blockPrefab);
                blockInstance.name = $"Block_{blockModel.Type}"; 
                blockView = blockInstance.GetComponent<BlockView>();

                if (blockView == null)
                {
                    Debug.LogWarning($"Prefab for {blockModel.Type} loaded, but it's missing the BlockView component. Adding it.", blockInstance);
                    blockView = blockInstance.AddComponent<BlockView>(); 
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error instantiating prefab for {blockModel.Type}: {e.Message}", blockPrefab);
                if (blockInstance != null) GameObject.Destroy(blockInstance); 
                return null; 
            }
        }

        if (blockView == null)
        {
            if (blockPrefab == null)
            {
                Debug.Log($"Prefab for {blockModel.Type} not found. Using BlockViewBuilder.");
            }
            else
            {
                Debug.LogWarning($"Using BlockViewBuilder for {blockModel.Type} because BlockView component was missing on prefab.");
            }

            try
            {
                blockInstance = BlockViewBuilder.BuildBlockView(blockModel);
                if (blockInstance != null)
                {
                    blockView = blockInstance.GetComponent<BlockView>(); 
                    if (blockView == null)
                    {
                        Debug.LogError($"BlockViewBuilder created instance for {blockModel.Type} but it's missing the BlockView component! Check BuildBlockView implementation.", blockInstance);
                     
                    }
                }
                else
                {
                    Debug.LogError($"BlockViewBuilder failed to build view instance for {blockModel.Type}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during BlockViewBuilder.BuildBlockView for {blockModel.Type}: {e.Message}");
                if (blockInstance != null) GameObject.Destroy(blockInstance); 
                return null;
            }
        }

        /* if (blockView == null)
         {
             Debug.LogError($"BlockViewFactory: CRITICAL - Failed to obtain BlockView component for {block.Type} after trying Prefab and Builder.");
             if (blockInstance != null) GameObject.Destroy(blockInstance);
             return null;
         }*/
        //blockView.InToolbox = isToolboxTemplate;

        Debug.Log($"BlockViewFactory: Assigning WorkspaceView (InstanceID: {workspaceView?.GetInstanceID()}) to BlockView '{blockView.gameObject.name}' BEFORE BindModel.", blockView.gameObject);
       // blockView.workSpaceView = workspaceView;

        blockView.BindModel(blockModel, workspaceView);

        if (blockPrefab != null)
        {
            if (blockModel.Mutator != null)
            {
                Debug.Log($"BlockViewFactory: Prefab used for {blockModel.Type}, checking if InputViews needed for Mutator.");
               
                BlockViewBuilder.BuildInputViews(blockModel, blockView);
            }
        }

        blockView.ChangeBgColor(sourceToolbox.GetColorOfBlock(blockModel.Type));
        blockView.BuildLayout(); 
        blockView.QueueForceLayoutUpdate();

        Debug.Log($"BlockViewFactory: Successfully created and bound BlockView for {blockModel.Type}.", blockView.gameObject);

        return blockView;
    }
} //Fin clase BlockViewFactory
    