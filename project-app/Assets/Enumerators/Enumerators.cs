/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 24/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Archivo que contiene los enumeradores utilizados en la aplicación
 */

public enum UpdateState
{
    Connected,
    Disconnected,
    BumpedAway, // Bloque que ha sido desconectado y ha sido alejado de la conexión
    Highlight, // Bloque que ha sido conectado y se ha resaltado
    UnHighlight //Bloque que ha sido desconectado 
}

public enum ViewType
{
    Block,
    LineGroup,    
    Input,
    Field,
    Connection,       
    ConnectionInput,
}

public enum ConnectionInputViewType
{
    Value = 0,    
    ValueSlot,    
    Statement,    
}

public enum UpdateStates
{
    Inputs = 0,
    //Fields = 1,
    Connections = 2,
    IsDisabled = 3,
    IsCollapsed = 4,
    IsEditable = 5,
    IsDeletable = 6,
    IsMovable = 7,
    IsInputInline = 8,
    IsShadow = 9

}

public enum EConnection
{
    InputValue,    // Entrada para valores (ej. un número en "mover X pasos")
    OutputValue,   // Salida de valores (ej. un bloque que genera un número)
    NextStatement, // Conexión para el siguiente bloque en una secuencia
    PrevStatement  // Conexión para el bloque anterior en una secuencia
}