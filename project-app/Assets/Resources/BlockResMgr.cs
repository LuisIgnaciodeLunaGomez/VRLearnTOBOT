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


    private Func<string, UnityEngine.Object> mABSyncLoad;
    private Action<string, Action<UnityEngine.Object>> mABASyncLoad;
    private Action<string> mABUnload;
   

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
