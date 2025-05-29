/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 26/05/2025
 * 
 * Versión: 1.0.
 * 
 * Descripción: 
 */

using UnityEngine;
using TMPro;
using System.Collections; 
public class BlockObserver : MonoBehaviour
{
    public static BlockObserver Instance { get; private set; }

    [SerializeField] private GameObject m_RobotObject;
    [SerializeField] private TextMeshProUGUI m_StatusText;

    private ExecutionController m_ExecutionControllerRef; //Subcripción a los eventos de ExecutionController

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
             DontDestroyOnLoad(gameObject);
            Logger.Log("<color=cyan>[BlockObserver] Singleton Instance created.</color>");
        }
        else if (Instance != this)
        {
            Logger.LogWarning("[BlockObserver] Duplicate BlockObserver instance detected. Destroying myself.");
            Destroy(gameObject);
           // return;
        }
    }

   

    void OnEnable()
    {
         Debug.Log("[BlockObserver] OnEnable called. No auto-subscription here.");
       // TrySubscribeToExecutionController();
        // Buscar el ExecutionController en la escena para suscribirse a sus eventos.
        /* m_ExecutionControllerRef = ExecutionController.Instance;
         if (m_ExecutionControllerRef != null)
         {
             m_ExecutionControllerRef.OnExecutionStartBlock += HandleExecutionStartBlock;
             m_ExecutionControllerRef.OnExecutionFinishBlock += HandleExecutionFinishBlock;
             m_ExecutionControllerRef.OnExecutionStart += HandleExecutionStart;
             m_ExecutionControllerRef.OnExecutionFinish += HandleExecutionFinish;
             m_ExecutionControllerRef.OnExecutionStop += HandleExecutionStop;
             m_ExecutionControllerRef.OnExecutionError += HandleExecutionError;
             Debug.Log("<color=cyan>[BlockObserver] Subscribed to ExecutionController events.</color>");
         }
         else
         {
             Debug.LogError("[BlockObserver] ExecutionController not found on OnEnable. Cannot subscribe. Ensure it's initialized first.");
         }*/
    }

    void OnDisable()
    {
        // Desuscribirse para evitar Memory Leaks
        if (m_ExecutionControllerRef != null)
        {
            m_ExecutionControllerRef.OnExecutionStartBlock -= HandleExecutionStartBlock;
            m_ExecutionControllerRef.OnExecutionFinishBlock -= HandleExecutionFinishBlock;
            m_ExecutionControllerRef.OnExecutionStart -= HandleExecutionStart;
            m_ExecutionControllerRef.OnExecutionFinish -= HandleExecutionFinish;
            m_ExecutionControllerRef.OnExecutionStop -= HandleExecutionStop;
            m_ExecutionControllerRef.OnExecutionError -= HandleExecutionError;
            Debug.Log("<color=cyan>[BlockObserver] Unsubscribed from ExecutionController events.</color>");
        }
    }

    /// <summary>
    /// Este método se llamará desde ExecutionController.InitializeController()
    /// Puede que no necesite WorkSpaceModel aquí directamente si los eventos de ExecutionController ya dan todo.
    /// Si necesita resetear el robot en un punto especifico:
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"<color=cyan>[BlockObserver] Initialized. Robot object reset.</color>");
        if (m_RobotObject != null)
        {
            m_RobotObject.transform.position = Vector3.zero;
            m_RobotObject.transform.rotation = Quaternion.identity;
        }
        if (m_StatusText != null)
        {
            m_StatusText.text = "System Ready. Waiting for program.";
        }
    }
    /// <summary>
    /// MÉTODO PÚBLICO para que AppController pueda solicitar la suscripción explícitamente.
    /// Se llamará cuando AppController sepa que ExecutionController está listo.
    /// </summary>
    public void SubscribeToExecutionController(ExecutionController executionController)
    {
        if (m_ExecutionControllerRef != null)
        {
            if (m_ExecutionControllerRef == executionController)
            {
                Logger.LogWarning("[BlockObserver.Subscribe] Already subscribed to this ExecutionController.");
                return;
            }
            else
            {
                // Si se pasa una nueva instancia, desuscribir de la vieja.
                OnDisable();
            }
        }

        if (executionController != null)
        {
            m_ExecutionControllerRef = executionController;
            m_ExecutionControllerRef.OnExecutionStartBlock += HandleExecutionStartBlock;
            m_ExecutionControllerRef.OnExecutionFinishBlock += HandleExecutionFinishBlock;
            m_ExecutionControllerRef.OnExecutionStart += HandleExecutionStart;
            m_ExecutionControllerRef.OnExecutionFinish += HandleExecutionFinish;
            m_ExecutionControllerRef.OnExecutionStop += HandleExecutionStop;
            m_ExecutionControllerRef.OnExecutionError += HandleExecutionError;

            Logger.Log("<color=green>[BlockObserver] SUCCESSFULLY SUBSCRIBED to ExecutionController events.</color>");
        }
        else
        {
            // Este error indica que AppController falló en pasar la instancia.
            Logger.LogError("[BlockObserver.Subscribe] ExecutionController instance passed was NULL! Cannot subscribe. Check AppController.Start().");
        }
    }


    // Manejadores para los eventos de ExecutionController
    public void HandleExecutionStart()
    {
        Debug.Log("---------- EXECUTION STARTED (via BlockObserver) ----------");
        if (m_StatusText != null) m_StatusText.text = "Executing...";
        if (m_RobotObject != null)
        {
            m_RobotObject.transform.position = Vector3.zero;
            m_RobotObject.transform.rotation = Quaternion.identity;
        }
    }

    public void HandleExecutionFinish()
    {
        Debug.Log("---------- EXECUTION FINISHED (via BlockObserver) ----------");
        if (m_StatusText != null) m_StatusText.text = "Execution Finished!";
    }

    public void HandleExecutionStop()
    {
        Debug.Log("---------- EXECUTION STOPPED (via BlockObserver) ----------");
        if (m_StatusText != null) m_StatusText.text = "Execution Stopped.";
    }

    public void HandleExecutionStartBlock(BlockModel block)
    {
        Debug.Log($"<color=yellow>BlockObserver: Starting processing of block: {block?.Type} (ID: {block?.ID})</color>");
        if (m_StatusText != null) m_StatusText.text = $"Running: {block?.Type}...";

    }

    public void HandleExecutionFinishBlock(BlockModel block)
    {
        Debug.Log($"<color=gray>BlockObserver: Finished Block: {block?.Type}</color>");
        // Si hay efectos de finalización de bloque 
    }

    public void HandleExecutionError(BlockModel block, string msg)
    {
        Debug.LogError($"BlockObserver: Execution ERROR in block {block?.Type}: {msg}");
        if (m_StatusText != null) m_StatusText.text = $"Error in block {block?.Type}: {msg}";
    }

    
    public IEnumerator MoveRobot(float steps)
    {
        if (m_RobotObject == null)
        {
            Debug.LogError("BlockObserver: Robot Object is not assigned to move!");
            yield break;
        }

        Vector3 startPosition = m_RobotObject.transform.position;
        // robot mira hacia adelante (Z+ o X+) y queremos moverlo en su dirección local.
        float unitsPerStep = 0.1f; //  1 paso de Scratch = 0.1 unidades de Unity (1 metro)
        Vector3 targetPosition = m_RobotObject.transform.position + m_RobotObject.transform.forward * steps * unitsPerStep;

        float duration = 0.5f; // Duración de la animación en segundos
        float elapsed = 0f;

        while (elapsed < duration)
        {
            m_RobotObject.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        m_RobotObject.transform.position = targetPosition; // Asegurarse de que termina en la posición exacta
        Debug.Log($"Robot moved {steps} steps.");
    }

    /// <summary>
    /// Método principal de inicialización y suscripción.
    /// Este método DEBE ser llamado desde AppController DESPUÉS de que ExecutionController.InitializeController() haya terminado.
    /// </summary>
    public void InitializeAndSubscribe(ExecutionController executionController)
    {
        if (executionController == null)
        {
            Debug.LogError("[BlockObserver] InitializeAndSubscribe: Passed ExecutionController is null. Cannot subscribe.");
            return;
        }

        // Si ya estamos suscritos a la misma instancia, no hacer nada
        if (m_ExecutionControllerRef == executionController && m_ExecutionControllerRef != null)
        {
            Debug.LogWarning("[BlockObserver] Already subscribed to this ExecutionController instance. Skipping re-subscription.");
            return;
        }

        // Si m_ExecutionControllerRef apunta a otra instancia, desuscribirse primero para evitar duplicados.
        if (m_ExecutionControllerRef != null && m_ExecutionControllerRef != executionController)
        {
            Debug.LogWarning("[BlockObserver] Subscribing to a new ExecutionController instance. Unsubscribing from old one first.");
            OnDisable(); // Llamar OnDisable para desuscribir del viejo.
        }

        m_ExecutionControllerRef = executionController; // Almacena la referencia.

        // Ahora nos suscribimos a los eventos del ExecutionController de forma segura.
        m_ExecutionControllerRef.OnExecutionStartBlock += HandleExecutionStartBlock;
        m_ExecutionControllerRef.OnExecutionFinishBlock += HandleExecutionFinishBlock;
        m_ExecutionControllerRef.OnExecutionStart += HandleExecutionStart;
        m_ExecutionControllerRef.OnExecutionFinish += HandleExecutionFinish;
        m_ExecutionControllerRef.OnExecutionStop += HandleExecutionStop;
        m_ExecutionControllerRef.OnExecutionError += HandleExecutionError;

        Debug.Log("<color=cyan>[BlockObserver] Subscribed to ExecutionController events via InitializeAndSubscribe().</color>");

        // Reinicia la posición del robot y el texto de estado.
        if (m_RobotObject != null)
        {
            m_RobotObject.transform.position = Vector3.zero;
            m_RobotObject.transform.rotation = Quaternion.identity;
        }
        if (m_StatusText != null)
        {
            m_StatusText.text = "System Ready. Waiting for program.";
        }
        Debug.Log($"<color=cyan>[BlockObserver] Robot and Status Text Initialized/Reset.</color>");
    }


    //Métodos del robot

    public IEnumerator _MoveRobot(float distance)
    {
        if (m_RobotObject == null)
        {
            Debug.LogError("MoveRobot: m_RobotObject is null!");
            yield break;
        }

        float animationDuration = 0.5f; 
        Vector3 startPos = m_RobotObject.transform.position;
        float unitsPerStep = 0.1f; // Conversión de pasos Scratch a unidades Unity.
        Vector3 endPos = startPos + m_RobotObject.transform.forward * distance * unitsPerStep;

        float timer = 0f;
        while (timer < animationDuration)
        {
            m_RobotObject.transform.position = Vector3.Lerp(startPos, endPos, timer / animationDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        m_RobotObject.transform.position = endPos; // Asegura que llega al destino exacto.
        Debug.Log($"<color=blue>Robot moved {distance} steps!</color>");
    }

    public IEnumerator TurnRobot(float degrees)
    {
        if (m_RobotObject == null) { Debug.LogError("TurnRobot: m_RobotObject is null!"); yield break; }

        float animationDuration = 0.3f; // Duración de la rotación.
        Quaternion startRot = m_RobotObject.transform.rotation;
        // La rotación en Unity se hace alrededor del eje Y para "girar"
        Quaternion endRot = startRot * Quaternion.Euler(0, degrees, 0);

        float timer = 0f;
        while (timer < animationDuration)
        {
            m_RobotObject.transform.rotation = Quaternion.Lerp(startRot, endRot, timer / animationDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        m_RobotObject.transform.rotation = endRot;
        Debug.Log($"<color=blue>Robot turned {degrees} degrees!</color>");
    }

    public IEnumerator WaitRobot(float seconds)
    {
        Debug.Log($"<color=blue>Robot waiting for {seconds} seconds...</color>");
        yield return new WaitForSeconds(seconds);
        Debug.Log($"<color=blue>Robot finished waiting.</color>");
    }
}//Fin clase BlockObserver