/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 02/04/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */

using UnityEngine;
using UnityEngine.UI;


public class ItemListMutatorDialog : BaseDialog
{
    [SerializeField] private Slider m_ItemCountSlider;
    [SerializeField] private Text m_ItemCountText;
    [SerializeField] private Text m_ItemCountTitle;

    private ItemListMutator mItemListMutator
    {
        get { return mBlock.Mutator as ItemListMutator; }
    }

    protected override void OnInit()
    {
        m_ItemCountSlider.value = mItemListMutator.ItemCount;
        m_ItemCountText.text = mItemListMutator.ItemCount.ToString();

        m_ItemCountTitle.text = I18n.Get(MsgDefine.TEXT_CREATE_JOIN_ITEM_TITLE_ITEM);

        AddCloseEvent(() =>
        {
            mItemListMutator.Mutate((int)m_ItemCountSlider.value);
        });

        m_ItemCountSlider.onValueChanged.AddListener((value) =>
        {
            m_ItemCountText.text = ((int)value).ToString();
        });
    }
}
