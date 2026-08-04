using UnityEngine;

namespace Werewolf.UI
{
    public interface IClientPanel
    {
        string LayerName { get; }

        bool Exists { get; }

        void Build(Transform layerRoot);

        void Destroy();
    }
}
