using Unity.Netcode;

namespace Tolik.RemakeSoF.Runtime.Game.Characters
{
    internal interface ICharacter
    {
        NetworkObject NetworkObject { get; }
    }
}
