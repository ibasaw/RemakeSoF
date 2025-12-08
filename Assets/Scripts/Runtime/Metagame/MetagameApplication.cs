using System;
using Unity.Netcode;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// The application that manages the Metagame
    /// </summary>
    public class MetagameApplication : BaseApplication<MetagameModel, MetagameView, MetagameController>
    {
        internal new static MetagameApplication Instance { get; private set; }
        protected override void Awake()
        {
            base.Awake();
            Instance = this;
        }
    }
}
