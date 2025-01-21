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


using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System;
using UnityEngine;


public class Generator
{
    public record CodeStruct(string Code = "", int Order = -1)
    {
        public bool IsEmpty => string.IsNullOrEmpty(Code); // Verifica si el código está vacío.
        public static CodeStruct Empty => new(); // Estructura vacía con valores predeterminados.
    }

    public abstract class scratchCodeGenerator
    {
        public abstract CodeName Name { get; } // Nombre del generador de código.
        private readonly Dictionary<string, MethodInfo> codeMap = new(); // Métodos asociados con tipos de bloques.
        protected readonly VariableNames VariableNames;
        protected readonly Dictionary<string, KeyValuePair<string, string>> FuncMap = new(); // Almacena funciones generadas.
        public string Indent { get; init; } = "    "; // Indentación predeterminada (propiedad inmutable).
        public static string FunctionNamePlaceholder => "{leCUI8hutHZI4480Dc}"; // Marcador de nombre de función.

    }

}
