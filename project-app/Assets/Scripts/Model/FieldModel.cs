/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 27/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */
using System.Reflection;
using System;

public delegate string FieldValidator(string text);

public abstract class FieldModel : Observable<string>
{
    protected FieldModel(string fieldName)
    {
        Name = fieldName;
    }

    public virtual bool IsEditable => false;
    public string Name { get; protected set; }

    protected string mText;
    public bool IsImage { get; protected set; }

    public BlockModel SourceBlock { get; internal set; }

    protected FieldValidator mValidator;

    public FieldModel PrefixField;

    public FieldModel SuffixField;

    private string mType = null;
    
    public string Type
    {
        get
        {
            if (string.IsNullOrEmpty(mType))
            {
                Type classType = this.GetType();
                MethodInfo methodInfo = classType.GetMethod("CreateFromJson", BindingFlags.Static | BindingFlags.NonPublic);
                if (methodInfo == null)
                    throw new Exception(string.Format(
                        "There is no static function \"CreateFromJson\" for creating field in class {0}. Please add one", classType));

                var attrs = methodInfo.GetCustomAttributes(typeof(FieldCreatorAttribute), false);
                if (attrs.Length == 0)
                    throw new Exception(string.Format(
                        "You should add a \"FieldCreatorAttribute\" to static method \"CreateFromJson\" in class {0}.", classType));
                mType = ((FieldCreatorAttribute)attrs[0]).FieldType;
            }
            return mType;
        }
    }

    public virtual void SetSourceBlock(BlockModel block)
    {
        if (SourceBlock == block)
            return;
        if (SourceBlock != null)
            throw new Exception("Field already bound to a block, can't bound to another block");
        this.SourceBlock = block;
    }
   
    public void SetValidator(FieldValidator handler)
    {
        this.mValidator = handler;
    }
    public FieldValidator GetValidator()
    {
        return this.mValidator;
    }

    protected virtual string ClassValidator(string text)
    {
        return text;
    }

    public string CallValidator(string text)
    {
        string classResult = ClassValidator(text);
        if (classResult == null)
            return null;

        text = classResult;

        FieldValidator userValidator = GetValidator();
        if (userValidator != null)
        {
            string userResult = userValidator(text);
            if (userResult == null)
            {
                return null;
            }
            text = userResult;
        }

        return text;
    }
    public virtual string GetText()
    {
        return mText;
    }
    public virtual void SetText(string newText)
    {
        if (string.IsNullOrEmpty(newText) || string.Equals(newText, mText))
        {
            return;
        }

        mText = newText;
        FireUpdate(mText);
    }

    public virtual string GetValue()
    {
        return GetText();
    }
    public virtual void SetValue(string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
        {
            return;
        }

        var oldValue = this.GetValue();
        if (string.Equals(oldValue, newValue))
            return;

        this.SetText(newValue);
    }
 
    public virtual void Dispose()
    {
        this.SourceBlock = null;
        this.mValidator = null;
    }
}//Fin clase FieldModels

