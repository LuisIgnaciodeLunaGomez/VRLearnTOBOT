/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 22/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */


using UnityEngine;


public class Block
{
    
    public string ID { get; private set; }
    public string Type { get; private set; }

    public Vector2 XY { get;  set; }

    public WorkSpace workSpace { get;  set; }

    public Block(string type, Vector2 position, WorkSpace workSpace)
    {
        this.ID = Utilidades.GenUid();
        this.Type = type;
        this.XY = position;

        this.workSpace = workSpace;

    }


    public Block Clone()
    {
        return new Block(this.Type, this.XY, this.workSpace);
    }

    public void Dispose()
    {
        workSpace.BlockDB.Remove(ID); //Elimina el bloque del diccionario de bloques del espacio de trabajo
    }   
}

