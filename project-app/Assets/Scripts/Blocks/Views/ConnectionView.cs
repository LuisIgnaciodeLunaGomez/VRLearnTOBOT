/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 10/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:  Manejo de las conexiones entre bloques
 * 
 */

using UnityEditor.MemoryProfiler;
using UnityEngine;

public class ConnectionView : BaseView
{
    public BlockConnection TargetConnection { get; private set; }
    private UpdateState State { get; set; }


    public override ViewType Type
    {
        get { return ViewType.Connection; }
    }

    protected override Vector2 CalculateSize()
    {
        return new Vector2(20, 20); // Tamaño estándar para los puntos de conexión
    }

    public void BindConnection(BlockConnection connection)
    {
        TargetConnection = connection;
    }

    public void Highlight(bool state)
    {
        this.GetComponent<Renderer>().material.color = state ? Color.green : Color.white;
    }
}