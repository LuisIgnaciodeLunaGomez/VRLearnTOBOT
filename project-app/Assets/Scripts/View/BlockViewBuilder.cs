/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 22/02/2025
 * 
 * Versión: 2.0.0
 * 
 * Descripción: Clase que representa un bloque visual en la interfaz de usuario premite la vinculación del modelo lógico con la UI
 * 
 */
using UnityEngine;
using UnityEngine.UI; 
using TMPro;        
using System.Collections.Generic;
using System.Linq;
using System; 

public static class BlockViewBuilder
{
   

    /**
     * Construye la estructura interna de GameObjects y Views hijas
     * dentro de un GameObject que ya tiene (o al que se le añadirá) un BlockView.
     * @param targetBlockViewGO El GameObject que contendrá/es el BlockView.
     * @param definition La definición del bloque que determina la estructura.
     */
    public static void BuildBlockViewContent(GameObject targetBlockViewGO, BlockDefinition definition)
    {
        if (targetBlockViewGO == null || definition == null)
        {
            Debug.LogError("BlockViewBuilder: Target GameObject or BlockDefinition is null.");
            return;
        }

        BlockView blockView = targetBlockViewGO.GetComponent<BlockView>();
        if (blockView == null)
        {
            // Añadir BlockView si no existe 
            blockView = targetBlockViewGO.AddComponent<BlockView>();
        }
        else
        {
            // Limpiar hijos visuales anteriores 
            foreach (Transform child in targetBlockViewGO.transform)
            {
                
                if (child.GetComponent<BaseView>() != null) 
                    GameObject.Destroy(child.gameObject);
            }
            blockView.ChildViews.Clear();
        }


        // Añadir Vistas de Conexión Principales (Prev, Next, Output)
        CreateMainConnectionViews(blockView, definition);

        // CrLíneas (LineGroupViews) y sus Contenidos (InputViews -> Fields/Connections)
        CreateInputLines(blockView, definition);

        //Configurar Imagen de Fondo 
        EnsureBackgroundImage(blockView, definition);

        //Añadir LayoutElement si el layout padre lo requiere
        EnsureLayoutElement(blockView);

    }

   
    private static void CreateMainConnectionViews(BlockView blockView, BlockDefinition definition)
    {
        // Añadir Prev/Output
        if (definition.hasOutput)
            AddConnectionView(blockView, EConnection.OutputValue, "Connection_Output");
        else if (definition.hasPreviousStatement)
            AddConnectionView(blockView, EConnection.PrevStatement, "Connection_Previous");

        // Añadir Next, si no tiene Output y sí tiene Next
        if (!definition.hasOutput && definition.hasNextStatement)
            AddConnectionView(blockView, EConnection.NextStatement, "Connection_Next");
    }

    private static ConnectionView AddConnectionView(BlockView parentBlockView, EConnection type, string gameObjectName)
    {
        // Crear GameObject hijo
        GameObject cvGO = new GameObject(gameObjectName);
        cvGO.transform.SetParent(parentBlockView.ViewTransform, false); // Hijo del BlockView

        // Añadir RectTransform 
        RectTransform rect = cvGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); // Top-Left
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = Vector2.zero; // UpdateLayout lo ajustará
        rect.sizeDelta = new Vector2(10, 10); // Tamaño por defecto/placeholder

        // Añadir la Vista específica
        ConnectionView connectionView;
        if (type == EConnection.InputValue || type == EConnection.NextStatement)
            connectionView = cvGO.AddComponent<ConnectionInputView>();
        else
            connectionView = cvGO.AddComponent<ConnectionView>();

        connectionView.InitializeConnectionType(type); 

        // Añadir como hijo visual en BaseView para la gestión del layout
        parentBlockView.AddChildView(connectionView);

