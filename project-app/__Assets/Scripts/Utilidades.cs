/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 30/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 * 
 */


using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public static class Utilidades
{
    public static char[] SOUP = "!#$%()*+,-./:;=?@[]^_`{|}~ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

    public static string GenUid()
    {
        var id = new List<char>();
        for (int i = 0; i < 20; i++)
        {
            id.Add(SOUP[Random.Range(0, SOUP.Length)]);
        }
        return new string(id.ToArray());
    }

    /**
     * Descripción: Obtiene un componente del GameObject. Si no existe, lo añade.
     * @param: go: GameObject al que se le añadirá el componente
     * return: T: Componente añadido
     * */
    public static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        if (go == null) return null;
        T component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }
        return component;
    }

    public static bool isSymbol(string word)
    {
        //char dd = Convert.ToChar(word);
        Regex rx = new Regex("^[\u4e00-\u9fa5]$");
        if (rx.IsMatch(word))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public static string StringSplit(string src)
    {
        string res = "";
        for (int i = 0; i < src.Length; ++i)
        {
            if (!isSymbol(src[i].ToString()))
            {
                res += src[i];
            }
        }
        return res;
    }

   

    /// <summary>
    /// Replaces string table references in a message , if the message is a string.
    /// For example,"%{bky_my_msg}" and "%{BKY_MY_MSG}" will both be replaced with
    /// the value in I18n.Msg['MY_MSG'].
    /// </summary>
   /* public static string ReplaceMessageReferences(JObject message)
    {
        if (!message.IsString())
            return message.ToString();

        return ReplaceMessageReferences(message.ToString());
    }*/

    /// <summary>
    /// Replaces string table references in a message , if the message is a string.
    /// For example,"%{bky_my_msg}" and "%{BKY_MY_MSG}" will both be replaced with
    /// the value in I18n.Msg['MY_MSG'].
    /// </summary>
    public static string ReplaceMessageReferences(string message)
    {
        var interpolatedResult = TokenizeInterpolation(message, false);
       
        return interpolatedResult.Count > 0 ? interpolatedResult[0] : "";
    }

    /// <summary>
    /// Internal implemention of the message reference and interpolation token
    /// Parsing used by tokenizeInterpolation() and replaceMessageReferences().
    /// </summary>
    public static List<string> TokenizeInterpolation(JToken message, bool parseInterpolationTokens = true)
    {
        return Utilidades.TokenizeInterpolation(message == null ? "" : message.ToString(), parseInterpolationTokens);
    }

    /// <summary>
    /// Internal implemention of the message reference and interpolation token
    /// Parsing used by tokenizeInterpolation() and replaceMessageReferences().
    /// </summary>
    public static List<string> TokenizeInterpolation(string message, bool parseInterpolationTokens = true)
    {
        var tokens = new List<string>();
        var chars = message.ToCharArray().ToList();
       
        var state = 0;
        var buffer = new List<string>();
        string number = "";
        for (var i = 0; i < chars.Count + 1; i++)
        {
            var c = i == chars.Count ? ' ' : chars[i];
            if (state == 0 && i != chars.Count)
            {
                if (c == '%')
                {
                    var text = string.Join("", buffer.ToArray());
                    if (!string.IsNullOrEmpty(text))
                    {
                        tokens.Add(text);
                    }
                    buffer.Clear();
                    state = 1; 
                }
                else
                {
                    buffer.Add(c.ToString());
                }
            }
            else if (state == 1)
            {
                if (c == '%')
                {
                    buffer.Add(c.ToString()); 
                    state = 0;
                }
                else if (parseInterpolationTokens && '0' <= c && c <= '9')
                {
                    state = 2;
                    number = c.ToString();
                    buffer.Add("");
                    var text = string.Join("", buffer.ToArray());
                    if (!string.IsNullOrEmpty(text))
                    {
                        tokens.Add(text);
                    }
                    buffer.Clear();
                }
                else if (c == '{')
                {
                    state = 3;
                }
                else
                {
                    buffer.Add("%"); 
                    if (i != chars.Count)
                        buffer.Add(c.ToString());
                    state = 0;
                }
            }
            else if (state == 2)
            {
                if ('0' <= c && c <= '9')
                {
                    number += c; 
                }
                else
                {
                    tokens.Add(int.Parse(number).ToString());
                    i--; 
                    state = 0;
                }
            }
            else if (state == 3)
            {
                if (i == chars.Count)
                {
                    
                    buffer.Insert(0, "%{");
                    i--; 
                    state = 0; 
                }
                else if (c != '}')
                {
                    buffer.Add(c.ToString());
                }
                else
                {
                    var rawKey = string.Join("", buffer.ToArray());
                    if (new Regex("[a-zA-Z][a-zAZ0-9_]").Match(rawKey).Success) 
                    {
                        var keyUpper = rawKey.ToUpper();

                           var bklyKey = keyUpper.StartsWith("BKY_") ? keyUpper.Substring(4) : null;
                        if (!string.IsNullOrEmpty(bklyKey) && I18n.Contains(bklyKey))
                        {
                            var rawValue = I18n.Get(bklyKey);
                            tokens.AddRange(TokenizeInterpolation(rawValue));
                        }
                        else
                        {
                            tokens.Add("%{" + rawKey + "}");
                        }

                        buffer.Clear(); 
                        state = 0;
                    }
                    else
                    {
                        tokens.Add("%{" + rawKey + "}");
                        buffer.Clear();
                        state = 0;        
                    }
                }
            }
        }

        var text1 = string.Join("", buffer.ToArray());
        if (!string.IsNullOrEmpty(text1))
        {
            tokens.Add(text1);
        }

        var mergedTokens = new List<string>();
        buffer.Clear();
        for (int i = 0; i < tokens.Count; i++)
        {
            int tokenNum = 0;

            if (int.TryParse(tokens[i], out tokenNum))
            {
                text1 = string.Join("", buffer.ToArray());
                if (!string.IsNullOrEmpty(text1))
                {
                    mergedTokens.Add(text1);
                }
                buffer.Clear();
                mergedTokens.Add(tokens[i]);
            }
            else
            {
                buffer.Add(tokens[i]);
            }
        }

        text1 = string.Join("", buffer.ToArray());

        if (!string.IsNullOrEmpty(text1))
        {
            mergedTokens.Add(text1);
        }
        buffer.Clear();

        return mergedTokens;
    }


#if UNITY_EDITOR
    private static bool mGenUidValueDirty = false;
    private static string mEditorDefaultGenUidValue = string.Empty;
    public static string EditorDefaultGenUidValue
    {
        set
        {
            mEditorDefaultGenUidValue = value;
            mGenUidValueDirty = true;
        }
    }

    public static void ResetGenUidValueDirty2False()
    {
        mGenUidValueDirty = false;
    }
#endif


    /// <summary>
    /// Given an array of strings, return the length of the shortest one.
    /// </summary>
    public static int ShortestStringLength(string[] strArray)
    {
        if (strArray == null || strArray.Length == 0)
            return 0;
        int minLength = int.MaxValue;
        for (int i = 0; i < strArray.Length; i++)
        {
            if (strArray[i].Length < minLength)
                minLength = strArray[i].Length;
        }
        return minLength;
    }

    /// <summary>
    /// Given an array of strings, return the length of the common prefix.
    /// Words may not be split.  Any space after a word is included in the length.
    /// </summary>
    public static int CommonWordPrefix(string[] strArray)
    {
        if (strArray == null || strArray.Length == 0)
            return 0;
        if (strArray.Length == 1)
            return strArray[0].Length;

        int maxLength = ShortestStringLength(strArray);
        int len = 0;
        int wordPrefix = 0;
        for (len = 0; len < maxLength; len++)
        {
            char letter = strArray[0][len];
            for (int i = 1; i < strArray.Length; i++)
            {
                if (letter != strArray[i][len])
                    return wordPrefix;
            }

            if (letter == ' ')
                wordPrefix = len + 1;
        }

        for (int i = 1; i < strArray.Length; i++)
        {
            if (strArray[i].Length > len)
            {
                char letter = strArray[i][len];
                if (letter != ' ')
                    return wordPrefix;
            }
        }
        return maxLength;
    }

    /// <summary>
    /// Given an array of strings, return the length of the common suffix. 
    /// Words may not be split.  Any space after a word is included in the length.
    /// </summary>
    public static int CommonWordSuffix(string[] strArray)
    {
        if (strArray == null || strArray.Length == 0)
            return 0;
        if (strArray.Length == 1)
            return strArray[0].Length;

        int maxLength = ShortestStringLength(strArray);
        int len = 0;
        int wordSuffix = 0;
        for (len = 0; len < maxLength; len++)
        {
            char letter = strArray[0][strArray[0].Length - len - 1];
            for (int i = 1; i < strArray.Length; i++)
            {
                if (letter != strArray[i][strArray[i].Length - len - 1])
                    return wordSuffix;
            }
            if (letter == ' ')
                wordSuffix = len + 1;
        }

        for (int i = 1; i < strArray.Length; i++)
        {
            if (strArray[i].Length > len)
            {
                char letter = strArray[i][strArray[i].Length - len - 1];
                if (letter != ' ')
                    return wordSuffix;
            }
        }
        return maxLength;
    }

    public static Vector2 Screen2WorkspacePos(WorkSpaceModel workspace, RectTransform codingAreaRect, Vector2 screenPos, Canvas canvas)
    {
        if (canvas == null || codingAreaRect == null || workspace == null)
        {
            Debug.LogError("Screen2WorkspacePos: Invalid parameters.");
            return Vector2.zero;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(codingAreaRect, screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out localPoint);

       
        Vector2 logicalPosition = localPoint; 

      

        return logicalPosition; 
    }
}//Fin clase utilidades
