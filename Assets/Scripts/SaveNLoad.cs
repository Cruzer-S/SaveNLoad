using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class CanSaveNLoad : Attribute
{
    public string PathName { get; private set; }

    public CanSaveNLoad(string PathName) => this.PathName = PathName;
}

public static partial class SaveNLoad
{
    private static string ListFormat = "{0}#{1}";
    private static string ListCount = ":Count";

    public static bool Save<T>(T data, string name) => Save(data.GetType(), data, name);

    public static T Load<T>(string name) => (T)Load(typeof(T), name);

    public static void Delete<T>(string name) => Delete(typeof(T), name);
}

public static partial class SaveNLoad
{
    private static bool Save(Type type, object data, string name, string basename = null)
    {
        FieldInfo[] fields;
        CanSaveNLoad attribute;

        attribute = type.GetCustomAttribute<CanSaveNLoad>();
        if (attribute == null)
            return false;

        fields = type.GetFields();
        foreach (FieldInfo field in fields)
        {
            string key;
            if (basename == null)
                key = Path.Combine(attribute.PathName, name, field.Name);
            else
                key = Path.Combine(basename, attribute.PathName, name, field.Name);

            object value = field.GetValue(data);

            if (field.FieldType.GetCustomAttribute(typeof(CanSaveNLoad)) != null)
                Save(field.FieldType, value, string.Empty, Path.Combine(attribute.PathName, name));
            else if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(List<>)))
                SaveListType(field.FieldType, key, value, ListFormat);
            else
                SaveBasicType(field.FieldType, key, value);
        }

        return true;
    }

    private static void SaveBasicType(Type type, string key, object value)
    {
        if (type == typeof(int))
            PlayerPrefs.SetInt(key, (int)value);
        else if (type == typeof(float))
            PlayerPrefs.SetFloat(key, (float)value);
        else if (type == typeof(string))
            PlayerPrefs.SetString(key, (string)value);
        else if (type == typeof(bool))
            PlayerPrefs.SetInt(key, ((bool)value) ? 1 : 0);
        else if (type == typeof(char))
            PlayerPrefs.SetInt(key, (char)value);
    }

    private static void SaveListType(Type type, string key, object values, string format)
    {
        Type elementType = type.GetGenericArguments().Single();
        IList list = values as IList;

        PlayerPrefs.SetInt(key + ListCount, list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            string formattedKey = string.Format(format, key, i);

            if (elementType.GetCustomAttribute(typeof(CanSaveNLoad)) != null)
                Save(elementType, list[i], string.Empty, formattedKey);
            else
                SaveBasicType(elementType, formattedKey, list[i]);
        }
    }
}

public static partial class SaveNLoad
{
    private static object Load(Type type, string name, string basename = null)
    {
        FieldInfo[] fields = type.GetFields();
        CanSaveNLoad attribute;

        attribute = type.GetCustomAttribute<CanSaveNLoad>();
        if (attribute == null)
            return null;

        object @object = Activator.CreateInstance(type);
        if (@object == null)
            return null;

        foreach (FieldInfo field in fields)
        {
            string key;
            if (basename == null)
                key = Path.Combine(attribute.PathName, name, field.Name);
            else
                key = Path.Combine(basename, attribute.PathName, name, field.Name);

            object value = null;

            if (field.FieldType.GetCustomAttribute(typeof(CanSaveNLoad)) != null)
                value = Load(field.FieldType, string.Empty, Path.Combine(attribute.PathName, name));
            else if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(List<>)))
                value = LoadListType(field.FieldType, key, ListFormat);
            else
                value = LoadBasicType(field.FieldType, key);

            if (value == null)
                return null;

            field.SetValue(@object, value);
        }

        return @object;
    }

    private static object LoadBasicType(Type type, string key)
    {
        object value = null;

        if (!PlayerPrefs.HasKey(key))
            return null;

        if (type == typeof(int))
            value = PlayerPrefs.GetInt(key);
        else if (type == typeof(float))
            value = PlayerPrefs.GetFloat(key);
        else if (type == typeof(string))
            value = PlayerPrefs.GetString(key);
        else if (type == typeof(bool))
            value = PlayerPrefs.GetInt(key) != 0 ? true : false;
        else if (type == typeof(char))
            value = (char)PlayerPrefs.GetInt(key);

        return value;
    }

    private static object LoadListType(Type type, string key, string format)
    {
        Type elementType = type.GetGenericArguments().Single();
        IList list = (IList)Activator.CreateInstance(type);
        string countKey = key + ListCount;
        int count;

        if (!PlayerPrefs.HasKey(countKey))
            return null;

        count = PlayerPrefs.GetInt(countKey);

        for (int i = 0; i < count; i++) {
            object data = null;

            if (elementType.GetCustomAttribute(typeof(CanSaveNLoad)) != null)
                data = Load(elementType, string.Empty, string.Format(format, key, i));
            else if (PlayerPrefs.HasKey(string.Format(format, key, i)))
                data = LoadBasicType(elementType, string.Format(format, key, i));

            if (data == null)
                return null;

            list.Add(data);
        }

        return list;
    }
}

public static partial class SaveNLoad
{
    private static void Delete(Type type, string name, string basename = null)
    {
        FieldInfo[] fields;
        CanSaveNLoad attribute;

        attribute = type.GetCustomAttribute<CanSaveNLoad>();
        if (attribute == null)
            return;

        fields = type.GetFields();
        foreach (FieldInfo field in fields)
        {
            string key;
            if (basename == null)
                key = Path.Combine(attribute.PathName, name, field.Name);
            else
                key = Path.Combine(basename, attribute.PathName, name, field.Name);

            if (field.FieldType.GetCustomAttribute(typeof(CanSaveNLoad)) != null)
                Delete(field.FieldType, string.Empty, Path.Combine(attribute.PathName, name));
            else if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(List<>)))
                DeleteListType(field.FieldType, key, ListFormat);
            else if (PlayerPrefs.HasKey(key))
                PlayerPrefs.DeleteKey(key);
        }
    }

    private static void DeleteListType(Type type, string key, string format)
    {
        Type elementType = type.GetGenericArguments().Single();
        string countKey = key + ListCount;
        int count;
        
        if ( !PlayerPrefs.HasKey(countKey) )
            return ;

        count = PlayerPrefs.GetInt(countKey);
        for (int i = 0; i < count; i++)
        {
            if (elementType.GetCustomAttribute(typeof(CanSaveNLoad)) != null)
                Delete(elementType, string.Empty, string.Format(format, key, i));
            else if (PlayerPrefs.HasKey(string.Format(format, key, i)))
                PlayerPrefs.DeleteKey(string.Format(format, key, i));
        }

        PlayerPrefs.DeleteKey(countKey);
    }
}