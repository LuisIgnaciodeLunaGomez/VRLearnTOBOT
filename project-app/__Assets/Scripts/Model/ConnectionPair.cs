/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 02/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Clase simple para almacenar un par de conexiones coincidentes durante la búsqueda de conexiones al arrastrar.
 */


public class ConnectionPair
{
   
    public ConnectionModel Mine { get; private set; }
    public ConnectionModel Neighbour { get; private set; }

 
    public float Distance { get; private set; }

    public ConnectionPair(ConnectionModel mine, ConnectionModel neighbour, float distance)
    {
        this.Mine = mine;
        this.Neighbour = neighbour;
        this.Distance = distance;
    }
}//Fin clase ConnectionPair