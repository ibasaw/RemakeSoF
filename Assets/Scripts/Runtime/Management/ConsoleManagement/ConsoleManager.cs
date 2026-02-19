using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.Core;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.ConsoleManagement
{
    /// <summary>
    /// The application that manages the console
    /// </summary>
    public class ConsoleManager : BaseApplication<ConsoleModel, ConsoleView, ConsoleController>
    {
        internal new static ConsoleManager Instance { get; private set; }
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
    }
}
