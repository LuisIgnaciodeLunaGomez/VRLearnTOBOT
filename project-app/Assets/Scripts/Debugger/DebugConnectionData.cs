/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 26/04/2025
 * 
 * Versión: 2.0.0
 * */

[System.Serializable]
public class DebugConnectionData
{
    public string ConnectionModelId;
    public string Type;
    public bool IsSuperior;
    public float LocationX;
    public float LocationY;
    public bool InDB;
    public bool Hidden;
    public bool IsConnected;
    public string TargetConnectionId;
    public string SourceBlockId;
    public string SourceBlockType;
    public string[] Checks;
}

[System.Serializable]
public class DebugBlockData
{
    public string BlockId;
    public string Type;
    public float XY_X;
    public float XY_Y;
  
}

[System.Serializable]
public class DebugConnectionDBsState
{
    public DebugConnectionData[] InputValuesDB;
    public DebugConnectionData[] OutputValuesDB;
    public DebugConnectionData[] NextStatementsDB;
    public DebugConnectionData[] PrevStatementsDB;

    // lista de todos los bloques 
    public DebugBlockData[] AllBlocks;
    
}