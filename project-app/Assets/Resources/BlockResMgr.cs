/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha:01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Integración de la estructura de Ublockly dentro del proyecto por semejanza con ScratchBlocks. 
 */

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

[Serializable]
public class BlockResParam
{
    public string IndexName;
    public string ResName;
}

[Serializable]
public class BlockObjectParam : BlockResParam
{
    public GameObject Prefab;
}

[Serializable]
public class BlockTextResParam : BlockResParam
{
    public TextAsset TextFile;
}

[Serializable]
public class BlockTextResWithSelectionParam : BlockTextResParam
{
    public bool Selected;
}

 
[CreateAssetMenu(menuName = "UBlockly/BlockResSettings", fileName = "BlockResSettings")]
public class BlockResMgr : ScriptableObject
{
    [SerializeField] private BlockResLoadType m_LoadType;
    [SerializeField] private List<BlockTextResWithSelectionParam> m_I18nFiles;
    [SerializeField] private List<BlockTextResParam> m_BlockJsonFiles;
    [SerializeField] private List<BlockTextResWithSelectionParam> m_ToolboxFiles;
    [SerializeField] public string m_BlockViewPrefabPath;
    [SerializeField] private List<BlockObjectParam> m_BlockViewPrefabs;
    [SerializeField] private List<BlockObjectParam> m_DialogPrefabs;

    public BlockResLoadType LoadType
    {
        get { return m_LoadType; }
    }
    public string BlockViewPrefabPath
    {
        get { return m_BlockViewPrefabPath; }
    }

    private Func<string, UnityEngine.Object> mABSyncLoad;
    private Action<string, Action<UnityEngine.Object>> mABASyncLoad;
    private Action<string> mABUnload;

 
    public void SetAssetbundleSyncLoadDelegate(Func<string, UnityEngine.Object> del)
    {
        mABSyncLoad = del;
    }


    public void SetAssetbundleASyncLoadDelegate(Action<string, Action<UnityEngine.Object>> del)
    {
        mABASyncLoad = del;
    }

   
    public void SetAssetbundleUnloadDelegate(Action<string> del)
    {
        mABUnload = del;
    }

    #region I18n Files

    public void LoadI18n()
    {
        if (m_I18nFiles == null || m_I18nFiles.Count == 0)
        {
            Debug.LogError("LoadI18n failed. Please assign i18n files to BlockResSettings.asset.");
            return;
        }

        var i18nSelected = m_I18nFiles.FindAll(file => file.Selected);
        if (i18nSelected.Count == 0)
        {
            Debug.LogWarning("Please select an i18n file in BlockResSettings.asset. Default select \'en\'.");
            i18nSelected.Add(m_I18nFiles.Find(file => file.IndexName == "en"));
        }
        else if (i18nSelected.Count > 1)
        {
            Debug.LogWarning("You have selected more than one i18n files in BlockResSettings.asset. The first one will be used.");
        }

        var resParam = i18nSelected[0];
        TextAsset textAsset = null;
        switch (m_LoadType)
        {
            case BlockResLoadType.Assetbundle:
                if (mABSyncLoad != null)
                    textAsset = mABSyncLoad(resParam.ResName) as TextAsset;
                break;
            case BlockResLoadType.Resources:
                textAsset = Resources.Load<TextAsset>(resParam.ResName);
                break;
            case BlockResLoadType.Serialized:
                textAsset = resParam.TextFile;
                break;
        }
        if (textAsset != null)
        {
            I18n.AddI18nFile(textAsset.text);
            if (m_LoadType == BlockResLoadType.Assetbundle && mABUnload != null)
                mABUnload(resParam.ResName);
        }

        Debug.Log("Select I18n: " + resParam.IndexName);
    }

    #endregion

