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
        base.OnXYUpdated();
        foreach (var child in ChildViews)
            child.OnXYUpdated();
    }

    protected override void InitializeView()
    {
        base.InitializeView();
       
    }

    public virtual void BindModel(InputModel inputModel, BlockView sourceBlockView)
    {
        if (m_InputModel == inputModel) return;
        UnBindModel(); 

        m_InputModel = inputModel;
        if (m_InputModel == null) return;

       
        int fieldIndex = 0;
        ConnectionInputView connectionView = null;

        if (ChildViews.LastOrDefault() is ConnectionInputView lastChildConnection)
        {
            connectionView = lastChildConnection;
            connectionView.BindModel(m_InputModel.Connection, sourceBlockView); 
        }

        foreach (BaseView child in ChildViews)
        {
            if (child is FieldView fieldView)
            {
                if (fieldIndex < m_InputModel.FieldRow.Count)
                {
                    fieldView.BindModel(m_InputModel.FieldRow[fieldIndex]);
                    fieldIndex++;
                }
                else
                {
                    Debug.LogError($"InputView ({gameObject.name}): Mismatch between FieldViews and FieldModels. More Views than Models.");
                    child.gameObject.SetActive(false);
                }
            }
            else if (child != connectionView) 
            {
                Debug.LogWarning($"InputView ({gameObject.name}): Contains unexpected child view type: {child.GetType()}");
            }
        }

        if (fieldIndex < m_InputModel.FieldRow.Count)
        {
            Debug.LogError($"InputView ({gameObject.name}): Mismatch between FieldViews and FieldModels. More Models than Views.");
        }

      
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