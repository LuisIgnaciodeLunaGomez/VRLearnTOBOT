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

using TMPro;
using UnityEngine;

public abstract class FieldView : BaseView
{
    public override ViewType Type => ViewType.Field;

    protected FieldModel m_FieldModel; 
    public FieldModel FieldModel => m_FieldModel;
    protected WorkSpaceView WorkspaceView => WorkSpaceView.Active;
    protected BlockView SourceBlockView => GetComponentInParent<BlockView>(); 
    private TextMeshProUGUI m_TextComponent;

    public abstract Vector2 CalculateFieldSize();

    protected BlockModel SourceBlock 
    {
        get
        {
            BlockView blockView = SourceBlockView;
            if (blockView != null)
            {
                return blockView.Block;
            }
            Debug.LogWarning($"FieldView ({gameObject.name}) could not find parent BlockView.");
            return null;
        }
    }


    protected abstract override Vector2 CalculateSize();

    public virtual void BindModel(FieldModel fieldModel)
    {
        if (m_FieldModel == fieldModel) return;
        UnbindModel();

        m_FieldModel = fieldModel;
        if (m_FieldModel == null)

        {

            // Manejo visualmente el estado sin modelo: Oculto elementos visuales del campo/conexión, etc.
            Debug.Log($"FieldView ('{gameObject.name}'): Bound with NULL model. Hiding visuals.", this.gameObject);
            gameObject.SetActive(false); 
            return; // Salir, no hay modelo para actualizar UI.
        }

        gameObject.SetActive(true);

        try
        {
            OnValueChanged(m_FieldModel.GetValue());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during initial OnValueChanged for {gameObject.name} ({m_FieldModel.GetType()}): {ex.Message}\n{ex.StackTrace}");
        }

    }

    public virtual void UnbindModel()
    {
        if (m_FieldModel != null)
        {
                 m_FieldModel = null;
        }
    }

   
    protected virtual void HandleModelValueChanged(string newValue) 
    {
        if (this == null || m_FieldModel == null || !gameObject.activeInHierarchy) return;

        OnValueChanged(newValue); 
        SourceBlockView?.QueueForceLayoutUpdate(); 
    }

    /**
     * Método abstracto que DEBEN implementar los subtipos.
     * Actualiza los componentes visuales (TextMeshProUGUI, Image, InputField, etc.)
     * para reflejar el nuevo valor PROVENIENTE DEL MODELO.
     */
    protected abstract void OnValueChanged(string newValue);

    /**
     * Método que DEBEN implementar los subtipos editables.
     * Registra los listeners de los componentes UI (e.g., TMP_InputField.onEndEdit, Button.onClick).
     * Estos listeners NO modificarán el modelo directamente, sino que llamarán
     * a un método en el InputController (o WorkspaceController).
     */
    protected abstract void RegisterInputListeners();

    /**
    * Método que los listeners de UI llamarán.
    * NO llama directamente a m_FieldModel.SetValue().
    * Llama al InputController para solicitar el cambio.
    * @param userInput El valor ingresado/seleccionado por el usuario en la UI.
    */
    protected void RequestModelUpdate(string userInput)
    {
        if (m_FieldModel == null) return; 

        Debug.Log($"FieldView {m_FieldModel.Name} requesting update with value: {userInput}"); 

        InputController inputController = InputController.Instance;
        if (inputController != null)
        {
            inputController.HandleFieldInputValueChange(m_FieldModel, userInput);
        }
        else
        {
            Debug.LogError("InputController instance not found!");
         
        }
    }


    protected override void InitializeView()
    {
        base.InitializeView();
        RegisterInputListeners(); 
    }

    public new void OnDestroy()
    {
        UnbindModel(); 
        
    }

    /** Forzar actualización de UI desde el valor actual del modelo (para revertir) */
    public void ForceUpdateDisplayFromModel()
    {
        if (m_FieldModel != null)
        {
            OnValueChanged(m_FieldModel.GetValue());
        }
    }

}//fin clase FieldView


