/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 08/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 * 
 */


using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


public class BlockViewEditor
{
    [MenuItem("UBlockly/Build Block Prefabs")]
    static void BuildBlockPrefabs()
    {
        ScratchBlocks.Dispose();
        ScratchBlocks.Init();
        WorkSpaceModel workspace = new WorkSpaceModel();

        BlockResMgr resMgr = BlockResMgr.Get();
        if (resMgr == null)
        {
            Debug.LogError("BlockResMgr instance not found! Cannot build prefabs.");
            return;
        }

        string prefabPath = resMgr.BlockViewPrefabPath;
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("BlockViewPrefabPath is not set in BlockResMgr! Cannot save prefabs.");
            return;
        }
        
        if (!prefabPath.EndsWith("/"))
        {
            prefabPath += "/";
        }


        var blocks = BlockFactory.Instance.GetAllBlockDefinitions().Keys;

        /*if (!Directory.Exists(BlockResMgr.Get().BlockViewPrefabPath))
            Debug.Log("BlockViewPrefabPath: " + BlockResMgr.Get().BlockViewPrefabPath);
        Directory.CreateDirectory(BlockResMgr.Get().BlockViewPrefabPath);*/

        if (!Directory.Exists(prefabPath))
        {
            Debug.Log("Creating Block Prefab Directory: " + prefabPath);
            Directory.CreateDirectory(prefabPath);
        }

        BlockResMgr.Get().ClearBlockViewPrefabs();

        try
        {
            int index = 0;
            int count = blocks.Count();
            foreach (string blockType in blocks)
            {
                //EditorUtility.DisplayProgressBar(null, "Building block prefab: " + name, index / (float)count);

                EditorUtility.DisplayProgressBar("Building Block Prefabs", $"Processing: {blockType} ({index + 1}/{count})", index / (float)count);

                /*
                BlockModel block = workspace.NewBlock(name);
                GameObject obj = BlockViewBuilder.BuildBlockView(block);

                string path = BlockResMgr.Get().BlockViewPrefabPath + obj.name + ".prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(obj, path, InteractionMode.UserAction);
                BlockResMgr.Get().AddBlockViewPrefab(prefab);

                GameObject.DestroyImmediate(obj);*/
                
                BlockModel block = null;
                GameObject obj = null; 
                try
                {
                    block = workspace.NewBlock(blockType); 
                    if (block == null)
                    {
                        Debug.LogError($"Failed to create block model for type: {blockType}");
                        continue; 
                    }

                    
                    obj = BlockViewBuilder.BuildBlockView(block); 
                    if (obj == null)
                    {
                        Debug.LogError($"BlockViewBuilder returned null for type: {blockType}");
                        continue;
                    }

                    string filePath = prefabPath + obj.name + ".prefab"; 
                    GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, filePath); 

                    if (prefab != null)
                    {
                        resMgr.AddBlockViewPrefab(prefab);
                    }
                    else
                    {
                        Debug.LogError($"Failed to save prefab for {blockType} at {filePath}");
                    }

                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing block type '{blockType}': {e.Message}\nStackTrace: {e.StackTrace}");
                }
                finally
                {
                    if (obj != null)
                        GameObject.DestroyImmediate(obj);
                    if (block != null)
                        block.Dispose(false);
                }
                index++;
            }
        }
        finally
        {
            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();
            workspace.Dispose();
            EditorUtility.ClearProgressBar();
            Debug.Log("Block Prefab build finished.");
            Resources.UnloadUnusedAssets();
        }
    }
}
