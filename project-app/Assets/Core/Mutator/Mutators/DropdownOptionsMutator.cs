/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 04/04/2025
 * 
 * Versión: 2.0.0
 * 
 * Descripción:
 * 
 */

using System;
using System.Text;
using System.Xml;
using System.Linq;

/// <summary>
/// Mutator for dynamic modifying options for field dropdown
/// no mutation process, but record the real options to xml, and read them from xml as well.
/// </summary>
[MutatorClass(MutatorId = "dropdown_options_mutator")]
public class DropdownOptionsMutator : Mutator
{
    private const string OPTION_NAME = "options";

    public override bool NeedEditor
    {
        get { return false; }
    }

    public override XmlElement ToXml()
    {
        FieldDropdownModel dropdown = mBlock.GetField("MENU") as FieldDropdownModel;
        if (dropdown == null)
            throw new Exception("FieldDropDown \"MENU\" not found.");

        StringBuilder sb = new StringBuilder();
        foreach (FieldDropdownModel.FieldDropdownMenu option in dropdown.GetOptions())
        {
            // Simple CSV-like encoding; potential issues if Text or Value contain ',' or ';'
            // Consider more robust encoding (e.g., JSON array, XML elements) if this is likely.
            sb.AppendFormat("{0},{1};", option.Text.Replace(",", @"\,").Replace(";", @"\;"), // Basic escaping example
                                      option.Value.Replace(",", @"\,").Replace(";", @"\;"));
        }

        XmlElement xmlElement = XmlUtil.CreateDom("mutation");
        xmlElement.SetAttribute("options", sb.ToString());

        return xmlElement;
    }

    public override void FromXml(XmlElement xmlElement)
    {
        FieldDropdownModel dropdown = mBlock.GetField("MENU") as FieldDropdownModel;
        if (dropdown == null)
            throw new Exception("FieldDropDown \"MENU\" not found.");

        if (xmlElement.HasAttribute("options"))
        {
            string optionText = xmlElement.GetAttribute("options");
            string[] options = optionText.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (options.Length % 2 != 0)
                throw new Exception(string.Format("Xml serialization for mutation {0} is damaged", MutatorId));

            FieldDropdownModel.FieldDropdownMenu[] menu = new FieldDropdownModel.FieldDropdownMenu[options.Length / 2];
            for (int i = 0; i < menu.Length; i++)
            {
                menu[i].Text = options[i * 2];
                menu[i].Value = options[i * 2 + 1];
            }
            dropdown.SetOptions(menu.ToList());
        }
    }
}
