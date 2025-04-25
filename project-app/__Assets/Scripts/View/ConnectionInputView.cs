/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 08/03/2025
 * 
 * Versión: 2.0.0
 * 
 * Descripción:
 * 
 */
using UnityEngine;
using UnityEngine.UI;

public class ConnectionInputView : ConnectionView 
{
    public override ViewType Type => ViewType.ConnectionInput;
    protected BlockView m_SourceBlockView;

 
    public bool IsSlot { get; set; } = true; 


    protected override void InitializeView()
    {
        base.InitializeView();
        //  visuales específicas del Slot
        Image img = GetComponent<Image>();
        if (img != null)
        {
            img.color = BlockViewSettings.Instance.InputSlotColor;
            img.raycastTarget = IsSlot;
        }
    }

    protected override Vector2 CalculateSize()
    {
             if (IsSlot)
        {
            if (ConnectionType == EConnection.InputValue)
                return BlockViewSettings.Instance.InputValueSlotSize;
            else if (ConnectionType == EConnection.NextStatement) 
                return BlockViewSettings.Instance.InputStatementSlotSize;
            else
                return base.CalculateSize(); 
        }
        else
        {
             return CalculateSizeForSlot(); 
        }
    }

    private Vector2 CalculateSizeForSlot()
    {
        if (ConnectionType == EConnection.InputValue) return BlockViewSettings.Instance.InputValueSlotSize;
        if (ConnectionType == EConnection.NextStatement) return BlockViewSettings.Instance.InputStatementSlotSize;
        return base.CalculateSize();
    }


   /* protected override void OnValueChanged(string newValue)
    {
        // Los ConnectionInput no tienen un "valor" que mostrar como los Fields
        // Pero reaccionamos a Connected/Disconnected del modelo
        base.OnValueChanged(newValue); // Llama base por si hace algo
    }*/

   /* protected override void RegisterInputListeners()
    {
        // Los slots de conexión usualmente no tienen listeners directos.
        // El drag & drop se maneja a nivel de BlockView/BlockDragController,
        // y la búsqueda de conexiones usa ConnectionDB.
    }*/


   
    protected override void HandleModelUpdate(ConnectionModel model, ConnectionUpdateEvent eventType, ConnectionModel partner) 
    {
        base.HandleModelUpdate(model, eventType, partner);

        if (model == ConnectionModel)
        {
            bool connected = (eventType == ConnectionUpdateEvent.Connected);
            
            if (IsSlot == connected)
            {
                IsSlot = !connected;
                Image img = GetComponent<Image>();
                if (img != null) img.enabled = IsSlot; 

              
               m_SourceBlockView?.QueueForceLayoutUpdate();
            }
        }
    }

    public override void BindModel(ConnectionModel connectionModel, BlockView sourceBlockView)
    {
        base.BindModel(connectionModel, sourceBlockView); 
        HandleModelUpdate(ConnectionModel, ConnectionModel.IsConnected ? ConnectionUpdateEvent.Connected : ConnectionUpdateEvent.Disconnected, ConnectionModel.TargetConnection);
    }

    public override void UnBindModel()
    {
        
        // if (m_ConnectionModel != null) m_ConnectionModel.OnUpdateWithPartner -= HandleModelUpdate;
        base.UnBindModel(); 
    }


}//Fin ConnectionInputView