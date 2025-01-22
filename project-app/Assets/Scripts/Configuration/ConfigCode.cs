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

using UnityEngine;

public static class ConfigCode
{

    private static ConfigGenerator codeGenerator;
    private static ConfigInterpreter codeInterpreter;
    private static ConfigRunner codeRunner;
    private static VariableNames variableNames;
    private static VariableData variableData;

    public static ConfigGenerator Generator => codeGenerator ??= new ConfigGenerator();
    public static ConfigInterpreter Interpreter => codeInterpreter ??= new ConfigInterpreter();
    public static ConfigRunner Runner => codeRunner ??= new ConfigRunner();
    public static VariableNames VariableNames => variableNames ??= new VariableNames();
    public static VariableData VariableData => variableData ??= new VariableData();

}
