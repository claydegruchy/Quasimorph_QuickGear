using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace QuasimorphHelloWorld.Framework
{
    /// <summary>
    /// Generic configuration manager for any mod config type.
    /// Handles loading/saving JSON files automatically.
    /// </summary>
    public class GenericConfigManager<T> where T : class, new()
    {
        private T _config;
        private string _configPath;

        public T Config => _config;

        public GenericConfigManager(string configPath)
        {
            _configPath = configPath;
            _config = new T();
        }

        public void EnsureDefaultConfig(T defaultConfig = null)
        {
            try
            {
                string dir = Path.GetDirectoryName(_configPath);

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (!File.Exists(_configPath))
                {
                    var configToSave = defaultConfig ?? new T();
                    string json = JsonConvert.SerializeObject(configToSave, Formatting.Indented);
                    File.WriteAllText(_configPath, json);
                    Debug.Log($"[ModFramework] Created default config at: {_configPath}");
                }

                LoadConfig();
            }
            catch (Exception e)
            {
                Debug.Log($"[ModFramework] Failed to ensure default config. Error: {e.Message}");
            }
        }

        public void LoadConfig()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    Debug.Log($"[ModFramework] Config file not found: {_configPath}");
                    _config = new T();
                    return;
                }

                string json = File.ReadAllText(_configPath);
                _config = JsonConvert.DeserializeObject<T>(json) ?? new T();
                Debug.Log($"[ModFramework] Loaded config from: {_configPath}");
            }
            catch (Exception e)
            {
                Debug.Log($"[ModFramework] Failed to load config. Error: {e.Message}");
                _config = new T();
            }
        }

        public void SaveConfig()
        {
            try
            {
                string dir = Path.GetDirectoryName(_configPath);

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(_configPath, json);
                Debug.Log($"[ModFramework] Saved config to: {_configPath}");
            }
            catch (Exception e)
            {
                Debug.Log($"[ModFramework] Failed to save config. Error: {e.Message}");
            }
        }

        public void LoadFromAlternatePath(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.Log($"[ModFramework] Config file not found: {path}");
                    return;
                }

                string json = File.ReadAllText(path);
                _config = JsonConvert.DeserializeObject<T>(json) ?? new T();
                Debug.Log($"[ModFramework] Loaded config from alternate path: {path}");
            }
            catch (Exception e)
            {
                Debug.Log($"[ModFramework] Failed to load config from alternate path. Error: {e.Message}");
            }
        }
    }
}
