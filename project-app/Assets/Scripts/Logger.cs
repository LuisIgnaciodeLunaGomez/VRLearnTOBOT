/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 11/05/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:  Clase para centralizar la gestión de logs y mensajes de depuración.
 * 
 */

using UnityEngine;

public static class Logger
{
    public static void Log(string message, UnityEngine.Object context = null)
    {
        Debug.Log($"{System.DateTime.Now:HH:mm:ss.fff} - {message}", context);
    }

    public static void LogWarning(string message, UnityEngine.Object context = null)
    {
        Debug.LogWarning($"{System.DateTime.Now:HH:mm:ss.fff} - {message}", context);
    }

    public static void LogError(string message, UnityEngine.Object context = null)
    {
        Debug.LogError($"{System.DateTime.Now:HH:mm:ss.fff} - {message}", context);
    }
}