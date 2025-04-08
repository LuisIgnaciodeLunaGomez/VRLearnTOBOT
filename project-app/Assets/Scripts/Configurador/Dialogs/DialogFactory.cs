
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

        // Intenta obtener el Canvas raíz del WorkSpaceView activo
        if (WorkSpaceView.Active?.RootCanvas != null)
        {
            return WorkSpaceView.Active.RootCanvas.transform;
        }

        // Fallback si no hay Workspace activo o Canvas raíz
        Debug.LogError("DialogFactory: Cannot create dialog. No parent provided and cannot find active WorkSpaceView with RootCanvas.");
        return null; // O lanzar una excepción
    }
    public static BaseDialog CreateDialog(string dialogId, Transform parent = null)
    {
        GameObject prefab = BlockResMgr.Get().LoadDialogPrefab(dialogId);
        if (prefab == null)
            throw new Exception($"Can't find dialog prefab for '{dialogId}'. Please ensure you configure it in BlockResSettings.");

        Transform finalParent = GetDefaultParent(parent);
        if (finalParent == null) return null; // No se pudo determinar el padre

        // Instanciar una sola vez con el padre correcto
        GameObject dialogObj = GameObject.Instantiate(prefab, finalParent, false);

        BaseDialog dialog = dialogObj.GetComponent<BaseDialog>();
        if (dialog == null)
        {
            // Importante: Destruir el objeto si el componente esperado no existe
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
        // Validar entrada
        if (block?.Mutator == null)
        {
            Debug.LogWarning("CreateMutatorDialog called with null block or block without a mutator.");
            return null;
        }

        // Obtener prefab
        GameObject prefab = BlockResMgr.Get().LoadDialogPrefab(block.Mutator.MutatorId);
        if (prefab == null)
        {
            Debug.LogError($"Can't find dialog prefab for mutator '{block.Mutator.MutatorId}'. Check BlockResSettings.");
            return null; // O lanzar excepción
        }

        // Obtener padre final
        Transform finalParent = GetDefaultParent(parent);
        if (finalParent == null)
        {
            Debug.LogError($"DialogFactory.CreateMutatorDialog: Failed to get a valid parent transform for mutator '{block.Mutator.MutatorId}'. Cannot instantiate.");
            return null;
        }

        // Instanciar
        GameObject dialogObj = GameObject.Instantiate(prefab, finalParent, false);

        // Obtener y verificar componente BaseDialog
        BaseDialog dialog = dialogObj.GetComponent<BaseDialog>();
        if (dialog == null)
        {
            Debug.LogError($"Prefab for mutator '{block.Mutator.MutatorId}' does not contain a BaseDialog component.", dialogObj);
            GameObject.Destroy(dialogObj);
            return null;
        }

        dialog.Init(block); // Pasar el bloque al Init
        return dialog;
    }

    public static T CreateMutatorDialog<T>(BlockModel block, Transform parent = null) where T : BaseDialog
    {
        return CreateMutatorDialog(block, parent) as T;
    }

    public static FieldDialog CreateFieldDialog(FieldModel field, Transform parent = null)
    {
        // 1. Validar entrada
        if (field == null)
        {
            Debug.LogWarning("CreateFieldDialog called with null field.");
            return null;
        }

        // 2. Obtener el Prefab (asume que field.Type es el ID del diálogo/prefab)
        GameObject prefab = BlockResMgr.Get().LoadDialogPrefab(field.Type);
        if (prefab == null)
        {
            // Es mejor usar string interpolation ($"...") para mensajes de error claros
            Debug.LogError($"Can't find dialog prefab for field type '{field.Type}'. Check BlockResSettings.");
            // Puedes lanzar una excepción o devolver null según prefieras el manejo de errores
            // throw new Exception($"Can't find dialog prefab for field type '{field.Type}'. Please ensure you configure it in BlockResSettings.");
            return null;
        }

        // 3. Determinar el Padre Correcto (¡LA CORRECCIÓN PRINCIPAL!)
        //    Ya NO se usa: if (parent == null) parent = UICanvas.transform;
        Transform finalParent = GetDefaultParent(parent);
        if (finalParent == null)
        {
            // GetDefaultParent ya mostró un error. No podemos continuar.
            Debug.LogError($"DialogFactory.CreateFieldDialog: Failed to get a valid parent transform for field type '{field.Type}'. Cannot instantiate.");
            return null;
        }

        // 4. Instanciar el GameObject usando el padre determinado
        GameObject dialogObj = GameObject.Instantiate(prefab, finalParent, false);

        // 5. Obtener el componente FieldDialog y Validarlo
        FieldDialog dialog = dialogObj.GetComponent<FieldDialog>();
        if (dialog == null)
        {
            Debug.LogError($"Prefab for field type '{field.Type}' does not contain a FieldDialog component.", dialogObj);
            GameObject.Destroy(dialogObj); // ¡Importante! Limpiar si el prefab es incorrecto
            return null;
        }

        // 6. Inicializar el diálogo con el FieldModel
        dialog.Init(field);

        // 7. Devolver el diálogo creado
        return dialog;
    }

    public static T CreateFieldDialog<T>(FieldModel field, Transform parent = null) where T : FieldDialog
    {
        // Llama a la versión corregida no genérica
        FieldDialog createdDialog = CreateFieldDialog(field, parent);

        // Intenta hacer el cast a T. Devuelve null si la creación falló o si el tipo no coincide.
        return createdDialog as T;
    }
}
