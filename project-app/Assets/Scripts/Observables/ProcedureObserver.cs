
/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 01/04/2025
 * 
 * Versión: 1.0.
 * 
 * Descripción: 
 */

public class ProcedureObserver : IObserver<ProcedureUpdateData>
{
    private BlockListView mToolboxRef;
    public ProcedureObserver(BlockListView toolbox) { mToolboxRef = toolbox; }
    public void OnUpdated(object subject, ProcedureUpdateData args)
    {
        if (mToolboxRef == null || mToolboxRef.gameObject == null)
        {
            try { ((Observable<ProcedureUpdateData>)subject)?.RemoveObserver(this); } catch { }
        }
        else
            mToolboxRef.OnProcedureUpdate(args);
    }
}//Fin clase ProcedureObserver