        return connectionView;
    }
    

    private static void CreateInputLines(BlockView blockView, BlockDefinition definition)
    {
        LineGroupView currentLine = null;

        bool useSingleLine = definition.GetInputsInlineEffective(); 

        if (useSingleLine)
        {
            currentLine = CreateLineGroupView(blockView);
        }

        foreach (ArgumentDefinition argDef in definition.args) 
        {
        
            if (!useSingleLine && (argDef.type == "input_value" || argDef.type == "input_statement"))
            {
                currentLine = CreateLineGroupView(blockView);
            }
            else if (currentLine == null) // 
            {
                currentLine = CreateLineGroupView(blockView);
            }

            // Si el arg es un Input, se crea InputView y le añadimos sus Fields
            if (argDef.type == "input_value" || argDef.type == "input_statement" || argDef.type == "input_dummy")
            {
                CreateInputView(currentLine, definition, argDef);
            }

            // `args` define los elementos visuales en orden.
            else if (argDef.type.StartsWith("field_"))
            {
                // Necesita  InputView para contener este Field
                InputView dummyInput = CreateOrGetImplicitInputView(currentLine, EConnection.DummyInput, $"DummyFieldContainer_{currentLine.ChildViews.Count}");
                CreateFieldView(dummyInput, argDef); 
            }
            else if (argDef.type == "label")
            { 
                // Tratamiento especial para 'label' como field_label
                InputView dummyInput = CreateOrGetImplicitInputView(currentLine, EConnection.DummyInput, $"DummyLabelContainer_{currentLine.ChildViews.Count}");
                CreateFieldView(dummyInput, argDef); 
            }
            else if (argDef.type == "image")
            { 
                // Tratamiento para 'image' como field_image
                InputView dummyInput = CreateOrGetImplicitInputView(currentLine, EConnection.DummyInput, $"DummyImageContainer_{currentLine.ChildViews.Count}");
                CreateFieldView(dummyInput, argDef);
            }
            // TODO Ignorar otros tipos desconocidos o manejar como error
        }
    }

    private static LineGroupView CreateLineGroupView(BlockView parentBlockView)
    {
        GameObject lgGO = new GameObject("LineGroup_" + parentBlockView.ChildViews.Count(v => v is LineGroupView));
        lgGO.transform.SetParent(parentBlockView.ViewTransform, false);
        RectTransform rect = lgGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = Vector2.zero;

        LineGroupView lineGroupView = lgGO.AddComponent<LineGroupView>();
        parentBlockView.AddChildView(lineGroupView);
        return lineGroupView;
    }

    // Crea un InputView ( input_value, input_statement, input_dummy)
    private static InputView CreateInputView(LineGroupView parentLineGroup, BlockDefinition definition, ArgumentDefinition inputArgDef)
    {
        string name = inputArgDef.name ?? $"Input_{parentLineGroup.ChildViews.Count}";
        GameObject ivGO = new GameObject($"Input_{name}");
        ivGO.transform.SetParent(parentLineGroup.ViewTransform, false);
        RectTransform rect = ivGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); 
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = Vector2.zero; 

        InputView inputView = ivGO.AddComponent<InputView>();
        parentLineGroup.AddChildView(inputView);

        //Obtener el InputModel correspondiente 
        InputModel inputModel = definition.CreateModelInputs(null) 
                                          .FirstOrDefault(im => im.Name == inputArgDef.name); 
        if (inputModel == null)
        {
            Debug.LogError($"Builder: Could not find InputModel for Arg '{inputArgDef.name}' in '{definition.type}'");
            return inputView; 
        }

        //Iterar sobre los FieldModels del InputModel y crear FieldViews
        foreach (FieldModel fieldModel in inputModel.FieldRow)
        {
            CreateFieldViewFromModel(inputView, fieldModel);
        }

        //Si el InputModel tiene conexión, crear ConnectionInputView
        if (inputModel.Connection != null)
        {
            CreateConnectionInputView(inputView, inputModel.Connection.Type); 
        }

        return inputView;
    }

    //  obtener/crear InputView implícito para fields sueltos
    private static InputView CreateOrGetImplicitInputView(LineGroupView parentLine, EConnection type, string name)
    {
        // Si el último hijo de la línea es un InputView Dummy, lo reutilizo
        if (parentLine.LastChildView is InputView lastInput && lastInput.InputModel?.Type == EConnection.DummyInput) 
        {
            return lastInput;
        }
        // Si no, creamos uno nuevo
        // Creamos un ArgDef temporal solo para pasar al builder de InputView 
        ArgumentDefinition dummyArgDef = new ArgumentDefinition { type = "input_dummy", name = name };
       

        // Crear GO + Vista directamente
        GameObject ivGO = new GameObject($"Input_{name}");
        ivGO.transform.SetParent(parentLine.ViewTransform, false);
        RectTransform rect = ivGO.AddComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = Vector2.zero;
        InputView inputView = ivGO.AddComponent<InputView>();
        // inputView.BindModel(dummyModel);  //Si al final decido crear un modelo para dummy
        parentLine.AddChildView(inputView);
        return inputView;
    }

    // Añade un FieldView hijo a un InputView basado en un ArgumentDefinition
    private static FieldView CreateFieldView(InputView parentInputView, ArgumentDefinition argDef)
    {
        if (parentInputView == null || argDef == null)  
        {
            Debug.LogError("CreateFieldView: parentInputView or argDef is null.");
            return null;
        }

        string name = argDef.name ?? $"Field_{parentInputView.ChildViews.Count}";

        // Crear GameObject con un nombre descriptivo
        GameObject fvGO = new GameObject($"Field_{argDef.type}_{name}");
        fvGO.transform.SetParent(parentInputView.ViewTransform, false);

        // Añadir RectTransform y configurar anclaje y posición
        RectTransform rect = fvGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); 
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = Vector2.zero; 

        FieldView fieldView = null;// Componente lógico FieldView (FieldLabelView, FieldInputView...)

        // Añadir el componente FieldView específico basado en argDef.type

        try {

            Debug.Log($"<color=orange>Processing Field Type: '{argDef.type}' for Arg Name: '{argDef.name ?? "N/A"}'</color>");
            switch (argDef.type)
            {
                case "field_label":
                case "label": // Tratar 'label' como 'field_label'
                     //Obtengo o añado TextMeshProUGUI 
                    TextMeshProUGUI tmpText = fvGO.GetComponent<TextMeshProUGUI>();
                    if (tmpText == null) tmpText = fvGO.AddComponent<TextMeshProUGUI>();

                    // Configuro el texto
                    tmpText.text = argDef.value ?? "";
                    tmpText.fontSize = 16; // Configurar tamaño
                    tmpText.color = Color.black; // Configurar color 
                    tmpText.alignment = TextAlignmentOptions.Left; 
                    // tmpText.font = ... // fuente si es necesario

                    // Añado el componente lógico FieldLabelView
                    fieldView = fvGO.AddComponent<FieldLabelView>();
                   

                    //  Añadir LayoutElement para controlar tamaño preferido por el texto
                    LayoutElement leLabel = fvGO.AddComponent<LayoutElement>();
                    leLabel.flexibleWidth = 0; // No flexible por defecto
                                             

                    break;
                case "field_number":
                    // /Obtengo o añado TMP_InputField
                    TMP_InputField numInputField = fvGO.GetComponent<TMP_InputField>();
                    if (numInputField == null) numInputField = fvGO.AddComponent<TMP_InputField>();

                    //  Añado fondo, área de texto, y texto 
                    Image numBgImage = fvGO.GetComponent<Image>();
                    if (numBgImage == null) numBgImage = fvGO.AddComponent<Image>();
                    numBgImage.sprite = Resources.Load<Sprite>("circle");
                    numBgImage.type = Image.Type.Sliced;
                    numBgImage.color = Color.white;

                    // Creo hijos para Text Area y Text 
                    GameObject numTextAreaGO = CreateChildGO(fvGO, "Text Area", typeof(RectTransform));
                    ConfigureRectFillWithPadding(numTextAreaGO.GetComponent<RectTransform>(), 5, 5, 3, 3);

                    GameObject numTextGO = CreateChildGO(numTextAreaGO, "Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    ConfigureRectFill(numTextGO.GetComponent<RectTransform>());
                    TextMeshProUGUI numTmpText = numTextGO.GetComponent<TextMeshProUGUI>();
                    numTmpText.fontSize = 16;
                    numTmpText.color = Color.black;
                    numTmpText.alignment = TextAlignmentOptions.Left;

                    // Configuro TMP_InputField para NÚMEROS
                    numInputField.textViewport = numTextAreaGO.GetComponent<RectTransform>();
                    numInputField.textComponent = numTmpText;
                    numInputField.text = argDef.defaultValue ?? "0"; 
                    numInputField.contentType = TMP_InputField.ContentType.DecimalNumber; 

                    // Añado componente lógico FieldNumberInputView
                    fieldView = fvGO.AddComponent<FieldNumberInputView>(); 

                    //Añado LayoutElement
                    LayoutElement leNum = fvGO.AddComponent<LayoutElement>();
                    leNum.preferredWidth = 40; 
                    leNum.preferredHeight = 30;
                    leNum.flexibleWidth = 0;
                    break;
                case "field_input":
                case "input": // Tratar 'input' como 'field_input'
                    //Obtengo o añado TMP_InputField (
                    TMP_InputField tmpInputField = fvGO.GetComponent<TMP_InputField>();
                    if (tmpInputField == null) tmpInputField = fvGO.AddComponent<TMP_InputField>();

                    // imagen de fondo y un área de texto hija
                    Image bgImage = fvGO.GetComponent<Image>(); // Fondo del input field
                    if (bgImage == null) bgImage = fvGO.AddComponent<Image>();
                    bgImage.sprite = Resources.GetBuiltinResource<Sprite>("circle"); // Sprite por defecto
                    bgImage.type = Image.Type.Sliced;
                    bgImage.color = Color.white; // O el color que desees

                    // Creao GO hijo para el área de texto
                    GameObject textAreaGO = new GameObject("Text Area");
                    RectTransform textAreaRect = textAreaGO.AddComponent<RectTransform>();
                    textAreaRect.SetParent(fvGO.transform, false);
                    // Configuro RectTransform del Text Area para llenar con padding
                    textAreaRect.anchorMin = Vector2.zero;
                    textAreaRect.anchorMax = Vector2.one;
                    textAreaRect.offsetMin = new Vector2(5, 3); // Padding izquierdo/abajo
                    textAreaRect.offsetMax = new Vector2(-5, -3); // Padding derecho/arriba

                    // Creo GO hijo para el texto editable dentro del Text Area
                    GameObject textComponentGO = new GameObject("Text");
                    RectTransform textCompRect = textComponentGO.AddComponent<RectTransform>();
                    textCompRect.SetParent(textAreaRect, false);
                    // Configuro RectTransform del Text para llenar Text Area
                    textCompRect.anchorMin = Vector2.zero;
                    textCompRect.anchorMax = Vector2.one;
                    textCompRect.sizeDelta = Vector2.zero;

                    TextMeshProUGUI inputText = textComponentGO.AddComponent<TextMeshProUGUI>();
                    inputText.fontSize = 16;
                    inputText.color = Color.black;
                    inputText.alignment = TextAlignmentOptions.Left; // O según necesites

                    //Configuro TMP_InputField
                    tmpInputField.textViewport = textAreaRect; // Área visible
                    tmpInputField.textComponent = inputText;   // Componente de texto
                    tmpInputField.text = argDef.defaultValue ?? "";
                    // tmpInputField.contentType = ... /
                    // tmpInputField.onValueChanged.AddListener(...); 

                    // Añado el componente lógico FieldInputView
                    if (argDef.inputType == "number")
                        fieldView = fvGO.AddComponent<FieldNumberInputView>(); 
                    else
                        fieldView = fvGO.AddComponent<FieldTextInputView>(); 

                    //Añado LayoutElement para darle tamaño preferido al InputField
                    LayoutElement leInput = fvGO.AddComponent<LayoutElement>();
                    leInput.preferredWidth = 60; 
                    leInput.preferredHeight = 30;
                    leInput.flexibleWidth = 0;

                    break;

                case "field_dropdown":
                    // TODO: Crear la estructura para un TMP_Dropdown
                    // Añadir TMP_Dropdown
                    // Añadir Imagen de fondo
                    // Añadir Label para texto seleccionado
                    // Crear Template para opciones (ScrollRect, Viewport, Content, Item Template)
                    // Añadir FieldDropdownView (lógico)
                    // Añadir LayoutElement
                    Debug.LogWarning($"Dropdown creation not fully implemented for '{name}'");
                    // Placeholder visual temporal:
                    TextMeshProUGUI tmpDrop = fvGO.AddComponent<TextMeshProUGUI>();
                    tmpDrop.text = $"[ {argDef.name ?? "Dropdown"} ▼ ]";
                    tmpDrop.color = Color.blue; tmpDrop.fontSize = 16;
                    fieldView = fvGO.AddComponent<FieldLabelView>(); // Usar LabelView temporalmente
                    fvGO.AddComponent<LayoutElement>().preferredWidth = 100; // Darle tamaño
                    break;

                case "field_image":
                case "image": // Tratar 'image' como 'field_image'
                    // Obtengo o añado Image
                    Image img = fvGO.GetComponent<Image>();
                    if (img == null) img = fvGO.AddComponent<Image>();

                    // Cargo Sprite 
                    
                    Sprite sprite = Resources.Load<Sprite>($"Icons/{argDef.value}");
                    if (sprite != null)
                    {
                        img.sprite = sprite;
                        img.preserveAspect = true;
                        //Añado LayoutElement para tamaño
                        LayoutElement leImage = fvGO.AddComponent<LayoutElement>();
                        
                        leImage.preferredWidth = 30; 
                        leImage.preferredHeight = 30;
                    }
                    else
                    {
                        Debug.LogError($"Icon sprite not found: Icons/{argDef.value}");
                        // TODO Poner un placeholder o texto de error
                        TextMeshProUGUI tmpErr = fvGO.AddComponent<TextMeshProUGUI>();
                        tmpErr.text = "[IMG?]"; tmpErr.color = Color.red;
                    }
                    img.raycastTarget = false; 

                    //AñadO componente lógico FieldImageView
                    fieldView = fvGO.AddComponent<FieldImageView>();

                    break;
               

                default:
                    Debug.LogError($"BlockViewBuilder: Unsupported field type '{argDef.type}' for arg '{argDef.name ?? argDef.value}'");
                    //Label de error
                    TextMeshProUGUI tmpErrDef = fvGO.AddComponent<TextMeshProUGUI>();
                    tmpErrDef.text = $"[ERR: {argDef.type}]"; tmpErrDef.color = Color.red;
                    fieldView = fvGO.AddComponent<FieldLabelView>(); 
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating field view for '{argDef.type}' ({argDef.name ?? argDef.value}): {ex.Message}\n{ex.StackTrace}");
            if (fvGO != null) GameObject.Destroy(fvGO); // Limpiar si falla la creación
            return null;
        }

        //  Añadir a la Jerarquía Lógica 
        if (fieldView != null)
        {
            
            parentInputView.AddChildView(fieldView);
        }
        else
        {
            if (fvGO != null) GameObject.Destroy(fvGO);
            Debug.LogWarning($"FieldView component could not be added for type '{argDef.type}'. GameObject destroyed.");
        }

        return fieldView;
    }
    
    private static FieldView CreateFieldViewFromModel(InputView parentInputView, FieldModel fieldModel)
    {
        if (fieldModel == null) return null;
        string name = fieldModel.Name ?? $"Field_{parentInputView.ChildViews.Count}";
        GameObject fvGO = new GameObject($"Field_{fieldModel.GetFieldType()}_{name}"); // Usar tipo del modelo
        fvGO.transform.SetParent(parentInputView.ViewTransform, false);
        RectTransform rect = fvGO.AddComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = Vector2.zero;

        FieldView fieldView = null;
        switch (fieldModel.GetFieldType())
        {
            case "field_label": fieldView = AddComponentAndView<FieldLabelView, TextMeshProUGUI>(fvGO); break;
            case "field_number": fieldView = AddComponentAndView<FieldNumberInputView, TMP_InputField>(fvGO); break;
            case "field_input": fieldView = AddComponentAndView<FieldTextInputView, TMP_InputField>(fvGO); break;
            // TODO case "field_dropdown": fieldView = AddComponentAndView<FieldDropdownView, TMP_Dropdown>(fvGO); break; 
            // TODO case "field_image": fieldView = AddComponentAndView<FieldImageView, Image>(fvGO); break;
            // TODO otros 
            default: Debug.LogError($"Unsupported FieldModel type: {fieldModel.GetFieldType()}"); break;
        }
        if (fieldView != null)
        {
            parentInputView.AddChildView(fieldView);
          
            if (fieldView is FieldLabelView labelView)
                fvGO.GetComponent<TextMeshProUGUI>().text = fieldModel.GetValue();
        }
        else UnityEngine.Object.Destroy(fvGO); 
        return fieldView;
    }

    //Genérico
    private static TView AddComponentAndView<TView, TComp>(GameObject go) where TView : FieldView where TComp : Component
    {
        if (go.GetComponent<TComp>() == null) go.AddComponent<TComp>();
        return go.AddComponent<TView>();
    }

    // Crea la vista para la conexión dentro de un Input
    private static ConnectionInputView CreateConnectionInputView(InputView parentInputView, EConnection modelType)
    {
        string name = (modelType == EConnection.InputValue) ? "Connection_InputVal" : "Connection_InputStmt";
        GameObject cvGO = new GameObject(name);
        cvGO.transform.SetParent(parentInputView.ViewTransform, false);
        RectTransform rect = cvGO.AddComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = BlockViewSettings.InputConnectionSize; // Tamaño específico

        ConnectionInputView connectionView = cvGO.AddComponent<ConnectionInputView>();
        if (connectionView != null)
        {
            connectionView.InitializeConnectionType(modelType); 
            connectionView.Size = BlockViewSettings.InputConnectionSize;
        }
        parentInputView.AddChildView(connectionView);
        return connectionView;
    }

    private static void EnsureBackgroundImage(BlockView blockView, BlockDefinition definition)
    {
        Image img = blockView.GetComponent<Image>();
        if (img == null) img = blockView.gameObject.AddComponent<Image>();
        img.type = Image.Type.Sliced; 
        img.sprite = Resources.Load<Sprite>($"Textures/{definition.spriteName}"); 
        if (img.sprite == null) Debug.LogError($"Sprite not found: {definition.spriteName}");
        img.color = definition.color; 
                                      
    }

    private static GameObject CreateChildGO(GameObject parent, string name, params Type[] components)
    {
        GameObject go = new GameObject(name, components);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static void ConfigureRectFill(RectTransform rect)
    {
        if (rect == null) return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }
    private static void EnsureLayoutElement(BlockView blockView)
    {
        LayoutElement le = blockView.GetComponent<LayoutElement>();
        if (le == null) le = blockView.gameObject.AddComponent<LayoutElement>();
        
        le.ignoreLayout = false; 
        // le.preferredWidth = -1; 
        // le.preferredHeight = -1;
    }

    private static void ConfigureRectFillWithPadding(RectTransform rect, float left, float right, float top, float bottom)
    {
        if (rect == null) return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);   
        rect.offsetMax = new Vector2(-right, -top);   
                                                      
    }

    private static void CreateInputLinesAndContent(BlockView blockView, BlockDefinition definition)
    {
        LineGroupView currentLine = null;
        bool isSingleLine = definition.GetInputsInlineEffective();

        // Obtener la lista de InputModels pre-construida
        List<InputModel> inputModels = definition.CreateModelInputs(null); 
        if (inputModels == null || inputModels.Count == 0) return; 

        for (int i = 0; i < inputModels.Count; i++)
        {
            InputModel inputModel = inputModels[i];

            // Determinar la Línea
            if (currentLine == null || !isSingleLine)
            {
                currentLine = AddViewComponent<LineGroupView>(blockView.gameObject, $"LineGroup_{blockView.ChildViews.OfType<LineGroupView>().Count()}", blockView);
            }
            if (currentLine == null) { Debug.LogError("Failed to create/get LineGroupView!"); continue; }

            // CreO la InputView para este InputModel
            InputView inputView = AddViewComponent<InputView>(currentLine.gameObject, $"Input_{inputModel.Name ?? "Anon"}_{i}", currentLine);
            

            
            if (inputView != null)
            {
                // Creo FieldViews 
                foreach (FieldModel fieldModel in inputModel.FieldRow)
                {
                    CreateFieldViewFromModel(inputView, fieldModel);
                }

                // Creo ConnectionInputView si el modelo tiene conexión
                if (inputModel.Connection != null)
                {
                    
                    CreateConnectionInputViewHelper(inputView, inputModel.Connection.Type);
                }
            }
            else
            {
                Debug.LogError($"Failed to create InputView for {inputModel.Name}");
            }
        }
    }

    private static ConnectionInputView CreateConnectionInputViewHelper(InputView parentInputView, EConnection modelType)
    {
        string name = (modelType == EConnection.InputValue) ? "Connection_InputVal" : "Connection_InputStmt";
        
        ConnectionInputView connectionView = AddViewComponent<ConnectionInputView>(parentInputView.gameObject, name, parentInputView);
        if (connectionView != null)
        {
            if (connectionView != null)
            {
                connectionView.InitializeConnectionType(modelType); 
                connectionView.Size = BlockViewSettings.InputConnectionSize;
            }
           
          
        }
        return connectionView;
    }

    /** 
     * Descripción: Crea un GameObject (GO), le añade el componente de vista TView,
     * lo hace hijo del parentGO, y lo añade a la jerarquía BaseView del parentView.
     * @param: parentGO El GameObject padre al que se añadirá el nuevo GO.
     * @param: gameObjectName El nombre del nuevo GameObject.
     * @param: parentView La vista BaseView padre que gestionará la jerarquía.
     * @return: El componente de vista TView creado y añadido.
     */
    private static TView AddViewComponent<TView>(GameObject parentGO, string gameObjectName, BaseView parentView) where TView : BaseView
    {
        // Crear GO
        GameObject childGO = new GameObject(gameObjectName);

        // Hacer hijo en Transform
        childGO.transform.SetParent(parentGO.transform, false); // false: mantener posición local en 0

        // Añadir RectTransform 
        childGO.AddComponent<RectTransform>();

        //Añadir el componente de Vista (TView)
        TView viewComponent = childGO.AddComponent<TView>();

        //Añadir como hijo en la jerarquía BaseView del padre
        
        if (parentView != null)
        {
            parentView.AddChildView(viewComponent);
        }
        else
        {
            Debug.LogWarning($"AddViewComponent: parentView was null for {gameObjectName}");
        }

        return viewComponent;
    }
  
}//fin clase BlockViewBuiler