/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 30/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 * 
 */


using System.Collections.Generic;
using UnityEngine;

public class Utilidades
{
    private static readonly string SOUP = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenUid()
    {
        var id = new List<char>();
        for (int i = 0; i < 20; i++)
        {
            id.Add(SOUP[UnityEngine.Random.Range(0, SOUP.Length)]);
        }
        return new string(id.ToArray());
    }

    /**
     * Descripción: Obtiene un componente del GameObject. Si no existe, lo añade.
     * @param: go: GameObject al que se le añadirá el componente
     * return: T: Componente añadido
     * */
    public static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        if (go == null) return null;
        T component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }
        return component;
    }
}
