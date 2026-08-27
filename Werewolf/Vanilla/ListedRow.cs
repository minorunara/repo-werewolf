using UnityEngine;

namespace Werewolf.Vanilla
{
    public sealed class ListedRow
    {
        internal readonly MenuPlayerListed Listed;

        internal readonly Color StaticHeadColor;

        internal readonly Color StaticNameColor;

        internal ListedRow(MenuPlayerListed listed)
        {
            Listed = listed;
        }

        internal ListedRow(MenuPlayerListed listed, Color headColor, Color nameColor)
        {
            Listed = listed;
            IsStatic = true;
            StaticHeadColor = headColor;
            StaticNameColor = nameColor;
        }

        public bool IsStatic { get; }

        public bool Exists => Listed != null;

        public GameObject GameObject => Listed.gameObject;

        public bool IsActiveInHierarchy => Listed.gameObject.activeInHierarchy;

        public void SetNameText(string text)
        {
            if (Listed.playerName != null) Listed.playerName.text = text;
        }

        public void SetNameColor(Color color)
        {
            if (Listed.playerName != null) Listed.playerName.color = color;
        }
    }
}
