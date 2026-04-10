using CrashKonijn.Agent.Core;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>Gemeinsame Action-Data-Klasse fuer alle AI-Bot-Aktionen.</summary>
    public class AIActionData : IActionData
    {
        /// <summary>Das Ziel dieser Aktion (Position oder Transform).</summary>
        public ITarget Target { get; set; }
    }
}
