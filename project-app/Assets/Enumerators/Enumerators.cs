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
    None,
    Connected,
    Disconnected,
    BumpedAway, // Bloque que ha sido desconectado y ha sido alejado de la conexión
    Highlight, // Bloque que ha sido conectado y se ha resaltado
    UnHighlight, //Bloque que ha sido desconectado 
    AcceptConnection,
    CancelConnection,
    ConnectionFailed,
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

public enum NumberType
{
    NaN,
    Int,
    Float,
    Double,
}

public enum EConnection
{
    InputValue,    // Entrada para valores (ej. un número en "mover X pasos")
    OutputValue,   // Salida de valores (ej. un bloque que genera un número)
    NextStatement, // Conexión para el siguiente bloque en una secuencia
    PrevStatement, // Conexión para el bloque anterior en una secuencia
    DummyInput,     // Input sin conexión (para agrupar fields)
    None            
}

public enum ConnectionZone { 
    None, 
    Top, 
    Bottom 
}

public enum ShadowZone { Top, Bottom }


/// <summary>
/// Enumera los diferentes tipos de cambios que pueden ocurrir en un BlockModel
/// y que necesitan ser notificados a la Vista (BlockView).
/// Se usa como parámetro en el evento OnUpdate de BlockModel.
/// </summary>
public enum BlockUpdateType
{
    // Cambios de Estado Simple (Afectan apariencia visual o interactividad)
    State_Disabled,
    State_Movable,
    State_Deletable,
    State_Shadow,
    State_Editable,
    State_Collapsed,
    State_InputsInline, // Si la apariencia cambia al hacer inline

    // Cambios Estructurales (Requieren reconstrucción/re-layout significativo de la vista)
    Structure_Inputs,    // Inputs añadidos/quitados/reordenados (ej. por mutators)
    Structure_Connections, // Conexiones principales Output/Prev/Next cambiadas

    // Cambios de Valor Interno (Afectan display en FieldViews y potencialmente layout)
    Value_Field,         // El valor de un FieldModel cambió
    Value_Variable,      // La variable referenciada por un FieldVariableModel cambió

    // Cambio de Posición Lógica (Actualiza posición visual)
    Position_XY
}

public enum WorkspaceChangeType
{
    BlockAdded, BlockRemoved, BlockMoved,
    ConnectionCreated, // Cuando dos ConnectionModel se enlazan
    ConnectionBroken, // Cuando dos ConnectionModel se desenlazan
    VariableAdded, VariableRemoved, VariableRenamed,
    Clear, LoadFinish
}

// --- Eventos de Ejecución ---
public enum ExecutionStatus
{
    Idle,      // No ejecutando
    Running,   // Ejecución en curso
    Paused,    // Pausado (no implementado aquí)
    Finished,  // Ejecución completada
    Error      // Error durante la ejecución
}

public enum BlockViewSettingsColor { 
    TopPanelColor, 
    CategoryPanelColor, 
    BlockListPanelColor, 
    CodingAreaPanelColor ,
    CategoryMotionColor,
    CategoryLooksColor,
    CategorySoundColor,
    CategoryEventsColor,
    CategoryControlColor,
    CategorySensingColor,
    CategoryOperatorsColor,
    CategoryVariablesColor,
    CategoryMyBlocksColor

}

public enum BlockResLoadType
{
    /// <summary>
    /// Serialized in scriptable object
    /// </summary>
    Serialized = 1,
    /// <summary>
    /// Load from Resources 
    /// </summary>
    Resources = 2,
    /// <summary>
    /// Load from Assetbundle
    /// </summary>
    Assetbundle = 3,
}

public enum ConnectionUpdateEvent
{
    Connected,
    Disconnected

}


public enum EAlign
{
    Left = -1,
    Center = 0,
    Right = 1,
}
