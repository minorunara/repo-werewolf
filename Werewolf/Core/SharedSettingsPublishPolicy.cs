namespace Werewolf.Core
{
    public sealed class SharedSettingsPublishPolicy
    {
        private string _publishedRoomId;
        private string _publishedBlob;

        public bool Claim(string roomId, string blob)
        {
            if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(blob)) return false;
            bool sameRoom = string.Equals(roomId, _publishedRoomId, System.StringComparison.Ordinal);
            bool sameBlob = string.Equals(blob, _publishedBlob, System.StringComparison.Ordinal);
            if (sameRoom && sameBlob) return false;
            _publishedRoomId = roomId;
            _publishedBlob = blob;
            return true;
        }

        public void Reset()
        {
            _publishedRoomId = null;
            _publishedBlob = null;
        }
    }
}
