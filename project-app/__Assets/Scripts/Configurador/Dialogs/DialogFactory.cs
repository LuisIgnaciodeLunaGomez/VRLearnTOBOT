
/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */
using System;
using UnityEngine;

public static class DialogFactory
{
    internal static readonly string VARIABLE_NAME_DIALOG_NAME = "DefaultVariableNameDialogId";

    private static Transform GetDefaultParent(Transform requestedParent)
    {
        if (requestedParent != null)
        {
            return requestedParent;
        }

        if (WorkSpaceView.Active?.RootCanvas != null)
        {
            return WorkSpaceView.Active.RootCanvas.transform;
        }

        Debug.LogError("DialogFactory: Cannot create dialog. No parent provided and cannot find active WorkSpaceView with RootCanvas.");
        return null; 
    }
    public static BaseDialog CreateDialog(string dialogId, Transform parent = null)
    {
        GameObject prefab = BlockResMgr.Get().LoadDialogPrefab(dialogId);
        if (prefab == null)
            throw new Exception($"Can't find dialog prefab for '{dialogId}'. Please ensure you configure it in BlockResSettings.");

        Transform finalParent = GetDefaultParent(parent);
        if (finalParent == null) return null; 

        GameObject dialogObj = GameObject.Instantiate(prefab, finalParent, false);

        BaseDialog dialog = dialogObj.GetComponent<BaseDialog>();
        if (dialog == null)
        {
            Debug.LogError($"Prefab for dialogId '{dialogId}' does not contain a BaseDialog component.", dialogObj);
            GameObject.Destroy(dialogObj);
            return null;
        }

        dialog.Init();
        return dialog;
    }

    public static BaseDialog CreateDialog<T>(string dialogId, Transform parent = null) where T : BaseDialog
    {
        return CreateDialog(dialogId, parent) as T;
    }

    public static BaseDialog CreateMutatorDialog(BlockModel block, Transform parent = null)
    {
        if (block?.Mutator == null)
        {
            Debug.LogWarning("CreateMutatorDialog called with null block or block without a mutator.");
            return null;
        }

        GameObject prefab = BlockResMgr.Get().LoadDialogPrefab(block.Mutator.MutatorId);
        if (prefab == null)
        {
            Debug.LogError($"Can't find dialog prefab for mutator '{block.Mutator.MutatorId}'. Check BlockResSettings.");
            return null; 
        }

        Transform finalParent = GetDefaultParent(parent);
        if (finalParent == null)
        {
            Debug.LogError($"DialogFactory.CreateMutatorDialog: Failed to get a valid parent transform for mutator '{block.Mutator.MutatorId}'. Cannot instantiate.");
            return null;
        }

        GameObject dialogObj = GameObject.Instantiate(prefab, finalParent, false);

        BaseDialog dialog = dialogObj.GetComponent<BaseDialog>();
        if (dialog == null)
        {
            Debug.LogError($"Prefab for mutator '{block.Mutator.MutatorId}' does not contain a BaseDialog component.", dialogObj);
            GameObject.Destroy(dialogObj);
            return null;
        }

        dialog.Init(block); 
        return dialog;
    }

    public static T CreateMutatorDialog<T>(BlockModel block, Transform parent = null) where T : BaseDialog
    {
        return CreateMutatorDialog(block, parent) as T;
    }

    public static FieldDialog CreateFieldDialog(FieldModel field, Transform parent = null)
    {
        if (field == null)
        {
            Debug.LogWarning("CreateFieldDialog called with null field.");
            return null;
        }

        GameObject prefab = BlockResMgr.Get().LoadDialogPrefab(field.Type);
        if (prefab == null)
        {
            Debug.LogError($"Can't find dialog prefab for field type '{field.Type}'. Check BlockResSettings.");
               return null;
        }

           Transform finalParent = GetDefaultParent(parent);
        if (finalParent == null)
        {
            Debug.LogError($"DialogFactory.CreateFieldDialog: Failed to get a valid parent transform for field type '{field.Type}'. Cannot instantiate.");
            return null;
        }

        GameObject dialogObj = GameObject.Instantiate(prefab, finalParent, false);

        FieldDialog dialog = dialogObj.GetComponent<FieldDialog>();
        if (dialog == null)
        {
            Debug.LogError($"Prefab for field type '{field.Type}' does not contain a FieldDialog component.", dialogObj);
            GameObject.Destroy(dialogObj); 
            return null;
        }

        dialog.Init(field);

        return dialog;
    }

    public static T CreateFieldDialog<T>(FieldModel field, Transform parent = null) where T : FieldDialog
    {
        FieldDialog createdDialog = CreateFieldDialog(field, parent);

        return createdDialog as T;
    }
}//Fin clase DialogFactory
