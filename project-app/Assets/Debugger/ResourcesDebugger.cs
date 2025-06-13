using System.IO;
using UnityEngine;

public class ResourcesDebugger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ListResourcesFiles();
    }

    void ListResourcesFiles()
    {
        string path = Application.dataPath + "/Resources"; // Ruta absoluta a Resources/
        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            Debug.Log("Archivos en Resources:");
            foreach (string file in files)
            {
                Debug.Log(file);
            }
        }
        else
        {
            Debug.LogError("La carpeta Resources no existe en: " + path);
        }
    }
}
