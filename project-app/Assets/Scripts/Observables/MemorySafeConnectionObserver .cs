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
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using UnityEngine;

public class MemorySafeConnectionObserver : IObserver<UpdateState> 
{
    private ConnectionView mViewRef;
    public MemorySafeConnectionObserver(ConnectionView viewRef)
    {
        mViewRef = viewRef;
    }

    public void OnUpdate(ConnectionModel connection, UpdateState newValue) 
    {
        if (mViewRef == null || mViewRef.ViewTransform == null || mViewRef.ConnectionModel != connection)
        {
            Debug.LogWarning($"[MemorySafeConnectionObserver] ViewRef invalid or Connection mismatch. Removing observer for Conn: {connection?.GetHashCode()} / View: {mViewRef?.GetHashCode()}");
            connection?.RemoveObserver(this); 
        }
        else
        {
            mViewRef.OnConnectStateUpdated(newValue); 
        }
    }

    public void OnUpdated(object subject, UpdateState args)
    {
        if (subject is ConnectionModel connection)
        {
            OnUpdate(connection, args); // 'args' es el UpdateState
        }
        else
        {
            
            Debug.LogWarning($"[MemorySafeConnectionObserver] OnUpdated called by unexpected subject type: {subject?.GetType()}");

           
        }
    }
}//fin clase MemorySafeConnectionObserver