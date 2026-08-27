namespace Werewolf.Core
{
    public static class CorpseReportProximity
    {
        public const float DistanceMeters = 3f;

        public static bool IsWithin(float distanceMeters)
            => distanceMeters <= DistanceMeters;
    }
}
