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
      

          GameObject blockPrefab = Resources.Load<GameObject>($"Prefabs/BlocksPrefab/{blockData.spriteName}");

         if (blockPrefab == null)
          {
              Debug.LogError($"No se encontró el prefab en 'Prefabs/BlocksPrefab/{blockData.spriteName}'. Verifica la ruta y existencia del archivo.");
              GameObject fallbackObj = new GameObject(blockData.type);
              blockView = fallbackObj.AddComponent<BlockView>();
              blockView.SetWorkSpaceView(workSpaceView); // Asignar WorkSpaceView
              blockView.BindModel(block, blockData);
              return blockView;
          }

          if (blockPrefab != null)
          {
              GameObject blockObj = GameObject.Instantiate(blockPrefab);
              blockObj.name = blockData.type;

              blockView = blockObj.GetComponent<BlockView>();
              if (blockView == null)
              {
                  Debug.LogError($"Prefab '{blockData.spriteName}' at 'Prefabs/BlocksPrefab/{blockData.spriteName}' lacks a BlockView component. Adding one.");
                  blockView = blockObj.AddComponent<BlockView>();
              }

              blockView.BindModel(block, blockData);
              blockView.BuildLayout();
          }
          else
          {
              Debug.LogWarning($"Prefab not found at 'Prefabs/BlocksPrefab/{blockData.spriteName}'. Falling back to default BlockView creation.");

              GameObject fallbackObj = new GameObject(blockData.type);
              blockView = fallbackObj.AddComponent<BlockView>();
              blockView.BindModel(block, blockData);
          }

          return blockView;
      }
}
