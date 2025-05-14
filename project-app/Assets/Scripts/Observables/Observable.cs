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
 * Descripción: Clase que gestiona las conexiones entre bloques
 */

using System.Collections.Generic;
public abstract class Observable<TArgs>
{
    private readonly List<IObserver<TArgs>> mObservers = new List<IObserver<TArgs>>();

    public void AddObserver(IObserver<TArgs> observer)
    {
        if (!mObservers.Contains(observer))
            mObservers.Add(observer);
    }

    public void RemoveObserver(IObserver<TArgs> observer)
    {
        mObservers.Remove(observer);
    }

    public void FireUpdate(TArgs args)
    {
        for (int i = mObservers.Count - 1; i >= 0; i--)
        {
            mObservers[i].OnUpdated(this, args);
        }
    }
}//Fin clase Observable