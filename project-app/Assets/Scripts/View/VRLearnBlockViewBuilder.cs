/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 15/06/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using UBlockly;
using UnityEngine;


public static class VRLearnBlockViewBuilder
{

    /// <summary>
    /// El método principal que construye la vista de un bloque.
    /// </summary>
    public static GameObject BuildBlockView(BlockModel blockModel, WorkSpaceView workspaceView)
    {
        // 1.  VALIDACIÓN y CARGA DEL LIENZO 
        if (blockModel.Definition == null)
        {
            Debug.LogError($"[VRLearnBlockViewBuilder] El modelo del bloque '{blockModel.Type}' no tiene Definición. No se puede construir la vista.");
            return null;
        }

        string spriteName = blockModel.Definition.spriteName;
        GameObject rootPrefab = BlockResMgr.Get().LoadBlockViewPrefab(spriteName);

        if (rootPrefab == null)
        {
            Logger.LogError($"[VRLearnBlockViewBuilder] No se pudo encontrar el prefab base para el sprite '{spriteName}'. Revisar BlockResMgr y la carpeta de Resources.");
            // Se devuelve un GO vacío para evitar que falle el editor, pero este bloque se verá mal
            return new GameObject("ERROR_PREFAB_NOT_FOUND_" + blockModel.Type);
        }

        // 2.  INSTANCIACIÓN Y ASIGNACIÓN DE SCRIPTS 
        GameObject blockRoot = GameObject.Instantiate(rootPrefab);
        blockRoot.name = "BlockView_" + blockModel.Definition.type;

        if (blockRoot.GetComponent<RectTransform>() == null)
        {
            Debug.LogWarning($"El prefab base '{spriteName}' fue instanciado sin un RectTransform. Añadiendo uno manualmente.", blockRoot);
            blockRoot.AddComponent<RectTransform>();
        }

        // Añadir/obtener el script BlockView!
        BlockView blockViewScript = AddViewComponent<BlockView>(blockRoot);
        blockViewScript.BindModel(blockModel, workspaceView);

        // Obtenemos el contenedor de los inputs desde el prefab base.
        Transform inputsContainer = blockRoot.transform.Find("InputsContainer");
        if (inputsContainer == null)
        {
            Debug.LogError($"[VRLearnBlockViewBuilder] El prefab base '{spriteName}' NO tiene un GameObject hijo llamado 'InputsContainer'.", blockRoot);
            // Se crea uno de emergencia para no detener el proceso
            GameObject emergencyContainer = new GameObject("InputsContainer");
            emergencyContainer.transform.SetParent(blockRoot.transform, false);
            inputsContainer = emergencyContainer.transform;
        }

        foreach (InputModel inputModel in blockModel.InputList)
        {
            //  PASO 1: CREAR JERARQUÍA VISUAL COMPLETA 

            // A. Crear el GameObject padre del Input
            GameObject inputGO = new GameObject("Input_" + inputModel.Name);
            inputGO.AddComponent<RectTransform>();
            inputGO.transform.SetParent(inputsContainer, false);

            // B. Añadir el script,
            InputView inputViewScript = AddViewComponent<InputView>(inputGO);

            // C. Construir TODOS los hijos visuales del InputView PRIMERO.
            //    Esto llenará la jerarquía de Transforms y la lista LÓGICA de vistas hijas (m_ChildViews).
            foreach (FieldModel fieldModel in inputModel.FieldRow)
            {
                BuildFieldView(fieldModel, inputViewScript);
            }
            if (inputModel.Type == EConnection.InputValue || inputModel.Type == EConnection.NextStatement)
            {
                BuildConnectionInputView(inputModel, inputViewScript);
            }

            //  PASO 2: VINCULAR EL MODELO A LA VISTA 
            
            inputViewScript.BindModel(inputModel, blockViewScript);

            //  PASO 3: AÑADIR A LA JERARQUÍA DEL BLOQUE 
            
            blockViewScript.AddChild(inputViewScript);
        }

        blockViewScript.BuildLayout(); // Inicia la cascada de layout.
        return blockRoot;
    }

    /// <summary>
    /// Instancia el prefab de la pieza visual correcta para un FieldModel y lo configura.
    /// </summary>
    /// <param name="fieldModel">El modelo de datos del campo que se va a visualizar.</param>
    /// <param name="parentInputView">La vista del Input que contendrá a esta pieza.</param>
    private static void BuildFieldView(FieldModel fieldModel, InputView parentInputView)

