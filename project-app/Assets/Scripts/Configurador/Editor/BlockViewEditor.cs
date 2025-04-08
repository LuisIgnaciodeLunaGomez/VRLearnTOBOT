
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

        var blocks = BlockFactory.Instance.GetAllBlockDefinitions().Keys;

        if (!Directory.Exists(BlockResMgr.Get().BlockViewPrefabPath))
            Debug.Log("BlockViewPrefabPath: " + BlockResMgr.Get().BlockViewPrefabPath);
        Directory.CreateDirectory(BlockResMgr.Get().BlockViewPrefabPath);

        BlockResMgr.Get().ClearBlockViewPrefabs();

        try
        {
            int index = 0;
            int count = blocks.Count();
            foreach (string name in blocks)
            {
                EditorUtility.DisplayProgressBar(null, "Building block prefab: " + name, index / (float)count);

                BlockModel block = workspace.NewBlock(name);
                GameObject obj = BlockViewBuilder.BuildBlockView(block);

                string path = BlockResMgr.Get().BlockViewPrefabPath + obj.name + ".prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(obj, path, InteractionMode.UserAction);
                BlockResMgr.Get().AddBlockViewPrefab(prefab);

                GameObject.DestroyImmediate(obj);

                index++;
            }
        }
        finally
        {
            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();

            EditorUtility.ClearProgressBar();
        }
    }
}
