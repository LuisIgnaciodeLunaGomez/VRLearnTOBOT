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

public class MemorySafeBlockObserver : IObserver<int>
{
    private BlockView view;

    public MemorySafeBlockObserver(BlockView view)
    {
        this.view = view;
    }

    public void OnUpdated(object model, int updateStateMask)
    {
        if (view == null || view.Block != model)
        {
            ((Block)model).RemoveObserver(this);
            return;
        }

        foreach (UpdateStates state in Enum.GetValues(typeof(UpdateStates)))
        {
            if (((1 << (int)state) & updateStateMask) != 0)
            {
                view.UpdateBlockState(state);
            }
        }
    }
}