    {
        // Comprobación inicial para evitar errores si el modelo es nulo.
        if (fieldModel == null)
        {
            Debug.LogError("[BuildFieldView] Se ha intentado construir una vista para un FieldModel nulo.");
            return;
        }

        GameObject fieldInstance = null;  // El GameObject que se instanciará.
        FieldView fieldViewScript = null; // El script de vista que se añadirá/obtendrá.

       
        if (fieldModel is FieldLabelModel labelModel)
        {
            fieldInstance = GameObject.Instantiate(BlockPieceMgr.Get().LabelPrefab, parentInputView.transform);
        }
        else if (fieldModel is FieldNumberModel numberModel)
        {
            fieldInstance = GameObject.Instantiate(BlockPieceMgr.Get().NumberInputPrefab, parentInputView.transform);
        }
        else if (fieldModel is FieldTextInputModel textInputModel)
        {
            fieldInstance = GameObject.Instantiate(BlockPieceMgr.Get().TextInputPrefab, parentInputView.transform);
        }
        else if (fieldModel is FieldDropdownModel dropdownModel)
        {
            fieldInstance = GameObject.Instantiate(BlockPieceMgr.Get().DropdownPrefab, parentInputView.transform);
        }
        else if (fieldModel is FieldVariableModel variableModel)
        {
            fieldInstance = GameObject.Instantiate(BlockPieceMgr.Get().VariablePrefab, parentInputView.transform);
        }
        else if (fieldModel is FieldCheckboxModel checkboxModel)
        {
            fieldInstance = GameObject.Instantiate(BlockPieceMgr.Get().CheckboxPrefab, parentInputView.transform);
        }
        else if (fieldModel is FieldImageModel imageModel)
        {
            fieldInstance = GameObject.Instantiate(BlockPieceMgr.Get().ImagePrefab, parentInputView.transform);
        }
        else
        {
            Debug.LogWarning($"[BuildFieldView] Tipo de campo no manejado: {fieldModel.GetType()}. No se creará ninguna vista para él.");
            return;
        }

        // LÍNEA DE SEGURIDAD (Se aplica a cualquier 'fieldInstance' que se haya creado)
        if (fieldInstance != null && fieldInstance.GetComponent<RectTransform>() == null)
        {
            Debug.LogWarning($"El prefab para {fieldModel.GetType().Name} no tenía RectTransform. Añadiendo uno.", fieldInstance);
            fieldInstance.AddComponent<RectTransform>();
        }

        if (fieldModel is FieldLabelModel labelModel_bind)
        {
            fieldViewScript = AddViewComponent<FieldLabelView>(fieldInstance);
            fieldViewScript.BindModel(labelModel_bind);
        }
        else if (fieldModel is FieldNumberModel numberModel_bind)
        {
            fieldViewScript = AddViewComponent<FieldNumberInputView>(fieldInstance);
            fieldViewScript.BindModel(numberModel_bind);
        }
        else if (fieldModel is FieldTextInputModel textInputModel_bind)
        {
            fieldViewScript = AddViewComponent<FieldTextInputView>(fieldInstance);
            fieldViewScript.BindModel(textInputModel_bind);
        }
        else if (fieldModel is FieldDropdownModel dropdownModel_bind)
        {
            fieldViewScript = AddViewComponent<FieldDropdownView>(fieldInstance);
            fieldViewScript.BindModel(dropdownModel_bind);
        }
        else if (fieldModel is FieldVariableModel variableModel_bind)
        {
            fieldViewScript = AddViewComponent<FieldVariableView>(fieldInstance);
            fieldViewScript.BindModel(variableModel_bind);
        }
        else if (fieldModel is FieldCheckboxModel checkboxModel_bind)
        {
            fieldViewScript = AddViewComponent<FieldCheckboxView>(fieldInstance);
            fieldViewScript.BindModel(checkboxModel_bind);
        }
        else if (fieldModel is FieldImageModel imageModel_bind)
        {
            fieldViewScript = AddViewComponent<FieldImageView>(fieldInstance);
            fieldViewScript.BindModel(imageModel_bind);
        }

        // 1. Asignar un nombre descriptivo al GO en la jerarquía para facilitar la depuración.
        if (fieldInstance != null && !string.IsNullOrEmpty(fieldModel.Name))
        {
            // Ejemplo de nombre: "FieldLabel(LABEL_START)"
            fieldInstance.name = fieldModel.GetType().Name.Replace("Model", "View") + "(" + fieldModel.Name + ")";
        }

        // 2. Añadir la VISTA recién creada como hija LÓGICA de su VISTA padre (el InputView).
      
        if (fieldViewScript != null)
        {
            parentInputView.AddChild(fieldViewScript);
        }
    }

