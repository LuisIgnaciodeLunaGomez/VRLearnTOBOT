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

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static FieldDropdownModel;


public class FieldDropdownDialog : FieldDialog
{
    [SerializeField] private GameObject m_ItemPrefab;
    [SerializeField] private Transform m_ItemsContainer;

    protected List<FieldDropdownMenu> mOptions;
    protected List<Toggle> mToggleItems;
    private string mOriginalValue; // Valor original del campo
    private FieldDropdownModel mFieldDropdown
    {
        get { return mField as FieldDropdownModel; }
    }

    protected override void OnInit()
    {
        if (mFieldDropdown == null)
        {
            Debug.LogError("FieldDropdownDialog: El Field asignado no es un FieldDropdownModel válido.");
            Destroy(gameObject); ; // Cierra si el field no es correcto
            return;
        }
        if (m_ItemPrefab == null)
        {
            Debug.LogError("FieldDropdownDialog: Falta asignar m_ItemPrefab.");
            Destroy(gameObject);
            return;
        }
        if (m_ItemsContainer == null)
        {
            Debug.LogError("FieldDropdownDialog: Falta asignar m_ItemsContainer (el padre de los items).");
            Destroy(gameObject);
            return;
        }

        // --- Configuración del ToggleGroup ---
        ToggleGroup toggleGroup = m_ItemsContainer.GetComponent<ToggleGroup>();
        if (toggleGroup == null)
        {
            Debug.LogWarning("FieldDropdownDialog: No se encontró ToggleGroup en m_ItemsContainer, añadiendo uno.");
            toggleGroup = m_ItemsContainer.gameObject.AddComponent<ToggleGroup>();
        }
        // Permitir deseleccionar si haces clic de nuevo en la opción seleccionada
        toggleGroup.allowSwitchOff = true;


        mOriginalValue = mFieldDropdown.GetValue(); // Guarda el valor actual
        mOptions = mFieldDropdown.GetOptions(); // Obtiene las opciones del modelo

        if (mOptions == null || mOptions.Count == 0)
        {
            Debug.LogWarning("FieldDropdownDialog: No se encontraron opciones para este dropdown.");
            // Podrías mostrar un mensaje al usuario aquí
        }


        mToggleItems = new List<Toggle>();

        // Ocultar el prefab original si es hijo del contenedor
        m_ItemPrefab.SetActive(false);

        for (int i = 0; i < mOptions.Count; i++)
        {
            FieldDropdownMenu option = mOptions[i];
            int currentIndex = i; // Captura el índice actual para el listener

            GameObject itemObj = GameObject.Instantiate(m_ItemPrefab, m_ItemsContainer, false);


            Text itemText = itemObj.GetComponentInChildren<Text>();
            if (itemText != null)
                itemText.text = option.Text;
            else
                Debug.LogWarning($"FieldDropdownDialog: Prefab del item no tiene componente Text en sus hijos para la opción '{option.Text}'.");

            Toggle toggle = itemObj.GetComponent<Toggle>();
            if (toggle == null)
            {
                Debug.LogError($"FieldDropdownDialog: Prefab del item no tiene componente Toggle para la opción '{option.Text}'. Destruyendo item.");
                Destroy(itemObj);
                continue; // Saltar esta opción
            }

            toggle.group = toggleGroup; // Asignar al grupo
            toggle.isOn = mOriginalValue == option.Value; // Seleccionar si el VALOR coincide

            // Listener para cuando cambia el estado del toggle
            // Podrías usar esto para una preselección visual si quisieras, pero el guardado es al cerrar.
            toggle.onValueChanged.AddListener((isSelected) => {
                if (isSelected)
                {
                    // Debug.Log($"Opción seleccionada (pero no guardada aún): {option.Text} / {option.Value}");
                }
            });

            mToggleItems.Add(toggle);
            itemObj.SetActive(true); // Mostrar el item instanciado
        }

        // Asegurar que el layout se reconstruya (si usas LayoutGroups)
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_ItemsContainer as RectTransform);


        // Evento al cerrar: Guarda el valor seleccionado
        AddCloseEvent(() =>
        {
            string selectedValue = mOriginalValue; // Por defecto, mantenemos el original si no se selecciona nada nuevo
            bool selectionFound = false;
            for (int i = 0; i < mToggleItems.Count; i++)
            {
                // Usamos try-catch porque el objeto Toggle podría haber sido destruido
                try
                {
                    if (mToggleItems[i] != null && mToggleItems[i].isOn)
                    {
                        selectedValue = mOptions[i].Value; // Obtenemos el VALOR asociado
                        selectionFound = true;
                        // Debug.Log($"Guardando valor: {mOptions[i].Text} ({selectedValue})");
                        break; // Solo puede haber una opción seleccionada con ToggleGroup
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"FieldDropdownDialog: Error al procesar el Toggle {i} al cerrar: {ex.Message}");
                }
            }

            // Si no se encontró ninguna selección Y el grupo no permite allowSwitchOff (o si quieres forzar selección)
            // podríamos revertir al valor original o seleccionar la primera opción, pero
            // con allowSwitchOff=true, si el usuario deselecciona todo, se debería guardar null o el valor por defecto.
            // De momento, si no hay nada seleccionado y se podía deseleccionar, guardamos el valor de la opción deseleccionada,
            // que podría ser el valor original si no se tocó nada, o el valor de la opción que fue activada y luego desactivada.
            // O MÁS SIMPLE: Si ninguna está ON, decide qué significa eso. ¿Revertir? ¿Vacío?
            // if (!selectionFound) { // Si ninguna está seleccionada al final...
            //selectedValue = mOriginalValue; // ...podríamos revertir al valor original
            // Debug.Log("Ninguna opción seleccionada. Revirtiendo al valor original: " + selectedValue);
            // O asignar un valor especial: selectedValue = null; o selectedValue = "";
            //}


            // Llama al método del modelo para actualizar el VALOR
            if (mFieldDropdown != null)
            {
                mFieldDropdown.SetValue(selectedValue); // Asume que FieldDropdownModel tiene SetValue
                                                        // O si solo tienes OnItemSelected(int index):
                                                        // int selectedIndex = -1;
                                                        // for(int i=0; i<mToggleItems.Count; ++i) if(mToggleItems[i].isOn) { selectedIndex = i; break; }
                                                        // if (selectedIndex != -1) mFieldDropdown.OnItemSelected(selectedIndex);
                                                        // else // Qué hacer si no hay índice seleccionado? Quizás llamar a OnItemSelected(-1) o similar
            }
            else
            {
                Debug.LogError("FieldDropdownDialog: mFieldDropdown es null al intentar guardar el valor.");
            }
        });
    }


    protected void Cleanup()
    {
        // Código para destruir los items creados y limpiar listeners si es necesario
        if (mToggleItems != null)
        {
            foreach (var toggle in mToggleItems)
            {
                if (toggle != null && toggle.gameObject != null && toggle.gameObject != m_ItemPrefab) // No destruir el prefab original
                {
                    Destroy(toggle.gameObject);
                }
            }
            mToggleItems.Clear();
        }
    }
}
