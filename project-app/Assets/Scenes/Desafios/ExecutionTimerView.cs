using UnityEngine;
using UnityEngine.UI; // o TMPro si usas TextMeshPro
using TMPro; // Asegúrate de tener este using si usas TextMeshPro

public class ExecutionTimerView : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText; 

    private void Awake()
    {
    
        if (timerText == null)
        {
            timerText = GetComponent<TMP_Text>();
        }
    }

    /// <summary>
    /// Actualiza el texto de la UI con el tiempo formateado.
    /// </summary>
    /// <param name="timeInSeconds">El tiempo total en segundos.</param>
    public void UpdateTimerDisplay(float timeInSeconds)
    {
        //  minutos, segundos y milisegundos
        int minutes = (int)timeInSeconds / 60;
        int seconds = (int)timeInSeconds % 60;
        int milliseconds = (int)((timeInSeconds * 100) % 100);

        // Formatear el string para que siempre tenga dos dígitos (ej. 01:05.09)
        timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }

    /// <summary>
    /// Resetea la pantalla del cronómetro a cero.
    /// </summary>
    public void ResetDisplay()
    {
        timerText.text = "00:00.00";
    }
}