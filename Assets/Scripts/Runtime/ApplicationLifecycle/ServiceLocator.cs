using System;
using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.ApplicationLifecycle
{
    public static class ServiceLocator
    {
        private static Dictionary<Type, object> s_Services = new();

        public static void Register<T>(T service) where T : class
        {
            s_Services[typeof(T)] = service;
        }

        public static T Get<T>() where T : class
        {
            if (s_Services.TryGetValue(typeof(T), out var service))
                return service as T;
            return null;
        }

        public static void ClearAll()
        {
            foreach (var service in s_Services.Values)
            {
                (service as TextureManager)?.ClearCache();
                (service as PrefabManager)?.ClearCache();
            }
            s_Services.Clear();
        }
    }
}