    public ToolboxConfig LoadToolboxConfig()
    {
        if (m_ToolboxFiles == null || m_ToolboxFiles.Count == 0)
        {
            Debug.LogError("Load Toolbox config failed. Please assign toolbox config files to BlockResSettings.asset.");
            return null;
        }

        var configSelected = m_ToolboxFiles.FindAll(file => file.Selected);
        if (configSelected.Count == 0)
        {
            Debug.LogWarning("Please select a toolbox config file in BlockResSettings.asset. Default select \'default\'.");
            var defaultFile = m_ToolboxFiles.Find(file => file.IndexName == "default");

           // configSelected.Add(m_ToolboxFiles.Find(file => file.IndexName == "default"));
            if (defaultFile == null)
            {
                Debug.LogError("No default toolbox config file found in BlockResSettings.");
                return null;
            }
            configSelected.Add(defaultFile);
        }
        else if (configSelected.Count > 1)
        {
            Debug.LogWarning("Please select a toolbox config XML file in BlockResSettings.asset. Default select 'default'.");
            
            //Debug.LogWarning("You have selected more than one toolbox config files in BlockResSettings.asset. The first one will be used.");
        }

        var resParam = configSelected[0];
        TextAsset textAsset = null;
        switch (m_LoadType)
        {
            case BlockResLoadType.Assetbundle:
                if (mABSyncLoad != null)
                    textAsset = mABSyncLoad(resParam.ResName) as TextAsset;
                break;
            case BlockResLoadType.Resources:
                textAsset = Resources.Load<TextAsset>(resParam.ResName);
                break;
            case BlockResLoadType.Serialized:
                textAsset = resParam.TextFile;
                break;
        }

        if (textAsset == null)
            return null;

        //ToolboxConfig toolboxConfig = JsonUtility.FromJson<ToolboxConfig>(textAsset.text);
        if (m_LoadType == BlockResLoadType.Assetbundle && mABUnload != null)
            mABUnload(resParam.ResName);

        try
        {
            ToolboxConfig toolboxConfig = new ToolboxConfig();
            toolboxConfig.BlockCategoryList = new List<ToolboxBlockCategory>();

            XDocument doc = XDocument.Parse(textAsset.text);

            XElement toolboxElement = doc.Root;
            if (toolboxElement == null)
            {
                Debug.LogError("Invalid Toolbox XML: Missing root element.");
                return null;
            }

           
            toolboxConfig.Style = toolboxElement.Attribute("style")?.Value ?? "default";

            foreach (XElement element in toolboxElement.Elements())
            {
                if (element.Name.LocalName.Equals("category", StringComparison.OrdinalIgnoreCase))
                {
                    ToolboxBlockCategory category = new ToolboxBlockCategory();
                    category.CategoryName = element.Attribute("name")?.Value; 
                    category.Colour = element.Attribute("colour")?.Value;    
                    category.Custom = element.Attribute("custom")?.Value;   
                    category.BlockList = new List<string>();

                    if (string.IsNullOrEmpty(category.CategoryName))
                    {
                        Debug.LogWarning($"Found category with missing 'name' attribute in toolbox XML.");
                        continue;
                    }

                    foreach (XElement blockElement in element.Elements("block"))
                    {
                        string blockType = blockElement.Attribute("type")?.Value;
                        if (!string.IsNullOrEmpty(blockType))
                        {
                            category.BlockList.Add(blockType);
                        }
                    }
                    toolboxConfig.BlockCategoryList.Add(category);
                }
               else if (element.Name.LocalName.Equals("sep", StringComparison.OrdinalIgnoreCase))
                {
                    ToolboxBlockCategory separator = new ToolboxBlockCategory
                    {
                        CategoryName = "---SEP---",
                        Custom = "SEPARATOR"
                    };
                    toolboxConfig.BlockCategoryList.Add(separator);
                }
            }

            if (m_LoadType == BlockResLoadType.Assetbundle && mABUnload != null)
                mABUnload(resParam.ResName);

            return toolboxConfig;

        }
        catch (System.Xml.XmlException ex)
        {
            Debug.LogError($"Error parsing Toolbox XML ({resParam.ResName}): {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected error processing Toolbox XML ({resParam.ResName}): {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    #region Block View Prefabs
    public GameObject LoadBlockViewPrefab(string blockType)
    {
        if (m_BlockViewPrefabs == null || m_BlockViewPrefabs.Count == 0)
            return null;

        BlockObjectParam resParam = m_BlockViewPrefabs.Find(o => o.IndexName.Equals(blockType));
        if (resParam == null)
            return null;

        GameObject blockPrefab = null;
        switch (m_LoadType)
        {
            case BlockResLoadType.Assetbundle:
                if (mABSyncLoad != null)
                    blockPrefab = mABSyncLoad(resParam.ResName) as GameObject;
                break;
            case BlockResLoadType.Resources:
                blockPrefab = Resources.Load<GameObject>(resParam.ResName);
                break;
            case BlockResLoadType.Serialized:
                blockPrefab = resParam.Prefab;
                break;
        }
        return blockPrefab;
    }

    public void UnloadBlockViewPrefab(string blockType)
    {
        if (m_BlockViewPrefabs == null || m_BlockViewPrefabs.Count == 0)
            return;

        BlockObjectParam resParam = m_BlockViewPrefabs.Find(o => o.IndexName.Equals(blockType));
        if (resParam == null)
            return;

        if (m_LoadType == BlockResLoadType.Assetbundle && mABUnload != null)
            mABUnload(resParam.ResName);
    }

    public void AddBlockViewPrefab(GameObject blockPrefab)
    {
        if (m_BlockViewPrefabs == null)
            m_BlockViewPrefabs = new List<BlockObjectParam>();

        string prefabName = blockPrefab.name.Replace("(Clone)", "");
        string indexName = prefabName.Substring("Block_".Length);
        if (m_BlockViewPrefabs.Exists(o => o.IndexName.Equals(indexName)))
            return;

        BlockObjectParam resParam = new BlockObjectParam();
        resParam.IndexName = indexName;
        resParam.ResName = prefabName;
        if (m_LoadType == BlockResLoadType.Serialized)
            resParam.Prefab = blockPrefab;
        m_BlockViewPrefabs.Add(resParam);
    }

    public void ClearBlockViewPrefabs()
    {
        m_BlockViewPrefabs.Clear();
    }

    #endregion

    #region Dialog Prefabs

    public GameObject LoadDialogPrefab(string dialogId)
    {
        if (m_DialogPrefabs == null || m_DialogPrefabs.Count == 0)
            return null;

        BlockObjectParam resParam = m_DialogPrefabs.Find(o => o.IndexName.Equals(dialogId));
        if (resParam == null)
            return null;

        GameObject dialogPrefab = null;
        switch (m_LoadType)
        {
            case BlockResLoadType.Assetbundle:
                if (mABSyncLoad != null)
                    dialogPrefab = mABSyncLoad(resParam.ResName) as GameObject;
                break;
            case BlockResLoadType.Resources:
                dialogPrefab = Resources.Load<GameObject>(resParam.ResName);
                break;
            case BlockResLoadType.Serialized:
                dialogPrefab = resParam.Prefab;
                break;
        }
        return dialogPrefab;
    }

    public void UnloadDialogPrefab(string dialogId)
    {
        if (m_DialogPrefabs == null || m_DialogPrefabs.Count == 0)
            return;

        BlockObjectParam resParam = m_DialogPrefabs.Find(o => o.IndexName.Equals(dialogId));
        if (resParam == null)
            return;

        if (m_LoadType == BlockResLoadType.Assetbundle && mABUnload != null)
            mABUnload(resParam.ResName);
    }

    #endregion

    public Texture2D LoadTexture(string texName)
    {
        if (mABSyncLoad != null)
            return mABSyncLoad(texName) as Texture2D;
        return Resources.Load<Texture2D>(texName);
    }

    public void UnloadTexture(string texName)
    {
        if (mABUnload != null)
            mABUnload(texName);
    }

    private static BlockResMgr mInstance = null;
    public static BlockResMgr Get()
    {
        if (mInstance == null)
            mInstance = Resources.Load<BlockResMgr>("BlockResSettings");
        if (mInstance == null)
            throw new Exception("There is no \"BlockResSettings\" ScriptObject under Resources folder");

        return mInstance;
    }

    public static void Dispose()
    {
        mInstance = null;
    }
}
