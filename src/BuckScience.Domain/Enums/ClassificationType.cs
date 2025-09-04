namespace BuckScience.Domain.Enums
{
    /// <summary>
    /// Distinguishes between all static and dynamic property features relevant to deer movement and habitat use.
    /// Topographical features shape movement; resource features provide food, water, or security.
    /// </summary>
    public enum ClassificationType
    {
        // Topographical (Terrain) Features
        Ridge = 1,
        RidgePoint = 2,
        RidgeSpur = 3,
        Saddle = 4,
        Bench = 5,
        Draw = 6,
        CreekCrossing = 7,
        Ditch = 8,
        Valley = 9,
        Bluff = 10,
        FieldEdge = 11,
        InsideCorner = 12,
        Peninsula = 13,
        Island = 14,
        PinchPointFunnel = 15,
        TravelCorridor = 16,
        Spur = 17,
        Knob = 18,

        // Resource Features: Food
        AgCropField = 31,
        FoodPlot = 32,
        MastTreePatch = 33,        // Hard or soft mast
        BrowsePatch = 34,
        PrairieForbPatch = 35,

        // Resource Features: Water
        Creek = 51,
        Pond = 52,
        Lake = 53,
        Spring = 54,
        Waterhole = 55,
        Trough = 56,

        // Resource Features: Bedding & Cover
        BeddingArea = 70,
        ThickBrush = 71,
        Clearcut = 72,
        CRP = 73,                  // Conservation Reserve Program/tall grass
        Swamp = 74,
        CedarThicket = 75,
        LeewardSlope = 76,
        EdgeCover = 77,
        IsolatedCover = 78,
        ManMadeCover = 79,

        // Other
        Other = 99
    }
}