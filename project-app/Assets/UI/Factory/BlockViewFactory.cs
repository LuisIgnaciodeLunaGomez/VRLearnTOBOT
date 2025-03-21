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


using UnityEngine;

public static class BlockViewFactory
{
     public static BlockView CreateView(Block block, BlockDataLoader.BlockData blockData, WorkSpaceView workSpaceView)
      {
        
        BlockView blockView = null;
            Debug.Log($"CreateView: BlockView: Asignando WorkSpaceView: {workSpaceView != null}");
            Debug.Log($"CreateView: BlockView: Creando BlockView para {blockData.spriteName}");

        GameObject blockPrefab = Resources.Load<GameObject>($"Prefabs/BlocksPrefab/{blockData.spriteName}");

         if (blockPrefab == null)
          {
              Debug.LogError($"No se encontró el prefab en 'Prefabs/BlocksPrefab/{blockData.spriteName}'. Verifica la ruta y existencia del archivo.");
              GameObject fallbackObj = new GameObject(blockData.type);
              blockView = fallbackObj.AddComponent<BlockView>();
              blockView.SetWorkSpaceView(workSpaceView); // Asignar WorkSpaceView

            if (block == null)
            {
                Debug.LogError("CreateView: BlockView: El objeto Block es nulo.");
                return null;
            }

            if (workSpaceView == null)
            {
                Debug.LogError("CreateView: BlockView: El objeto WorkSpaceView es nulo.");
                return null;
            }

            if (blockData == null)
            {
                Debug.LogError("CreateView: BlockView:  El objeto BlockData es nulo.");
                return null;
            }
            blockView.BindModel(block, blockData, workSpaceView); //Vincula datos del bloque (block y blockData)
              return blockView;
          }

        if (blockPrefab != null)
        {
            GameObject blockObj = GameObject.Instantiate(blockPrefab);
            blockObj.name = blockData.type;

            blockView = blockObj.GetComponent<BlockView>();
            if (blockView == null)
            {
                Debug.LogError($"Prefab '{blockData.spriteName}' en 'Prefabs/BlocksPrefab/{blockData.spriteName}' No stenía el componente BlockView creado, se le añade uno.");
                blockView = blockObj.AddComponent<BlockView>();
            }

            blockView.BindModel(block, blockData, workSpaceView);
            blockView.BuildLayout();
        }
        else
        {
            Debug.LogWarning($"Prefab no encontrado en 'Prefabs/BlocksPrefab/{blockData.spriteName}'.");

        }

          return blockView;
      }
}
