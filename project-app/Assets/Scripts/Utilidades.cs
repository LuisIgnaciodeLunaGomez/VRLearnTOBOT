using System.Collections.Generic;
using UnityEngine;

public class Utilidades
{
    private static readonly string SOUP = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenUid()
    {
        var id = new List<char>();
        for (int i = 0; i < 20; i++)
        {
            id.Add(SOUP[UnityEngine.Random.Range(0, SOUP.Length)]);
        }
        return new string(id.ToArray());
    }
}
