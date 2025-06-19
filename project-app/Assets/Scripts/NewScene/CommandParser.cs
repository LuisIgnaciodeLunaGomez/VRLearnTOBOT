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
 * Descripción: 
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

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim().ToLower(); // Se convierte a minúsculas y se eliminan espacios en blanco al principio y al final
            if (string.IsNullOrEmpty(line) || line.StartsWith("//")) continue;

            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

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

                default:
                    errorMessage = $"Error Comando Línea {i + 1}: '{parts[0]}' no reconocido.";
                    return false;
            }
        }
        return true;
    }
}
