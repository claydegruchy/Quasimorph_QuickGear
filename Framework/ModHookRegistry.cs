using System;
using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace QuasimorphHelloWorld.Framework
{
    /// <summary>
    /// Central registry for mod lifecycle hooks and triggers.
    /// Simplifies mod initialization and event handling.
    /// </summary>
    public static class ModHookRegistry
    {
        private static List<Action<IModContext>> _bootstrapActions = new List<Action<IModContext>>();
        private static List<Action<IModContext>> _saveLoadedActions = new List<Action<IModContext>>();
        private static List<Action<IModContext>> _spaceUpdateActions = new List<Action<IModContext>>();

        public static void RegisterBootstrap(Action<IModContext> action)
        {
            _bootstrapActions.Add(action);
        }

        public static void RegisterSaveLoaded(Action<IModContext> action)
        {
            _saveLoadedActions.Add(action);
        }

        public static void RegisterSpaceUpdate(Action<IModContext> action)
        {
            _spaceUpdateActions.Add(action);
        }

        public static void ExecuteBootstrap(IModContext context)
        {
            foreach (var action in _bootstrapActions)
            {
                try
                {
                    action?.Invoke(context);
                }
                catch (Exception e)
                {
                    Debug.Log($"[ModFramework] Error in bootstrap action: {e.Message}");
                }
            }
        }

        public static void ExecuteSaveLoaded(IModContext context)
        {
            foreach (var action in _saveLoadedActions)
            {
                try
                {
                    action?.Invoke(context);
                }
                catch (Exception e)
                {
                    Debug.Log($"[ModFramework] Error in save loaded action: {e.Message}");
                }
            }
        }

        public static void ExecuteSpaceUpdate(IModContext context)
        {
            foreach (var action in _spaceUpdateActions)
            {
                try
                {
                    action?.Invoke(context);
                }
                catch (Exception e)
                {
                    Debug.Log($"[ModFramework] Error in space update action: {e.Message}");
                }
            }
        }
    }
}
