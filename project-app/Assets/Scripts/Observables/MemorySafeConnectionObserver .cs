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
            connection?.RemoveObserver(this); 
        }
        else
        {
            mViewRef.OnConnectStateUpdated(newValue); 
        }
    }

    public void OnUpdated(object subject, UpdateState args)
    {
        throw new System.NotImplementedException();
    }
}//fin clase MemorySafeConnectionObserver