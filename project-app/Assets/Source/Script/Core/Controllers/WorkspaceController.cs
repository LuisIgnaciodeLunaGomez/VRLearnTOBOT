/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 21/06/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: controlador principal del workspace que gestiona la lógica de bloques, toolbox y ejecución.
 */
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace UBlockly.UGUI
{
    public class WorkspaceController
    {
        public Workspace Workspace { get; private set; }
        public WorkspaceView WorkspaceView { get; private set; }
        public ToolboxController ToolboxController { get; private set; }
        
        public BlockController BlockController { get; private set; }

        private string mSavePath;
        private string GetSavePath()
        {
            if (string.IsNullOrEmpty(mSavePath))
            {
                mSavePath = Path.Combine(Application.persistentDataPath, "XmlSave");
                if (!Directory.Exists(mSavePath))
                    Directory.CreateDirectory(mSavePath);
            }
            return mSavePath;
        }

        public BaseToolbox Toolbox { get { return WorkspaceView.Toolbox; } }

        public WorkspaceController(Workspace model, WorkspaceView view)
        {
            this.Workspace = model;
            this.WorkspaceView = view;

            // Creamos los sub-controladores
            this.BlockController = new BlockController(this);

            // Enlazamos la vista principal con el modelo
            this.WorkspaceView.BindModel(this.Workspace);

            this.ToolboxController = new ToolboxController(this);
        }

        public void SaveWorkspace(string fileName)
        {
            var dom = UBlockly.Xml.WorkspaceToDom(this.Workspace);
            string text = UBlockly.Xml.DomToText(dom);

            string path = GetSavePath();
            fileName = string.IsNullOrEmpty(fileName) ? "Default.xml" : fileName + ".xml";
            path = Path.Combine(path, fileName);

            File.WriteAllText(path, text);
            Debug.Log($"Workspace guardado en: {path}");
        }
        public IEnumerator LoadWorkspace(string fileName)
        {
            // Limpiamos primero la vista y el modelo para preparar la carga.
            this.WorkspaceView.CleanViews();
            this.Workspace.Clear(); // Es importante limpiar el modelo también

            string path = Path.Combine(GetSavePath(), fileName + ".xml");
            string inputXml;

            // La lógica original para cargar desde una URL o un archivo local
            if (path.Contains("://"))
            {
                using (UnityWebRequest webRequest = UnityWebRequest.Get(path))
                {
                    yield return webRequest.SendWebRequest();
                    if (webRequest.result != UnityWebRequest.Result.Success)
                    {
                        throw new Exception(webRequest.error + ": " + path);
                    }
                    inputXml = webRequest.downloadHandler.text;
                }
            }
            else
            {
                inputXml = File.ReadAllText(path);
            }

            // Aquí estaba el error: parseamos el texto a XML
            var dom = UBlockly.Xml.TextToDom(inputXml);

            // Cargamos el XML en el modelo del Workspace
            UBlockly.Xml.DomToWorkspace(dom, this.Workspace);

            // Le decimos a la vista que se reconstruya a partir del nuevo estado del modelo
            this.WorkspaceView.BuildViews();
        }

        /// <summary>
        /// Inicia la ejecución de los scripts del workspace.
        /// </summary>
        public void StartExecution()
        {
            CSharp.Runner.Run(this.Workspace);
        }

        /// <summary>
        /// Detiene la ejecución actual.
        /// </summary>
        public void StopExecution()
        {
            CSharp.Runner.Stop();
        }

    }
}