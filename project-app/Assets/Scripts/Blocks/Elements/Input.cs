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
 * Descripción: 
 */


using System;
using System.Collections.Generic;
//using UnityEditor.MemoryProfiler;


public class Input
{

    public string Name { get; private set; }
    public readonly BlockConnection Connection;
    public object FieldValue { get; private set; }
    private Block m_SourceBlock; //Bloque al que pertenece la conexión

    public Block sourceBlock
    {

        get => m_SourceBlock;
        set
        {
            m_SourceBlock = value;
            if (Connection != null)
            {
                Connection.sourceBlock = value?.behaviour;
            }
        }
    }

    public void Dispose()
    {
        if (Connection != null)
        {
            Connection.Disconnect();
            Connection.Dispose();
        }
    }

    public Input(string name, EConnection type = EConnection.InputValue, object defaultValue = null)
    {
        Name = name;
        Connection = new BlockConnection(null, type);
        FieldValue = defaultValue;
    }

  
}