    /// <summary>
    /// Construye la vista visual para una conexión de entrada (el "hueco" para Valor o Statement).
    /// Instancia el prefab del "slot", le inyecta su script de vista y lo vincula al modelo de conexión.
    /// </summary>
    /// <param name="inputModel">El modelo del Input que requiere la conexión.</param>
    /// <param name="parentInputView">La vista del Input que contendrá a esta conexión visual.</param>
    private static void BuildConnectionInputView(InputModel inputModel, InputView parentInputView)
    {
        //  1. Validación 
        // Si el InputModel no espera una conexión lógica, no hay nada que construir.
        if (inputModel?.Connection == null)
        {
            Debug.LogWarning($"[BuildConnectionInputView] Se intentó construir una vista de conexión para el Input '{inputModel?.Name}', pero su modelo de conexión es nulo. Esto es normal para inputs dummy.");
            return;
        }

        GameObject connectionInstance = null;

        //  2. Selección del Prefab de Hueco Correcto 
        // Dependiendo del tipo de conexión que espera el modelo, instanciamos un prefab u otro.
        if (inputModel.Type == EConnection.InputValue)
        {
            // El modelo espera un bloque reportero (valor). Usamos el prefab para el hueco de valor.
            connectionInstance = GameObject.Instantiate(BlockPieceMgr.Get().InputValueSlotPrefab, parentInputView.transform);
        }
        else if (inputModel.Type == EConnection.NextStatement)
        {
            // El modelo espera una pila de bloques (statement). Usamos el prefab para el hueco 'C'.
            connectionInstance = GameObject.Instantiate(BlockPieceMgr.Get().InputStatementSlotPrefab, parentInputView.transform);
        }
        else
        {
            Debug.LogError($"[BuildConnectionInputView] Tipo de InputModel no manejado: {inputModel.Type}. No se pudo crear la vista de conexión.");
            return;
        }

        // 3. Instanciación y Configuración 
        if (connectionInstance != null)
        {
            // Aplicamos la línea de seguridad para asegurar el RectTransform.
            if (connectionInstance.GetComponent<RectTransform>() == null)
            {
                Debug.LogWarning($"El prefab para la conexión de '{inputModel.Name}' no tenía RectTransform. Añadiendo uno.", connectionInstance);
                connectionInstance.AddComponent<RectTransform>();
            }

            // Asignamos un nombre descriptivo para la depuración en la jerarquía.
            connectionInstance.name = "ConnectionInputView(" + inputModel.Name + ")";

            // Inyectamos el script de vista 'ConnectionInputView' en el GO recién creado.
            
            ConnectionInputView connectionViewScript = AddViewComponent<ConnectionInputView>(connectionInstance);

            // Vinculamos la vista al MODELO DE CONEXIÓN específico que le corresponde.
            // El ConnectionInputView ahora sabe todo sobre su conexión (qué tipos acepta, etc.).
            // Se le pasa el SourceBlockView del padre para que la conexión sepa a qué bloque pertenece.
            connectionViewScript.BindModel(inputModel.Connection, parentInputView.ParentBlockView);

            // Añadimos la nueva VISTA DE CONEXIÓN como hija LÓGICA de la VISTA del input.
            // Esto es crucial para la cascada de layout manual (Conexión es el último elemento en una fila de input.)
            parentInputView.AddChild(connectionViewScript);
        }
        else
        {
            Debug.LogError($"[BuildConnectionInputView] Falló la instanciación del prefab de conexión para el Input '{inputModel.Name}'. Revisa el BlockPieceMgr y la carpeta de Resources.");
        }
    }

    /// <summary>
    /// Método genérico para añadir un componente de vista a un GO
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="viewObject"></param>
    /// <returns></returns>
    private static T AddViewComponent<T>(GameObject viewObject) where T : BaseView
    {
        if (viewObject == null)
        {
            Debug.LogError("AddViewComponent fue llamado con un GameObject nulo.");
            return null;
        }

        T viewScript = viewObject.GetComponent<T>();
        if (viewScript == null)
        {
            // El script no existe, así que lo añadimos dinámicamente
            viewScript = viewObject.AddComponent<T>();
        }

        // InitComponents - método que captura referencias a sus propios componentes (como GetComponent<TextMeshProUGUI>())
        viewScript.InitComponents();

        return viewScript;
    }
}
