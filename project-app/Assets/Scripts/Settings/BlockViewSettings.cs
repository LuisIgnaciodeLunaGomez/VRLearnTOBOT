using System;

using UnityEngine;

public class BlockViewSettings: ScriptableObject
{
    private static BlockViewSettings m_Instance = null;

    [Tooltip("Basic block height, without margin.")]
    [SerializeField] public int BlockHeight = 60;
    [SerializeField] public int MinUnitWidth = 40;
    [SerializeField] public RectOffset ContentMargin;
    [SerializeField] public Vector2 ContentSpace;
    [SerializeField] public int ColorFieldWidth;

    [Tooltip("Whether block pattern views in toolbox have masks and are unable to receive input.")]
    [SerializeField] public bool MaskedInToolbox = true;
    [Tooltip("Maximum misalignment between connections for them to snap together.")]
    [SerializeField] public int ConnectSearchRange = 100;
    [Tooltip("The offset for bumpping away disconnected blocks ")]
    [SerializeField] public Vector2 BumpAwayOffset;
    [SerializeField] public Rect ValueConnectPointRect;
    [SerializeField] public Rect StatementConnectPointRect;

    [SerializeField] public GameObject PrefabRoot;
    [SerializeField] public GameObject PrefabRootOutput;
    [SerializeField] public GameObject PrefabRootPrev;
    [SerializeField] public GameObject PrefabRootNext;
    [SerializeField] public GameObject PrefabRootPrevNext;

    [SerializeField] public GameObject PrefabInputValue;
    [SerializeField] public GameObject PrefabInputValueSlot;
    [SerializeField] public GameObject PrefabInputStatement;

    [SerializeField] public GameObject PrefabFieldLabel;
    [SerializeField] public GameObject PrefabFieldInput;
    [SerializeField] public GameObject PrefabFieldImage;
    [SerializeField] public GameObject PrefabFieldButton;
    [SerializeField] public GameObject PrefabFieldVariable;
    [SerializeField] public GameObject PrefabFieldCheckbox;

    [SerializeField] public GameObject PrefabBtnCreateVar;
    [SerializeField] public GameObject PrefabConnectHighlight;
    [SerializeField] public GameObject PrefabStatusLight;

    public float ContentHeight
    {
        get { return BlockHeight; }
    }


    public static BlockViewSettings Get()
    {
        if (m_Instance == null)
        {
            m_Instance = Resources.Load<BlockViewSettings>("BlockViewSettings");

            if (m_Instance == null)
            {
                Debug.LogWarning("No se encontró 'BlockViewSettings.asset'. Se usará una instancia predeterminada.");
                m_Instance = ScriptableObject.CreateInstance<BlockViewSettings>(); // Crear objeto predeterminado.
            }
        }

        return m_Instance;
    }

    public static void Dispose()
    {
        m_Instance = null;
    }
}


