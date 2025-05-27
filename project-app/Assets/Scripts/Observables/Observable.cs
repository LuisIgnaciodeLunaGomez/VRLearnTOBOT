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
using UnityEngine;
public abstract class Observable<TArgs>
{
    private readonly List<IObserver<TArgs>> mObservers = new List<IObserver<TArgs>>();

    public void AddObserver(IObserver<TArgs> observer)
    {
        if (observer == null)
        {
            Debug.LogError("[Observable] Cannot add null observer."); 
            return;
        }

        if (!mObservers.Contains(observer))

            mObservers.Add(observer);

        else
            Debug.LogWarning("[Observable] Observer already added: " + observer.GetType().Name);
    }

    public void RemoveObserver(IObserver<TArgs> observer)
    {
        if (observer == null)
        {
            Debug.LogError("[Observable] Cannot remove null observer."); 
            return;
        }
        //mObservers.Remove(observer);

        if (mObservers.Remove(observer))
        {
             Debug.Log("[Observable] Observer removed: " + observer.GetType().Name); 
        }
        else
        {
             Debug.LogWarning("[Observable] Observer not found for removal: " + observer.GetType().Name); 
        }
    }

    public void FireUpdate(TArgs args)
    {
        for (int i = mObservers.Count - 1; i >= 0; i--)
        {
            // mObservers[i].OnUpdated(this, args);
            try
            {
                mObservers[i].OnUpdated(this, args); // 'this' es la instancia del Observable (el Runner)
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Observable] Error notifying observer {mObservers[i].GetType().Name}: {ex.Message}");
            }

        }
    }
}//Fin clase Observable