/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha:01/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Integración de la estructura de Ublockly dentro del proyecto por semejanza con ScratchBlocks. 
 */

using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class ToolboxBlockCategory
{
    public string CategoryName;
    public string Colour; public string Custom;
    public string BlockTypePrefix;
    public List<string> BlockList;

    [NonSerialized] private bool mInited = false;

    public Color Color { get; private set; }


    public void Init()
    {
        if (mInited) return;

      
       /* if (!string.IsNullOrEmpty(BlockTypePrefix)) 
        {
            var typesFromPrefix = BlockFactory.Instance.GetBlockTypesOfPrefix(BlockTypePrefix);
            if (typesFromPrefix != null)
            {
                // Solo añade si BlockList está realmente vacío para evitar duplicados
                if (BlockList == null || BlockList.Count == 0)
                {
                    if (BlockList == null) BlockList = new List<string>();
                    BlockList.AddRange(typesFromPrefix);
                }
                // else Debug.LogWarning($"BlockList for '{CategoryName}' already populated. Ignoring prefix '{BlockTypePrefix}'.");
            }
            else
                Debug.LogWarning($"No block types found for prefix '{BlockTypePrefix}' in category '{CategoryName}'");
        }*/
        if (BlockList == null)
        {
            BlockList = new List<string>(); 
        }


        if (!string.IsNullOrEmpty(Colour))
        {
          
            if (ColorUtility.TryParseHtmlString(Colour, out Color parsedColor))
            {
                Color = parsedColor;
            }
            else
            {
                Color = UnityEngine.Color.grey; 
                Debug.LogWarning($"Failed to parse ColorHex '{Colour}' for category '{CategoryName}'. Using default grey.");
            }
        }
        else
        {
            Color = UnityEngine.Color.grey; 
            Debug.LogWarning($"No ColorHex defined for category '{CategoryName}'. Using default grey.");
        }
        mInited = true;
    }

}