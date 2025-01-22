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

public class OperatorPrecedence
{
    /// <summary>
    /// Niveles de precedencia para operadores en C#.
    /// </summary>
    public enum enumOperatorPrecedence
    {
        Atomic = 0,          // Literales
        Expression = 1,      // Postfix: [] (), etc.
        Unary = 2,           // Unary: ++, --, !, ~
        TypeCast = 3,        // Casts: (T)x
        Multiplicative = 4,  // *, /, %
        Additive = 5,        // +, -
        Relational = 7,      // <, >, <=, >=
        Equality = 8,        // ==, !=
        LogicalAnd = 12,     // &&
        LogicalOr = 13,      // ||
        Assignment = 15,     // =
        None = 99            // Sin precedencia
    }

    public enum CodeName //enumeración (enum) que define los lenguajes de programación soportados.
    {
        CSharp
    }

    public enum ControlFlowType //representa los tipos de control de flujo en bloques de código.
    {
        None, //Indica que no hay control de flujo.
        Break, //Representa una interrupción del flujo (como break en un bucle).
        Continue, //Representa una instrucción que pasa a la siguiente iteración de un bucle.
    }
}
