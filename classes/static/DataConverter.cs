using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public static class DataConverter 
{

    public static JsonValue ParseData(IntPtr data, int size)
    {
        byte[] managedArray = new byte[size];
        Marshal.Copy(data, managedArray, 0, size);
        string str = System.Text.Encoding.Default.GetString(managedArray);
        JsonValue result = new JsonValue();
        result.Parse(str);
        return result;
    }

    public static JsonValue ParseData(byte[] data)
    {
        string str = System.Text.Encoding.Default.GetString(data);
        JsonValue result = new JsonValue();
        result.Parse(str);
        return result;
    }

    /// <summary>
    /// Converts string to a byte [].
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static byte[] SealData(string obj)
    {
        byte[] result = Encoding.Default.GetBytes(obj);
        return result;

    }

}
