/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 08/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

public class VariableObserver : IObserver<VariableUpdateData>
{
    private BlockListView mToolboxRef;
    public VariableObserver(BlockListView toolbox) { mToolboxRef = toolbox; }
    public void OnUpdated(object subject, VariableUpdateData args)
    {
        if (mToolboxRef == null || mToolboxRef.gameObject == null)
        {
            try { ((Observable<VariableUpdateData>)subject)?.RemoveObserver(this); } catch { }
        }
        else
            mToolboxRef.OnVariableUpdate(args);
    }
}//Fin clase VariableObserver