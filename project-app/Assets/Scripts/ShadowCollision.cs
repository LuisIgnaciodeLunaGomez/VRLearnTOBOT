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
            BlockBehaviour parentBlock = GetComponentInParent<BlockBehaviour>();
            if (block != null && block != parentBlock)
            {

                // Si una sombra ya está activa, no activar la otra
                if ((name.Contains("Top") && block.collidingWithBottomShadowOf != null) ||
                    (name.Contains("Bottom") && block.collidingWithTopShadowOF != null))
                {
                    return;
                }

                shadowImage.enabled = true; // Mostrar la sombra

              
                // Debug.Log($"ShadowCollision: Bloque {block.gameObject.name} entró en {gameObject.name} de {parentBlock.name}");
                this.shadowImage.color = new Color(0, 0, 0, 0.3f); // Gris al entrar
                this.OnBlockEntered?.Invoke(block); // Notificar entrada
                                               // Notificar al bloque en movimiento
                if (name.Contains("Top"))
                {
                    //block.collidingWithTopShadowOF = parentBlock;
                    //parentBlock.ShowTopShadow(ConnectionZone.Top);
                    parentBlock.SetConnectionZone(ConnectionZone.Top);
                }
                else if (name.Contains("Bottom"))
                {
                    //block.collidingWithBottomShadowOf = parentBlock;
                   // parentBlock.ShowBottomShadow(ConnectionZone.Bottom);
                    parentBlock.SetConnectionZone(ConnectionZone.Bottom);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Block"))
        {
            BlockBehaviour block = other.GetComponent<BlockBehaviour>();
            BlockBehaviour parentBlock = GetComponentInParent<BlockBehaviour>();
            if (block != null && block != parentBlock)
            {
                this.shadowImage.enabled = false; // Ocultar la sombra
                this.shadowImage.color = new Color(0, 0, 0, 0); // Transparente al salir
                this.OnBlockExited?.Invoke(block); // Notificar salida
              //  Debug.Log($"ShadowCollision: Bloque {block.gameObject.name} salió de {gameObject.name} de {parentBlock.gameObject.name}");
             /*   if (name.Contains("Top") && block.collidingWithTopShadowOF == parentBlock)
                {
                    block.collidingWithTopShadowOF = null;
                }
                else if (name.Contains("Bottom") && block.collidingWithBottomShadowOf == parentBlock)
                {
                    block.collidingWithBottomShadowOf = null;
                }*/
                if (block != null && block != parentBlock)
                {
                    //parentBlock.HideAllShadows(ConnectionZone.None);
                    parentBlock.ClearConnectionZone();
                }
            }
        }
    }
}