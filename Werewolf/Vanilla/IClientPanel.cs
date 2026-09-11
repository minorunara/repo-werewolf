using UnityEngine;

namespace Werewolf.Vanilla
{
    public interface IClientPanel
    {
        string LayerName { get; }

        bool Exists { get; }

        void Build(Transform layerRoot);

        void Destroy();
    }
}
