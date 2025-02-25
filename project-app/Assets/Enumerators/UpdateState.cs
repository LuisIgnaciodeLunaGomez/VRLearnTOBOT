/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 24/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

public enum UpdateState
{
    Connected,
    Disconnected,
    BumpedAway, // Bloque que ha sido desconectado y ha sido alejado de la conexión
    Highlight, // Bloque que ha sido conectado y se ha resaltado
    UnHighlight //Bloque que ha sido desconectado 
}