/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 20/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Gestor de colisiones entre bloques
 */


using System;
using UnityEngine;
using UnityEngine.UI;

public class ShadowCollision : MonoBehaviour
{
    private Image shadowImage;
    public Action<BlockBehaviour> OnBlockEntered; // Evento cuando un bloque entra
    public Action<BlockBehaviour> OnBlockExited;  // Evento cuando un bloque sale
    public void SetShadowImage(Image image)
    {
        this.shadowImage = image;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Block"))
        {
            BlockBehaviour block = other.GetComponent<BlockBehaviour>();
            if (block != null)
            {
                shadowImage.color = new Color(0, 0, 0, 0.3f); // Gris al entrar
                OnBlockEntered?.Invoke(block); // Notificar entrada
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Block"))
        {
            BlockBehaviour block = other.GetComponent<BlockBehaviour>();
            if (block != null)
            {
                shadowImage.color = new Color(0, 0, 0, 0); // Transparente al salir
                OnBlockExited?.Invoke(block); // Notificar salida
            }
        }
    }
}