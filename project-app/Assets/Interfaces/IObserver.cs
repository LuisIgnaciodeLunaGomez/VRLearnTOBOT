/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 26/32/2025
 * 
 * Versión: 1.0.
 * 
 * Descripción: 
 */

public interface IObserver<in TArgs>
{
    void OnUpdated(object subject, TArgs args);
}