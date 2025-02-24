/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 21/02/2025
 * 
 * Versión: 1.0.1
 * 
 * Descripción: Clase que se encarga de la creación de los bloques para cada categoría
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlockFactory
{
    
    public static Block CreateBlock(string Type, Vector2 position, WorkSpace workSpace)
    {
        return new Block(Type, position, workSpace);
    }

  
}

