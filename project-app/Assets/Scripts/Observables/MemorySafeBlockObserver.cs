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
 * Descripción: 
 */

using System;
using UnityEditor;
using UnityEngine;

public class MemorySafeBlockObserver : IObserver<int>
{
    private BlockView m_view;

    public MemorySafeBlockObserver(BlockView view)
    {
        this.m_view = view;
    }

    public void OnUpdated(object model, int updateStateMask)
    {
        if (m_view == null || m_view.Block != model || m_view.gameObject == null)
        {
            try
            {
               
                ((Observable<int>)model).RemoveObserver(this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to remove observer: {ex.Message}");
            }
            return;
        }
        foreach (BlockUpdateType stateType in Enum.GetValues(typeof(BlockUpdateType))) 
        {
            int stateMaskValue = 1 << (int)stateType; 
            if ((stateMaskValue & updateStateMask) != 0)
            {
                m_view.HandleModelUpdate((BlockModel)model, stateType); 
            }
        }
    }

}//fin clase MemorySafeBlockObserver