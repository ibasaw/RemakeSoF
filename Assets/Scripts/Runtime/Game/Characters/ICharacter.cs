using Unity.Netcode;

namespace Tolik.RemakeSoF.Runtime
{
    internal interface ICharacter
    {
        NetworkObject NetworkObject { get; }
    }
}
