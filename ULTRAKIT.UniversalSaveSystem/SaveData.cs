using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ULTRAKIT.UniversalSaveSystem
{
    public static class SaveData
    {
        private const string folderName = "data";
        private static string dataFilePath = GetDataPath("save.ultradata");

        internal static PersistentData data
        {
            get
            {
                if (_data == null)
                    Load();
                return _data;
            }
            private set 
            { 
                data = value; 
            }
        }

        private static PersistentData _data;

        /// <summary>
        /// Gets the (sub)directory of Universal Save System's data folder
        /// </summary>
        /// <param name="subpath"></param>
        /// <returns></returns>
        internal static string GetDataPath(params string[] subpath)
        {
            string modDir = Assembly.GetExecutingAssembly().Location;
            modDir = Path.GetDirectoryName(modDir);
            string localPath = Path.Combine(modDir, folderName);

            if (subpath.Length > 0)
            {
                string subLocalPath = Path.Combine(subpath);
                localPath = Path.Combine(localPath, subLocalPath);
            }

            return localPath;
        }

        // What all internal functions use, returns true if the data already exists
        /// <summary>
        /// Internal data setter
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool Internal_SetValue<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
                Save();
                return true;
            }
            dict.Add(key, value);
            Save();
            return false;
        }

        // Creates a dictionary for the calling assembly if it doesn't exist, then passes it off to Internal_SetValue()
        /// <summary>
        /// Internal private data setter, do not use
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        private static bool Internal_SetPrivateValue<TKey, TValue>(Dictionary<string, Dictionary<TKey, TValue>> dict, TKey key, TValue value, string assemblyName)
        {
            if (dict.ContainsKey(assemblyName))
            {
                return Internal_SetValue(dict[assemblyName], key, value);
            }
            dict.Add(assemblyName, new Dictionary<TKey, TValue>());
            Internal_SetValue(dict[assemblyName], key, value);
            return false;
        }

        /// <summary>
        /// Saves data to a file
        /// </summary>
        internal static void Save()
        {
            if (!Directory.Exists(GetDataPath()))
                Directory.CreateDirectory(GetDataPath());

            string json = JsonConvert.SerializeObject(_data);
            File.WriteAllText(dataFilePath, json);
            Mod.Logger.LogInfo("Saved persistent data");
        }

        /// <summary>
        /// Loads data from the data file
        /// </summary>
        internal static void Load()
        {
            Mod.Logger.LogInfo("Loading persistent data...");
            if (!File.Exists(dataFilePath))
            {
                _data = PersistentData.Default;
                Save();
                return;
            }

            string json;
            using (StreamReader reader = new StreamReader(dataFilePath))
            {
                json = reader.ReadToEnd();
            }
            _data = JsonConvert.DeserializeObject<PersistentData>(json);
            return;
        }

        // Please excuse the following methods, they work the way they do to keep the user from having to call 16 different functions (which honestly wouldn't look much better)
        /// <summary>
        /// Saves a value to persistent data,.
        /// If `global == true` the value will be saved in a dictionary unique to the calling assembly.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="global"></param>
        /// <returns>`true` if the data already exists in the registry, `false` otherwise.</returns>
        public static bool SetPersistent(string key, object value, bool global)
        {
            if (global)
            {
                if (value is string)
                { return Internal_SetValue(data.g_string_data, key, (string)value); }
                if (value is int)
                { return Internal_SetValue(data.g_int_data, key, (int)value); }
                if (value is float)
                { return Internal_SetValue(data.g_float_data, key, (float)value); }
                if (value is bool)
                { return Internal_SetValue(data.g_bool_data, key, (bool)value); }
                if (value is string[])
                { return Internal_SetValue(data.g_string_data_array, key, (string[])value); }
                if (value is int[])
                { return Internal_SetValue(data.g_int_data_array, key, (int[])value); }
                if (value is float[])
                { return Internal_SetValue(data.g_float_data_array, key, (float[])value); }
                if (value is bool[])
                { return Internal_SetValue(data.g_bool_data_array, key, (bool[])value); }
                return false;
            }
            string assembly = Assembly.GetCallingAssembly().GetName().Name;
            if (value is string)
            { return Internal_SetPrivateValue(data.p_string_data, key, (string)value, assembly); }
            if (value is int)
            { return Internal_SetPrivateValue(data.p_int_data, key, (int)value, assembly); }
            if (value is float)
            { return Internal_SetPrivateValue(data.p_float_data, key, (float)value, assembly); }
            if (value is bool)
            { return Internal_SetPrivateValue(data.p_bool_data, key, (bool)value, assembly); }
            if (value is string[])
            { return Internal_SetPrivateValue(data.p_string_data_array, key, (string[])value, assembly); }
            if (value is int[])
            { return Internal_SetPrivateValue(data.p_int_data_array, key, (int[])value, assembly); }
            if (value is float[])
            { return Internal_SetPrivateValue(data.p_float_data_array, key, (float[])value, assembly); }
            if (value is bool[])
            { return Internal_SetPrivateValue(data.p_bool_data_array, key, (bool[])value, assembly); }
            return false;
        }

        /// <summary>
        /// Internal data getter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="global"></param>
        /// <param name="value"></param>
        /// <returns>`true` if successful, `false` otherwise (in which case it passes a `default` out value).</returns>
        private static bool Internal_TryGetPersistent<T>(string key, bool global, string assembly, out object value)
        {
            Type type = typeof(T);
            if (global)
            {
                if (type.IsEquivalentTo(typeof(string)))
                {
                    string outValue;
                    bool success = data.g_string_data.TryGetValue(key, out outValue);
                    value = outValue;
                    return success;
                }
                if (type.IsEquivalentTo(typeof(int)))
                {
                    int outValue;
                    bool success = data.g_int_data.TryGetValue(key, out outValue);
                    value = outValue;
                    return success;
                }
                if (type.IsEquivalentTo(typeof(float)))
                {
                    float outValue;
                    bool success = data.g_float_data.TryGetValue(key, out outValue);
                    value = outValue;
                    return success;
                }
                if (type.IsEquivalentTo(typeof(bool)))
                {
                    bool outValue;
                    bool success = data.g_bool_data.TryGetValue(key, out outValue);
                    value = outValue;
                    return success;
                }
                if (type.IsEquivalentTo(typeof(string[])))
                {
                    string[] outValue;
                    bool success = data.g_string_data_array.TryGetValue(key, out outValue);
                    value = outValue;
                    return success;
                }
                if (type.IsEquivalentTo(typeof(int[])))
                {
                    int[] outValue;
                    bool success = data.g_int_data_array.TryGetValue(key, out outValue);
                    value = outValue;
                    return success;
                }
                if (type.IsEquivalentTo(typeof(float[])))
                {
                    float[] outValue;
                    bool success = data.g_float_data_array.TryGetValue(key, out outValue);
                    value = outValue;
                    return success;
                }
                if (type.IsEquivalentTo(typeof(bool[])))
                {
                    bool[] outValue;
                    bool success = data.g_bool_data_array.TryGetValue(key, out outValue);
                    value = outValue;
                    return success;
                }
                value = default(T);
                return false;
            }
            if (type.IsEquivalentTo(typeof(string)))
            {
                bool success = false;
                string outValue = default(string);
                Dictionary<string, string> outDict;
                if (data.p_string_data.TryGetValue(assembly, out outDict))
                    success = outDict.TryGetValue(key, out outValue);
                value = outValue;
                return success;
            }
            if (type.IsEquivalentTo(typeof(int)))
            {
                bool success = false;
                int outValue = default(int);
                Dictionary<string, int> outDict;
                if (data.p_int_data.TryGetValue(assembly, out outDict))
                    success = outDict.TryGetValue(key, out outValue);
                value = outValue;
                return success;
            }
            if (type.IsEquivalentTo(typeof(float)))
            {
                bool success = false;
                float outValue = default(float);
                Dictionary<string, float> outDict;
                if (data.p_float_data.TryGetValue(assembly, out outDict))
                    success = outDict.TryGetValue(key, out outValue);
                value = outValue;
                return success;
            }
            if (type.IsEquivalentTo(typeof(bool)))
            {
                bool success = false;
                bool outValue = default(bool);
                Dictionary<string, bool> outDict;
                if (data.p_bool_data.TryGetValue(assembly, out outDict))
                    success = outDict.TryGetValue(key, out outValue);
                value = outValue;
                return success;
            }
            if (type.IsEquivalentTo(typeof(string[])))
            {
                bool success = false;
                string[] outValue = default(string[]);
                Dictionary<string, string[]> outDict;
                if (data.p_string_data_array.TryGetValue(assembly, out outDict))
                    success = outDict.TryGetValue(key, out outValue);
                value = outValue;
                return success;
            }
            if (type.IsEquivalentTo(typeof(int[])))
            {
                bool success = false;
                int[] outValue = default(int[]);
                Dictionary<string, int[]> outDict;
                if (data.p_int_data_array.TryGetValue(assembly, out outDict))
                    success = outDict.TryGetValue(key, out outValue);
                value = outValue;
                return success;
            }
            if (type.IsEquivalentTo(typeof(float[])))
            {
                bool success = false;
                float[] outValue = default(float[]);
                Dictionary<string, float[]> outDict;
                if (data.p_float_data_array.TryGetValue(assembly, out outDict))
                    success = outDict.TryGetValue(key, out outValue);
                value = outValue;
                return success;
            }
            if (type.IsEquivalentTo(typeof(bool[])))
            {
                bool success = false;
                bool[] outValue = default(bool[]);
                Dictionary<string, bool[]> outDict;
                if (data.p_bool_data_array.TryGetValue(assembly, out outDict))
                    success = outDict.TryGetValue(key, out outValue);
                value = outValue;
                return success;
            }
            value = default(T);
            return false;
        }

        /// <summary>
        /// <para>Retrieves a persistent value, passing it to an out value. If `global == false`, it retrieves from a dictionary unique to the calling assembly.</para>
        /// T must match the type being retrieved, though the out variable must be of type `object` and then cast into the correct type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="global"></param>
        /// <param name="value"></param>
        /// <returns>`true` if successful, `false` otherwise (in which case it passes a `default` out value).</returns>
        public static bool TryGetPersistent<T>(string key, bool global, out object value)
        {
            return Internal_TryGetPersistent<T>(key, global, Assembly.GetCallingAssembly().GetName().Name, out value);
        }

        /// <summary>
        /// <para>Retrieves a persistent value. If `global == false`, it retrieves from a dictionary unique to the calling assembly.</para>
        /// T must match the type being retrieved.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="global"></param>
        /// <param name="value"></param>
        /// <returns>`true` if successful, `false` otherwise (in which case it passes a `default` out value).</returns>
        public static T GetPersistent<T>(string key, bool global)
        {
            string assembly = Assembly.GetCallingAssembly().GetName().Name;

            if (Internal_TryGetPersistent<T>(key, global, assembly, out object value))
            {
                return (T)value;
            }
            throw new KeyNotFoundException($"Key '{key}' not found in global data.");
        }
    }

    [System.Serializable]
    public class PersistentData
    {
        // External Data - Global
        public Dictionary<string, string> g_string_data;
        public Dictionary<string, int> g_int_data;
        public Dictionary<string, float> g_float_data;
        public Dictionary<string, bool> g_bool_data;
        public Dictionary<string, string[]> g_string_data_array;
        public Dictionary<string, int[]> g_int_data_array;
        public Dictionary<string, float[]> g_float_data_array;
        public Dictionary<string, bool[]> g_bool_data_array;

        // External Data - Private
        public Dictionary<string, Dictionary<string, string>> p_string_data;
        public Dictionary<string, Dictionary<string, int>> p_int_data;
        public Dictionary<string, Dictionary<string, float>> p_float_data;
        public Dictionary<string, Dictionary<string, bool>> p_bool_data;
        public Dictionary<string, Dictionary<string, string[]>> p_string_data_array;
        public Dictionary<string, Dictionary<string, int[]>> p_int_data_array;
        public Dictionary<string, Dictionary<string, float[]>> p_float_data_array;
        public Dictionary<string, Dictionary<string, bool[]>> p_bool_data_array;

        internal static readonly PersistentData Default = new PersistentData()
        {
            g_string_data = new Dictionary<string, string>(),
            g_int_data = new Dictionary<string, int>(),
            g_float_data = new Dictionary<string, float>(),
            g_bool_data = new Dictionary<string, bool>(),
            g_string_data_array = new Dictionary<string, string[]>(),
            g_int_data_array = new Dictionary<string, int[]>(),
            g_float_data_array = new Dictionary<string, float[]>(),
            g_bool_data_array = new Dictionary<string, bool[]>(),

            p_string_data = new Dictionary<string, Dictionary<string, string>>(),
            p_int_data = new Dictionary<string, Dictionary<string, int>>(),
            p_float_data = new Dictionary<string, Dictionary<string, float>>(),
            p_bool_data = new Dictionary<string, Dictionary<string, bool>>(),
            p_string_data_array = new Dictionary<string, Dictionary<string, string[]>>(),
            p_int_data_array = new Dictionary<string, Dictionary<string, int[]>>(),
            p_float_data_array = new Dictionary<string, Dictionary<string, float[]>>(),
            p_bool_data_array = new Dictionary<string, Dictionary<string, bool[]>>()
        };
    }
}
