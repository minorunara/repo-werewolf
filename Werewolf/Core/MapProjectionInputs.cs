namespace Werewolf.Core
{
    public struct MapProjectionInputs
    {
        public float MapScale, OriginX, OriginZ, CamX, CamZ, Ortho, Aspect;

        public bool SameAs(in MapProjectionInputs o)
            => MapScale == o.MapScale && OriginX == o.OriginX && OriginZ == o.OriginZ
               && CamX == o.CamX && CamZ == o.CamZ && Ortho == o.Ortho && Aspect == o.Aspect;

        public MapPanelProjection ToProjection(float panelWidth, float panelHeight)
            => new MapPanelProjection(MapScale, OriginX, OriginZ, CamX, CamZ, Ortho, Aspect, panelWidth, panelHeight);
    }
}
