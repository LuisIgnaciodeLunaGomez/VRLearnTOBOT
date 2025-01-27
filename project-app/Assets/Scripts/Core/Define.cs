/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 21/01/2025
 * 
 * Versión: 1.0.0
 */
using System;
using UnityEngine;
using static enumerator;

public class Define
{
    public enum EConnection
    {
        InputValue,
        OutputValue,
        NextStatement,
        PrevStatement
    }

    public static EConnection OppositeConnection(EConnection connectionType)
    {
        return connectionType switch
        {
            EConnection.InputValue => EConnection.OutputValue,
            EConnection.OutputValue => EConnection.InputValue,
            EConnection.NextStatement => EConnection.PrevStatement,
            EConnection.PrevStatement => EConnection.NextStatement,
            _ => throw new Exception("Tipo de conexión no válido.")
        };
    }
}
