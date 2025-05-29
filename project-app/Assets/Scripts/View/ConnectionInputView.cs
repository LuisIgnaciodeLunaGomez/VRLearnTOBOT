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
    protected new BlockView m_SourceBlockView;

 
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
     
       // Debug.Log($"---> ConnectionInputView({gameObject.name}).BindModel START. Received connectionModel is {(connectionModel == null ? "NULL" : "VALID")}");
       // if (connectionModel != null) Debug.Log($"    Received Model ID: {ConnectionModel.GetConnectionModelID(connectionModel)}");

        base.BindModel(connectionModel, sourceBlockView);

        //Debug.Log($"---> ConnectionInputView({gameObject.name}) AFTER base.BindModel.");
      //  Debug.Log($"     m_ConnectionModel is now {(ConnectionModel == null ? "NULL" : "VALID")}");//<---DEJARLO
        if (ConnectionModel != null)
        {
           // Debug.Log($"     m_ConnectionModel.IsConnected = {ConnectionModel.IsConnected}");
           // Debug.Log($"     m_ConnectionModel.TargetConnection is {(ConnectionModel.TargetConnection == null ? "NULL" : "VALID")}");
            // Verifico también la propiedad pública que usa en HandleModelUpdate
           // Debug.Log($"     Property 'ConnectionModel' returns: {(this.ConnectionModel == null ? "NULL" : "VALID")}");
        }

        //llamada a HandleModelUpdate
       // Debug.Log($"---> Calling HandleModelUpdate...");
        try
        {
            // Pasa la variable local connectionModel que se ha recibido no se pasa la propiedad this.ConnectionModel
            HandleModelUpdate(
                connectionModel, // Usa el argumento que SÍ sabemos que no es null al entrar
                connectionModel != null ? (connectionModel.IsConnected ? ConnectionUpdateEvent.Connected : ConnectionUpdateEvent.Disconnected) : ConnectionUpdateEvent.Disconnected, // Verifica null antes de acceder
                connectionModel?.TargetConnection // Usa el operador ?. por seguridad
                );
           // Debug.Log($"---> HandleModelUpdate called SUCCESSFULLY.");
        }
        catch (System.NullReferenceException nre)
        {
            Debug.LogError($"!!! NRE occurred DURING HandleModelUpdate call !!! StackTrace:\n{nre.StackTrace}", this.gameObject);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"!!! Exception occurred DURING HandleModelUpdate call: {ex.Message} !!! StackTrace:\n{ex.StackTrace}", this.gameObject);
        }
    }

    public override void UnBindModel()
    {
        
        // if (m_ConnectionModel != null) m_ConnectionModel.OnUpdateWithPartner -= HandleModelUpdate;
        base.UnBindModel(); 
    }

}//Fin ConnectionInputView