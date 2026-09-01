namespace Engine.Graphics {
    [Flags]
    public enum TextAnchor {
        Default = 0,
        Left = 0,
        Top = 0,
        HorizontalCenter = 1,
        VerticalCenter = 1 << 1,
        Right = 1 << 2,
        Bottom = 1 << 3,
        Center = 3,
        DisableSnapToPixels = 1 << 4
    }
}