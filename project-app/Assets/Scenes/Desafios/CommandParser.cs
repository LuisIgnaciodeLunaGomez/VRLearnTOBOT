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
 * Descripción: Interprete de comandos que analiza el código fuente y genera una lista de instrucciones.
 */

using System;
using System.Collections.Generic;
public class CommandParser
{
    public static bool Parse(string sourceCode, out List<Instruction> program, out string errorMessage)
    {
        program = new List<Instruction>();
        errorMessage = "";

        string[] lines = sourceCode.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

      
        var instructionStack = new Stack<List<Instruction>>();  // Pila para manejar bloques anidados (como bucles dentro de bucles)
        instructionStack.Push(program);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim().ToLower(); // Se convierte a minúsculas y se eliminan espacios en blanco al principio y al final

            if (string.IsNullOrEmpty(line) || line.StartsWith("//")) continue;

            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            var currentInstructionList = instructionStack.Peek(); // Lista donde añadimos comandos ahora

            switch (parts[0])
            {
                case "mover":
                    if (parts.Length != 3 || !float.TryParse(parts[1], out float steps) || parts[2] != "pasos")
                    {
                        errorMessage = $"Error Sintaxis Línea {i + 1}: Usa 'mover <numero> pasos'.";
                        return false;
                    }
                    program.Add(new Instruction(CommandType.MoveForward, steps));
                    break;

                case "girar":
                    if (parts.Length != 2)
                    {
                        errorMessage = $"Error Sintaxis Línea {i + 1}: Usa 'girar <izquierda/derecha>'.";
                        return false;
                    }
                    if (parts[1] == "izquierda")
                    {
                        program.Add(new Instruction(CommandType.TurnLeft));
                    }
                    else if (parts[1] == "derecha")
                    {
                        program.Add(new Instruction(CommandType.TurnRight));
                    }
                    else
                    {
                        errorMessage = $"Error Sintaxis Línea {i + 1}: Dirección '{parts[1]}' desconocida.";
                        return false;
                    }
                    break;

                // --- NUEVA LÓGICA DE BUCLES ---
                case "repetir":
                    int repetitions;
                    if (parts.Length == 2 && parts[1] == "por_siempre")
                    {
                        repetitions = -1; // -1  para bucle infinito
                    }
                    else if (parts.Length == 3 && int.TryParse(parts[1], out repetitions) && parts[2] == "veces")
                    {
                        // Bucle finito, todo correcto
                    }
                    else
                    {
                        errorMessage = $"Error Línea {i + 1}: Usa 'repetir <N> veces' o 'repetir por_siempre'."; return false;
                    }

                    // Nueva instrucción de tipo Repeat
                    var repeatInstruction = new Instruction(CommandType.Repeat, repetitions);
                    currentInstructionList.Add(repeatInstruction);

                    // Siguientes instrucciones irán DENTRO del bucle.
                    instructionStack.Push(repeatInstruction.NestedInstructions);
                    break;

                case "fin_repetir":
                    if (instructionStack.Count <= 1)
                    { 
                        errorMessage = $"Error Línea {i + 1}: 'fin_repetir' sin un 'repetir' correspondiente."; return false;
                    }
                    // Hemos terminado el bloque, salimos un nivel en la pila.
                    instructionStack.Pop();
                    break;
                case "mover_durante":
                    if (parts.Length != 3 || !float.TryParse(parts[1], out float duration) || parts[2] != "segundos")
                    {
                        errorMessage = $"Error Sintaxis Línea {i + 1}: Usa 'mover_durante <numero> segundos'.";
                        return false;
                    }
                    // Añadimos a la lista la nueva instrucción
                    currentInstructionList.Add(new Instruction(CommandType.MoveForDuration, duration));
                    break;

                default:
                    errorMessage = $"Error Comando Línea {i + 1}: '{parts[0]}' no reconocido.";
                    return false;
            }
        }

        if (instructionStack.Count > 1)
        {
            errorMessage = "Error: El código termina, pero un bloque 'repetir' no fue cerrado con 'fin_repetir'."; return false;
        }
        return true;
    }
}
