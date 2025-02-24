using System;
using UnityEngine;
using Object = UnityEngine.Object;
public static class GeneradorUI
{
    public static WorkSpaceView workSpaceView; //Vista del área de trabajo
    public static Canvas UICanvas; //Canvas de la interfaz de usuario

    /**
    * Método que se encarga de crear un nuevo espacio de trabajo
    */
    public static void NewWorkspace()
    {
        if (workSpaceView != null)
            throw new Exception("AVISO: Ya existe un entorno de trabajo"); //Si ya existe un área de trabajo se lanza una excepción

        WorkSpace workspace = new WorkSpace(); //Se crea un nuevo espacio de trabajo con sus opciones
        workSpaceView = Object.FindFirstObjectByType<WorkSpaceView>(); //Se busca el área de trabajo en la escena
        workSpaceView.BindModel(workspace); //Se enlaza el modelo lógico con la vista
        UICanvas = workSpaceView.GetComponentInParent<Canvas>(); //Se obtiene el canvas de la interfaz de usuario

    }

    /**
    * Método que se encarga de destruir el espacio de trabajo
    */
    public static void DestroyWorkspace()
    {
        if (workSpaceView == null)
            return;

        workSpaceView.Dispose();
        if (workSpaceView.gameObject != null)
            GameObject.Destroy(workSpaceView.gameObject);
        workSpaceView = null;
    }


}
