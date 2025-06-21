/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 19/06/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: En este archivo se manejan las instrucciones que el robot puede ejecutar.
 */


using System.Collections.Generic;
using UnityEngine;

public enum CommandType
{
    MoveForward,
    TurnLeft,
    TurnRight,
    Repeat
}

public class Instruction
{
    public CommandType Type;
    public float Value; // -> "pasos" moverse.

    public List<Instruction> NestedInstructions { get; private set; } //Lista de instrucciones anidadas para el comando Repeat

    public Instruction(CommandType type, float value = 0)
    {
        this.Type = type;
        this.Value = value;
        this.NestedInstructions = new List<Instruction>();

    }
}