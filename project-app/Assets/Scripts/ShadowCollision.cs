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
using System.Security.Policy;
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
            if (m_block != null && m_block != m_parentBlock)
            {
                // Activar la sombra visual
                SetShadowVisible(true);

                // Notificar
                Debug.Log($"[ShadowCollision] {m_zone} => Bloque {m_block.name} ENTRÓ en la sombra de {m_parentBlock.name}");
                OnBlockEntered?.Invoke(m_block);
                /* Debug.Log($"Colisión entrada: (ID: {block.blockModel?.ID}) con {gameObject.name} " +
                       $"en posición: {block.transform.localPosition}");
                 // Activar la sombra correspondiente del bloque estático
                 if (gameObject.name.Contains("Top"))
                 {
                     shadowImage.enabled = true;
                     parentBlock.shadowTop.SetActive(true);
                     parentBlock.shadowTop.GetComponent<Image>().enabled = true;
                     parentBlock.shadowTop.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Gris translúcido
                     Debug.Log($"Estado sombra Top: {parentBlock.shadowTop.GetComponent<Image>().enabled}, Color: {parentBlock.shadowTop.GetComponent<Image>().color}");
                 }
                 else if (gameObject.name.Contains("Bottom"))
                 {
                     shadowImage.enabled = true;
                     parentBlock.shadowBottom.SetActive(true);
                     parentBlock.shadowBottom.GetComponent<Image>().enabled = true;
                     parentBlock.shadowBottom.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f,0.3f); // Gris translúcido

                     Debug.Log($"Estado sombra Bottom: {parentBlock.shadowBottom.GetComponent<Image>().enabled}, Color: {parentBlock.shadowBottom.GetComponent<Image>().color}");
                 }

                 OnBlockEntered?.Invoke(block);*/

                /*  if (gameObject.name.Contains("Top"))
                  {
                      parentBlock.SetConnectionZone(ConnectionZone.Top);
                  }
                  else if (gameObject.name.Contains("Bottom"))
                  {
                      parentBlock.SetConnectionZone(ConnectionZone.Bottom);
                  }*/
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
                SetShadowVisible(false);
                Debug.Log($"[ShadowCollision] {m_zone} => Bloque {m_block.name} SALIÓ de la sombra de {m_parentBlock.name}");
                OnBlockExited?.Invoke(m_block);
                m_parentBlock.ClearConnectionZone();
                /* // Desactivar la sombra del bloque estático
                 if (gameObject.name.Contains("Top"))
                 {
                     shadowImage.enabled = false;
                     parentBlock.shadowTop.GetComponent<Image>().enabled = false;
                 }
                 else if (gameObject.name.Contains("Bottom"))
                 {
                     shadowImage.enabled = false;
                     parentBlock.shadowBottom.GetComponent<Image>().enabled = false;
                 }

                 OnBlockExited?.Invoke(block);
                 parentBlock.ClearConnectionZone();*/
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
        Debug.Log($"SetShadowVisible: Setting visibility to {visible} on {gameObject.name}");
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
            Debug.LogError($"Shadow image is null on {gameObject.name}");
        }
    }
}