/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 27/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using UnityEngine;
using System.Linq;
public class InputView : BaseView
{
    public override ViewType Type => ViewType.Input;

    private InputModel m_InputModel;
    public InputModel InputModel => m_InputModel;
    [SerializeField] private float m_FieldSpacing = 2f; 

    public override Vector2 ChildStartXY => Vector2.zero; 

    [Tooltip("Indicates if the content of this input should be aligned to the right.")]
    [SerializeField] private bool m_AlignRight = false; 
    public bool AlignRight
    {
        get => m_AlignRight;
         set => m_AlignRight = value;
    }

    protected override Vector2 CalculateSize()
    {
        if (!HasChildren) return BlockViewSettings.Instance.MinUnitSize; 

        float totalWidth = 0;
        totalWidth += ChildViews.Sum(v => v.Size.x);
        totalWidth += Mathf.Max(0, ChildViews.Count - 1) * m_FieldSpacing;

        float maxHeight = ChildViews.Max(v => v.Size.y);

        return new Vector2(totalWidth, maxHeight);
    }

    protected internal override void OnXYUpdated()
    {
        //Debug.Log($"InputView::OnXYUpdated calling base.OnXYUpdated().", this.gameObject);
        base.OnXYUpdated();
        //foreach (var child in ChildViews)
         // child.OnXYUpdated();
         
    }

    protected override void InitializeView()
    {
        base.InitializeView();
       
    }

    public virtual void BindModel(InputModel inputModel, BlockView sourceBlockView)
    {
       
        if (m_InputModel != null)
        {
            //  Debug.Log($"InputView ({gameObject.name}): BindModel - Unbinding old model {m_InputModel.Name}.", this.gameObject);
            UnBindModel();
        }

        m_InputModel = inputModel;

        //Debug.Log($"InputView ({gameObject.name}): BindModel START. New Input Model ID: {m_InputModel?.Name ?? "NULL"}.", this.gameObject);

        if (m_InputModel == null) return;

        int fieldIndex = 0;
        ConnectionInputView foundConnectionView = null;

        foreach (BaseView childVisual in ChildViews.Where(c => c != null))
        {
            if (childVisual is FieldView fieldView)
            {
                // Mapeo visual FieldView[i] a FieldModel lógico[i] en el FieldRow del InputModel
                FieldModel fieldModelToBind = null;
                if (m_InputModel.FieldRow != null && fieldIndex < m_InputModel.FieldRow.Count)
                {
                    fieldModelToBind = m_InputModel.FieldRow[fieldIndex];
                    fieldIndex++; // Avanza en el FieldRow lógico solo si se encontró un modelo potencial
                }
                else
                {
                    Debug.LogWarning($"InputView ('{gameObject.name}'): Visual FieldView '{childVisual.gameObject.name}' at logical field index {fieldIndex} has no corresponding FieldModel in InputModel '{m_InputModel.Name}'. InputModel has {m_InputModel.FieldRow?.Count ?? 0} fields. Bindiando a NULL.", childVisual.gameObject);
                }
                fieldView.BindModel(fieldModelToBind);

                fieldIndex++;
                // if (fieldModelToBind != null && fieldView.FieldModel == null) Debug.LogError($"!!! FieldView '{childVisual.gameObject.name}' (Bound with: {fieldModelToBind?.GetType().Name}) STILL NULL AFTER BindModel !!!", fieldView.gameObject);

            }
            else if (childVisual is ConnectionInputView connectionInputView)
            {

                foundConnectionView = connectionInputView;

                if (m_InputModel.Connection != null)
                {
                    if (m_InputModel.Type == connectionInputView.ConnectionType)
                    {
                        ConnectionModel inputConnectionModel = m_InputModel.Connection;
                       // Debug.Log($"InputView ('{gameObject.name}'): Found ConnectionInputView '{connectionInputView.gameObject.name}'. Binding to Model: {ConnectionModel.GetConnectionModelID(inputConnectionModel)}.", connectionInputView.gameObject);
                        connectionInputView.BindModel(inputConnectionModel, sourceBlockView); // sourceBlockView viene como argumento a InputView.BindModel
                    }
                    else
                    {
                        Debug.LogError($"InputView ('{gameObject.name}'): CRITICAL MISMATCH! ConnectionInputView '{connectionInputView.gameObject.name}' has visual type {connectionInputView.ConnectionType} but InputModel '{m_InputModel.Name}' expects type {m_InputModel.Type}. Binding View to NULL.", connectionInputView.gameObject);
                        connectionInputView.BindModel(null, sourceBlockView);
                    }
                }
                else // m_InputModel.Connection == null (El modelo NO espera conexión)
                {
                    Debug.LogError($"InputView ('{gameObject.name}'): Visual ConnectionInputView '{connectionInputView.gameObject.name}' found, but InputModel '{m_InputModel.Name}' (Type: {m_InputModel.Type}) does NOT expect a connection (ConnectionModel is NULL). Binding View to NULL.", connectionInputView.gameObject);
                    connectionInputView.BindModel(null, sourceBlockView);
                }

            }
            else
            {
                Debug.LogWarning($"InputView ('{gameObject.name}'): Unexpected BaseView type ('{childVisual.gameObject.name}' Type:{childVisual.GetType().Name}) found as direct child. Is prefab structure incorrect?", childVisual.gameObject);
            }

      
    }
       
        if (m_InputModel.Connection != null && foundConnectionView == null)
        {
            Debug.LogError($"InputView ('{gameObject.name}'): Mismatch! InputModel '{m_InputModel.Name}' expects a connection, but no visual ConnectionInputView was found/processed among children.", this.gameObject);
        }

      //  Debug.Log($"InputView ({gameObject.name}): BindModel END. Final model state assigned.", this.gameObject);

    }

    public virtual void UnBindModel()
    {
        if (m_InputModel == null) return;

        foreach (BaseView child in ChildViews)
        {
            if (child is FieldView fieldView) fieldView.UnbindModel();
            else if (child is ConnectionInputView connectionView) connectionView.UnBindModel();
        }
        m_InputModel = null;
    }


    public bool HasConnection => GetConnectionView() != null;

    public ConnectionInputView GetConnectionView()
    {
        return ChildViews.LastOrDefault() as ConnectionInputView;
    }
}//fin clase InputViews