/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 26/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Opciones para el espacio de trabajo
 */

public class WorkSpaceOptions
{

    
    public int MaxBlocks = -1;
    public bool ReadOnly = false;
    public bool Synchronous = false;

    public WorkSpaceOptions Options { get; private set; }
}
