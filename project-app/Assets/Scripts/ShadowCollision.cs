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
    private Image m_shadowImage;
    public Action<BlockBehaviour> OnBlockEntered; // Evento cuando un bloque entra
    public Action<BlockBehaviour> OnBlockExited;  // Evento cuando un bloque sale
    private ConnectionZone m_zone;
    private BlockBehaviour m_parentBlock;
    private BlockBehaviour m_block;

    // public void SetShadowImage(Image image)
    // {
    // this.shadowImage = image;
    // shadowImage.enabled = false; // Asegura que la sombra inicia desactivada

    // }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Block"))
        {
            m_block = other.GetComponent<BlockBehaviour>();
           // BlockBehaviour parentBlock = GetComponentInParent<BlockBehaviour>();
            if (m_block != null && m_block != m_parentBlock &&m_parentBlock.isDraggable)
            {
                // Activar la sombra visual
                SetShadowVisible(true);
                if (m_zone == ConnectionZone.Top)
                    m_parentBlock.collidingWithTopShadowOF = m_block;
                else if (m_zone == ConnectionZone.Bottom)
                    m_parentBlock.collidingWithBottomShadowOf = m_block;
                // Notificar
                Debug.Log("[ShadowCollision] " + m_zone + " => Bloque " + m_block.name + " (ID: " + m_block.blockModel?.ID + ") ENTRÓ en la sombra de " + m_parentBlock.name + " (ID: " + m_parentBlock.blockModel?.ID + ")");
                OnBlockEntered?.Invoke(m_block);
                
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Block"))
        {
            m_block = other.GetComponent<BlockBehaviour>();
           // BlockBehaviour parentBlock = GetComponentInParent<BlockBehaviour>();
            if (m_block != null && m_block != m_parentBlock)
            {

                if (m_zone == ConnectionZone.Top && m_parentBlock.collidingWithTopShadowOF == m_block)
                    m_parentBlock.collidingWithTopShadowOF = null;
                else if (m_zone == ConnectionZone.Bottom && m_parentBlock.collidingWithBottomShadowOf == m_block)
                    m_parentBlock.collidingWithBottomShadowOf = null;
                SetShadowVisible(false);
                Debug.Log("[ShadowCollision] " + m_zone + " => Bloque " + m_block.name + " (ID: " + m_block.blockModel?.ID + ") SALIÓ de la sombra de " + m_parentBlock.name + " (ID: " + m_parentBlock.blockModel?.ID + ")");
                OnBlockExited?.Invoke(m_block);
                m_parentBlock.ClearConnectionZone();
               
            }
        }
    }

    public void Initialize(BlockBehaviour parent, ConnectionZone zone, UnityEngine.UI.Image img)
    {
        this.m_parentBlock = parent;
        this.m_zone = zone;
        this.m_shadowImage = img;

        // Ajustes iniciales de la imagen si existe
        if (m_shadowImage != null)
        {
            m_shadowImage.enabled = false;
            ;
            m_shadowImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        }

        // Configurar el collider para ser Trigger
        var boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider)
        {
            boxCollider.isTrigger = true;
        }
    }

    private void SetShadowVisible(bool visible)
    {
        Debug.Log($"SetShadowVisible: Setting visibility to {visible} on {m_block.blockModel.ID} para {gameObject.name}");
        if (m_shadowImage != null)
        {
            m_shadowImage.enabled = visible;
            if (visible)
            {
                
                 m_shadowImage.color = (m_zone == ConnectionZone.Top) ? new Color(0.5f, 0.5f, 0.5f, 0.3f) : new Color(0.5f, 0.5f, 0.5f, 0.3f); ;
            }
        }
        else
        {
            Debug.LogError($"Shadow image is null on {m_block.blockModel.ID} para {gameObject.name}");
        }
    }
}