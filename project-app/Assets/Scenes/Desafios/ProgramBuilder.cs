/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 20/06/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: constructor de programas que permite al usuario construir un programa línea por línea.
 */

using System.Collections.Generic;
public class ProgramBuilder
{
    // Almacenamos el programa como una lista de strings (lo que el usuario escribe)
    private List<string> codeLines = new List<string>();

    // Método para añadir una nueva línea de código
    public void AddLine(string line)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            codeLines.Add(line);
        }
    }

    // Método para borrar la última línea
    public void RemoveLastLine()
    {
        if (codeLines.Count > 0)
        {
            codeLines.RemoveAt(codeLines.Count - 1);
        }
    }

    // Método para borrar todo el programa
    public void Clear()
    {
        codeLines.Clear();
    }

    public List<string> GetCodeLines()
    {
        return codeLines;
    }

    // El método que ensambla el código y lo parsea
    public bool TryParseProgram(out List<Instruction> program, out string errorMessage)
    {
        // Une todas las líneas en un solo bloque de texto
        string fullSourceCode = string.Join("\n", codeLines);

        //Usa el CommandParser existente para procesarlo todo de una vez
        return CommandParser.Parse(fullSourceCode, out program, out errorMessage);
    }
}