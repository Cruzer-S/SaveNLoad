using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class CanSaveNLoad : Attribute
{
    public string PathName { get; private set; }
    public BindingFlags BindingFlags { get; private set; }

    public CanSaveNLoad(string pathName, BindingFlags bindingFlags = BindingFlags.Default)
    {
        PathName = pathName;
        BindingFlags = bindingFlags;
    }
}

public static partial class SaveNLoad
{
    private static string ListFormat = "{0}#{1}";
    private static string ListCount = ":Count";
    private static string DictCount = ":DictCount";
    private static string DictKeyFormat = "{0}${1}";
    private static string DictValueFormat = "{0}@{1}";

    public static bool Save<T>(T data, string name)
    {
        Delete(data.GetType(), name);
        return Save(data.GetType(), data, name);
    }

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

        fields = type.GetFields(attribute.BindingFlags);
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
                SaveListType(field.FieldType, key, value);
            else if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                SaveDictionaryType(field.FieldType, key, value);
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

    private static void SaveListType(Type type, string key, object values)
    {
        Type elementType = type.GetGenericArguments().Single();
        IList list = values as IList;

        PlayerPrefs.SetInt(key + ListCount, list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            string formattedKey = string.Format(ListFormat, key, i);

            if (elementType.GetCustomAttribute(typeof(CanSaveNLoad)) != null)
                Save(elementType, list[i], string.Empty, formattedKey);
            else
                SaveBasicType(elementType, formattedKey, list[i]);
        }
    }

    private static void SaveDictionaryType(Type type, string key, object values)
    {
        IDictionary dictionary = values as IDictionary;
        Type keyType = type.GetGenericArguments()[0];
        Type valueType = type.GetGenericArguments()[1];

        PlayerPrefs.SetInt(key + DictCount, dictionary.Count);

        int index = 0;
        foreach (object dictKey in dictionary.Keys)
        {
            string formattedKey = string.Format(DictKeyFormat, key, index);
            string formattedValue = string.Format(DictValueFormat, key, index);

            object dictValue = dictionary[dictKey];

            if (keyType.GetCustomAttribute(typeof(CanSaveNLoad)) != null)
                Save(keyType, dictKey, string.Empty, formattedKey);
            else
                SaveBasicType(keyType, formattedKey, dictKey);

            if (valueType.GetCustomAttribute(typeof(CanSaveNLoad)) != null)
                Save(valueType, dictValue, string.Empty, formattedValue);
            else
                SaveBasicType(keyType, formattedValue, dictValue);

            index++;
        }
    }
}

public static partial class SaveNLoad
{
    private static object Load(Type type, string name, string basename = null)
    {
        FieldInfo[] fields;
        CanSaveNLoad attribute;

        attribute = type.GetCustomAttribute<CanSaveNLoad>();
        if (attribute == null)
            return null;

        fields = type.GetFields(attribute.BindingFlags);

        object @object = Activator.CreateInstance(type, true);
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
                value = LoadListType(field.FieldType, key);
            else if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                value = LoadDictionaryType(field.FieldType, key);
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

    private static object LoadListType(Type type, string key)
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
                data = Load(elementType, string.Empty, string.Format(ListFormat, key, i));
            else if (PlayerPrefs.HasKey(string.Format(ListFormat, key, i)))
                data = LoadBasicType(elementType, string.Format(ListFormat, key, i));

            if (data == null)
                return null;

            list.Add(data);
        }

        return list;
    }

    private static object LoadDictionaryType(Type type, string key)
    {
        IDictionary dictionary = (IDictionary) Activator.CreateInstance(type);
        Type keyType = type.GetGenericArguments()[0];
        Type valueType = type.GetGenericArguments()[1];

        string countKey = key + DictCount;
        int count;

        if (!PlayerPrefs.HasKey(countKey))
            return null;

        count = PlayerPrefs.GetInt(countKey);

        for (int i = 0; i < count; i++)
        {
            object dictKey = null;
            object dictValue = null;

            if (keyType.GetCustomAttribute(typeof(CanSaveNLoad)) != null)
                dictKey = Load(keyType, string.Empty, string.Format(DictKeyFormat, key, i));
            else if (PlayerPrefs.HasKey(string.Format(DictKeyFormat, key, i)))
                dictKey = LoadBasicType(keyType, string.Format(DictKeyFormat, key, i));

            if (valueType.GetCustomAttribute(typeof(CanSaveNLoad)) != null)
                dictValue = Load(valueType, string.Empty, string.Format(DictValueFormat, key, i));
            else if (PlayerPrefs.HasKey(string.Format(DictKeyFormat, key, i)))
                dictValue = LoadBasicType(valueType, string.Format(DictValueFormat, key, i));

            if (dictKey == null || dictValue == null)
                return null;

            dictionary.Add(dictKey, dictValue);
        }

        return dictionary;
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

        fields = type.GetFields(attribute.BindingFlags);
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
                DeleteListType(field.FieldType, key);
            else if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                DeleteDictionaryType(field.FieldType, key);
            else if (PlayerPrefs.HasKey(key))
                PlayerPrefs.DeleteKey(key);
        }
    }

    private static void DeleteListType(Type type, string key)
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
                Delete(elementType, string.Empty, string.Format(ListFormat, key, i));
            else if (PlayerPrefs.HasKey(string.Format(ListFormat, key, i)))
                PlayerPrefs.DeleteKey(string.Format(ListFormat, key, i));
        }

        PlayerPrefs.DeleteKey(countKey);
    }

    private static void DeleteDictionaryType(Type type, string key)
    {
        Type keyType = type.GetGenericArguments()[0];
        Type valueType = type.GetGenericArguments()[1];
        string countKey = key + DictCount;
        int count;

        if ( !PlayerPrefs.HasKey(countKey) )
            return;

        count = PlayerPrefs.GetInt(countKey);
        for (int i = 0; i < count; i++)
        {
            if (keyType.GetCustomAttribute(typeof(CanSaveNLoad)) != null)
                Delete(keyType, string.Empty, string.Format(DictKeyFormat, key, i));
            else if (PlayerPrefs.HasKey(string.Format(DictKeyFormat, key, i)))
                PlayerPrefs.DeleteKey(string.Format(DictKeyFormat, key, i));

            if (valueType.GetCustomAttribute(typeof(CanSaveNLoad)) != null)
                Delete(valueType, string.Empty, string.Format(DictValueFormat, key, i));
            else if (PlayerPrefs.HasKey(string.Format(DictValueFormat, key, i)))
                PlayerPrefs.DeleteKey(string.Format(DictValueFormat, key, i));
        }

        PlayerPrefs.DeleteKey(countKey);
    }
}