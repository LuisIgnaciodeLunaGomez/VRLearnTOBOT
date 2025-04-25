/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 28/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción:
 */

public abstract class FieldDialog : BaseDialog
{
    protected FieldModel mField;
    public FieldModel Field { get { return mField; } }

    public void Init(FieldModel field)
    {
        mField = field;
        mBlock = field.SourceBlock;
        Init();
    }

}
