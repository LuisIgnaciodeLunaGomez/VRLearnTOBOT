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
 * Versión: 2.0.0
 * 
 * Descripción: Proporcionar una API segura para que otros controladores (InputController, BlockDragController, ExecutionController) modifiquen el WorkspaceModel.
 * 
 * Aplicar reglas de negocio o validaciones antes de confirmar cambios en el modelo.
 *
 * Orquestar acciones complejas que involucran múltiples modelos (conectar dos bloques).
 *
 * Gestionar el historial de Undo/Redo.
 * 
 */

using System;
using System.Collections.Generic;
using UnityEngine;


public class WorkspaceController : MonoBehaviour
{
   
    public static WorkspaceController Instance { get; private set; }

    private WorkSpaceModel m_WorkspaceModel; 
    [SerializeField] private WorkSpaceView m_WorkspaceView; 

    public bool IsReadOnly() => m_WorkspaceModel?.Options.ReadOnly ?? true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this; 

        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }
    public void InitializeController(WorkSpaceModel workspace, WorkSpaceView view)
    {
        m_WorkspaceModel = workspace ?? throw new ArgumentNullException(nameof(workspace));
        m_WorkspaceView = view;
        if (m_WorkspaceView == null) m_WorkspaceView = FindFirstObjectByType<WorkSpaceView>();
        if (m_WorkspaceView == null) Debug.LogError("WorkspaceController: WorkSpaceView reference is missing!", this.gameObject);
        Debug.Log("WorkspaceController Initialized with UBlockly.Workspace.");
    }

    #region API para Otros Controladores


    public BlockModel ConfirmAddBlock(BlockModel potentialBlock)
    {

        BlockModel ublocklyBlock = potentialBlock as BlockModel; 

        if (ublocklyBlock == null)
        {
            Debug.LogError("WorkspaceController.ConfirmAddBlock: Expected a UBlockly.BlockModel but received a different type.");
            return null; 
        }


        if (IsReadOnly() || m_WorkspaceModel == null) return null;


        ublocklyBlock.SetParent(null); 

        Debug.Log($"WorkspaceController: Confirmed and added BlockModel {ublocklyBlock.ID} to UBlockly Workspace TopBlocks.");

   
        return ublocklyBlock; 
    }


    public bool RequestBlockUnplug(BlockModel blockToUnplug, bool healStack) 
    {
        if (IsReadOnly() || blockToUnplug == null || m_WorkspaceModel == null) return false;

        blockToUnplug.UnPlug(healStack);


        Debug.Log($"WorkspaceController: Requested Unplug BlockModel {blockToUnplug.ID}.");
        return true; 
    }


    public void RequestBlockMove(BlockModel block, Vector2 newLogicalPosition) 
    {
        if (IsReadOnly() || block == null || m_WorkspaceModel == null || !block.Movable) return;

        block.XY = newLogicalPosition;

        Debug.Log($"WorkspaceController: BlockModel {block.ID} model XY updated to {newLogicalPosition}. Relying on View updates for ConnectionDB.");
    }

    public void RequestDeleteBlock(BlockModel block) 
    {
        if (IsReadOnly() || block == null || m_WorkspaceModel == null || !block.Deletable) return;

        block.Dispose(false); 

        Debug.Log($"WorkspaceController: Requested deletion of BlockModel {block.ID}.");
    }
    public bool RequestConnection(ConnectionModel connection1, ConnectionModel connection2) 
    {
        if (IsReadOnly() || connection1 == null || connection2 == null || m_WorkspaceModel == null) return false;

        try
        {
            Debug.Log($"WorkspaceController: Requesting connect {connection1} <-> {connection2}");
            connection1.Connect(connection2); 
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"WorkspaceController: Connection failed - {e.Message}");
            return false;
        }
    }

  
    public bool RequestFieldSetValue(FieldModel fieldModel, string newValue) 
    {
        if (IsReadOnly() || fieldModel == null || m_WorkspaceModel == null) return false;
        if (fieldModel.SourceBlock != null && !fieldModel.SourceBlock.Editable)
        {
            Debug.LogWarning("FieldSetValue rejected: BlockModel is not editable.");
            return false;
        }

        fieldModel.SetValue(newValue); 

       
        Debug.Log($"WorkspaceController: Field '{fieldModel.Name}' value set request processed by UBlockly model.");

             return true;
    }

     public bool RequestFieldVariableChange(FieldVariableModel fieldModel, string newVariableName) 
    {
        if (IsReadOnly() || fieldModel == null || m_WorkspaceModel == null) return false;
        if (fieldModel.SourceBlock != null && !fieldModel.SourceBlock.Editable) return false;
      fieldModel.SetValue(newVariableName); 

        Debug.Log($"WorkspaceController: FieldVariable '{fieldModel.Name}' variable name set request processed for '{newVariableName}'.");
        return true;
    }

      public BlockModel RequestCloneBlockBegin(BlockModel templateModelSource, Vector2 initialPosition) 
    {
        if (IsReadOnly() || templateModelSource == null || m_WorkspaceModel == null) return null;

        Debug.Log($"WorkspaceController: Requesting Clone of {templateModelSource.Type}");

        BlockModel clonedModel = templateModelSource.Clone();
        clonedModel.XY = initialPosition; 

        if (clonedModel != null)
        {
            m_WorkspaceModel.RemoveTopBlock(clonedModel);
       BlockDragController.Instance?.RegisterPendingClone(clonedModel); 
            Debug.Log($"WorkspaceController: Created Pending Clone {clonedModel.ID}");
        }
        return clonedModel;
    }


    public void RegisterClonedBlock(BlockModel block) 
    {
        if (block == null || m_WorkspaceModel == null || IsReadOnly()) return;

    
        if (block.ParentBlock == null && !m_WorkspaceModel.TopBlocks.Contains(block))
        {
            Debug.Log($"Registering previously pending clone {block.ID} that was dropped loose.");
            block.SetParent(null); 
        }
    }


    public void RequestLoadWorkspace()
    {
    
        Debug.LogWarning("RequestLoadWorkspace called directly. UI for loading needed.");
        string savedXml = PlayerPrefs.GetString("LastWorkspace_UBlockly", "");
        if (!string.IsNullOrEmpty(savedXml))
        {
            RequestLoadWorkspaceFromData(savedXml);
        }
    }

    public void RequestLoadWorkspaceFromData(string xmlData)
    {
        if (IsReadOnly()) { Debug.LogWarning("Workspace is read-only, load cancelled."); return; }
        if (string.IsNullOrEmpty(xmlData) || m_WorkspaceModel == null) return;
        if (m_WorkspaceModel == null) { Debug.LogError("Load cancelled: Workspace Model is not initialized."); return; }
        if (m_WorkspaceView == null) { Debug.LogError("Load cancelled: Workspace View is not initialized."); return; }


        Debug.Log($"WorkspaceController: Loading from provided XML data (length: {xmlData.Length})...");
        try
        {
        
            m_WorkspaceModel.Clear();
            Debug.Log("Workspace model cleared.");

            m_WorkspaceView.CleanViews();
            Debug.Log("Workspace views cleaned.");


            var xmlDoc = Xml.TextToDom(xmlData);
            List<string> newBlockIds = Xml.DomToWorkspace(xmlDoc, m_WorkspaceModel);
            Debug.Log($"Loaded {newBlockIds?.Count ?? 0} top-level blocks into model {m_WorkspaceModel.Id}.");

            m_WorkspaceView.BuildViews();
            Debug.Log("Workspace views rebuilt from loaded model.");


            m_WorkspaceModel.UpdateProcedureDB();
            m_WorkspaceModel.UpdateVariableStore(true); 

            Debug.Log("<color=green>Workspace loaded successfully from data.</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"WorkspaceController: Error during LoadWorkspaceFromData: {ex.Message}\n{ex.StackTrace}");
                m_WorkspaceModel?.Clear();
            m_WorkspaceView?.CleanViews();
        }
    }

    public void RequestSaveWorkspace()
    {
        if (m_WorkspaceModel == null) { Debug.LogError("Cannot save, WorkspaceModel is null."); return; }
        // if (IsReadOnly()) { Debug.LogWarning("Workspace is read-only, save cancelled."); return; }

        Debug.Log("WorkspaceController: Requesting Save Workspace...");
        try
        {
            var workspaceXml = Xml.WorkspaceToDom(m_WorkspaceModel);
            string xmlData = Xml.DomToText(workspaceXml);

            PlayerPrefs.SetString("LastWorkspace_UBlockly", xmlData);
            PlayerPrefs.Save();
            Debug.Log($"Workspace saved successfully to PlayerPrefs.\n{xmlData}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"WorkspaceController: Error during SaveWorkspace: {ex.Message}\n{ex.StackTrace}");
        }
    }


    #endregion
}//fin WorkSpaceController