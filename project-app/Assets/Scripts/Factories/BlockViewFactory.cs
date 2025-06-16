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
 * Versión: 2.0.2
 * 
 * Descripción: Clase que crea las vistas de los bloques y las plantillas de bloques cargando el prefab correcto según la información
 */

using System;
using UnityEngine;
public static class BlockViewFactory
{
    /// <summary>
    /// Crea y configura la vista para un BlockModel dado.
    /// Carga el prefab usando convención de nombres (o BlockResMgr), lo instancia,
    /// obtiene el componente BlockView y llama a su BindModel.
    /// </summary>
    /// <param name="blockModel">El modelo de datos del bloque a visualizar.</param>
    /// <param name="sourceToolbox">Referencia al Toolbox (para obtener WorkspaceView y Color).
    /// Puede ser null si se crea un bloque directamente en el workspace sin pasar por el toolbox,
    /// en cuyo caso se intentará obtener el WorkspaceView activo.</param>
    /// <returns>La BlockView creada y vinculada, o null si falla.</returns>
    public static BlockView CreateView(BlockModel blockModel, BlockListView sourceToolbox, Transform parentTransform)
    {
        if (blockModel == null)
        {
            Debug.LogError("BlockViewFactory.CreateView: BlockModel is null!");
            return null;
        }

        // Obtener la referencia necesaria a WorkSpaceView
        //WorkSpaceView workspaceView = WorkSpaceView.Active;

        WorkSpaceView workspaceView = sourceToolbox?.WorkspaceViewForFactory ?? WorkSpaceView.Active;


        if (workspaceView == null && sourceToolbox is BlockListView scrollList)
        {
            workspaceView = scrollList.WorkspaceViewForFactory; 
        }
        if (workspaceView == null && !blockModel.IsTemplate) // Solo es un problema si no es una plantilla
        {
              Debug.LogWarning($"BlockViewFactory: Could not get WorkspaceView for block {blockModel.Type}. Some interactions might fail if it's not a template.");

        }

        //  Carga e Instanciaciación del Prefab 
        BlockView blockView = null;
        GameObject blockInstance = null;

        // 1. Obtener el Prefab 
        string blockType = blockModel.Type;
        GameObject blockPrefab = BlockResMgr.Get()?.LoadBlockViewPrefab(blockType);

        if (blockPrefab == null)
        {
            Debug.LogError($"<color=red><b>¡PREFAB NO ENCONTRADO!</b></color> BlockViewFactory no encontró el prefab para el tipo de bloque: '{blockType}'. Revisa tu asset 'BlockResSettings' y asegúrate de que la entrada para '{blockType}' está configurada y el prefab está arrastrado correctamente.", BlockResMgr.Get());
            return null; // Devuelve null para detener el proceso.
        }

        // 2. Instanciar el Prefab encontrado
        try
        {
            // Instanciar sin padre inicial. 
            blockInstance = GameObject.Instantiate(blockPrefab, parentTransform);
            blockInstance.name = $"BlockView_{blockType}_{blockModel.ID}"; 


            // Obtener el componente BlockView del Prefab instanciado
            blockView = blockInstance.GetComponent<BlockView>();

            if (blockView == null)
            {
                Debug.LogError($"BlockViewFactory: Prefab for '{blockType}' ({blockPrefab.name}) IS MISSING the BlockView component! Fix the prefab.", blockPrefab);
                GameObject.Destroy(blockInstance); 
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"BlockViewFactory: Error INSTANTIATING prefab '{blockPrefab?.name}' for block '{blockModel.Type}': {e.Message}\n{e.StackTrace}", blockPrefab);
            if (blockInstance != null) GameObject.Destroy(blockInstance); 
            return null;
        }

        //  Vinculación y Configuración Inicial 

        // 3. Vincular la vista creada a su modelo
       
        blockView.BindModel(blockModel, workspaceView); // Pasar modelo y contexto

        // 4. Aplicar Configuración Visual Inicial
     
        Color blockColor = Color.grey; // Default
        if (sourceToolbox != null)
        {
            blockColor = sourceToolbox.GetColorOfBlock(blockType);
        }
        else if (workspaceView?.Toolbox != null)
        { 
            blockColor = workspaceView.Toolbox.GetColorOfBlock(blockType);
        }

        blockView.ChangeBgColor(blockColor);

        return blockView; 
    }
} //Fin clase BlockViewFactory
    