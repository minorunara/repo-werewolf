using System;

namespace Werewolf.Core
{
    public static class CosmeticGrantWire
    {
        public static object[] ToWire(CosmeticGrant grant)
        {
            if (grant == null) throw new ArgumentNullException(nameof(grant));

            return new object[] { grant.Actors, grant.Rarities };
        }

        public static bool TryFromWire(object[] payload, out CosmeticGrant grant)
        {
            grant = null;

            if (payload == null || payload.Length != 2)
            {
                return false;
            }

            var actors = payload[0] as int[];
            var rarities = payload[1] as byte[];
            if (actors == null || rarities == null)
            {
                return false;
            }

            if (rarities.Length != actors.Length * CosmeticLottery.CoinsPerPlayer)
            {
                return false;
            }

            for (int i = 0; i < rarities.Length; i++)
            {
                if (rarities[i] >= CoinRarity.Count)
                {
                    return false;
                }
            }

            grant = new CosmeticGrant(actors, rarities);
            return true;
        }
    }
}
