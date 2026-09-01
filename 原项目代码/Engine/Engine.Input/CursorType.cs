namespace Engine.Input {
    public enum CursorType {

        /// <summary>
        /// Default cursor.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Regular arrow cursor.
        /// </summary>
        Arrow = 1,

        /// <summary>
        /// Text input I-beam cursor.
        /// </summary>
        IBeam = 2,

        /// <summary>
        /// Crosshair cursor.
        /// </summary>
        Crosshair = 3,

        /// <summary>
        /// Hand cursor.
        /// </summary>
        Hand = 4,

        /// <summary>
        /// Horizontal resize arrow cursor.
        /// </summary>
        HResize = 5,

        /// <summary>
        /// Vertical resize arrow cursor.
        /// </summary>
        VResize = 6,

        /// <summary>
        /// Top-left to bottom-right diagonal resize/move arrow cursor.
        /// </summary>
        NwseResize = 7,

        /// <summary>
        /// Top-right to bottom-left diagonal resize/move arrow cursor.
        /// </summary>
        NeswResize = 8,

        /// <summary>
        /// Omni-directional resize/move cursor.
        /// </summary>
        ResizeAll = 9,

        /// <summary>
        /// Operation not allowed cursor.
        /// </summary>
        NotAllowed = 10,

        /// <summary>
        /// Hourglass/waiting cursor.
        /// </summary>
        Wait = 11,

        /// <summary>
        /// Regular arrow but with an hourglass/waiting icon cursor.
        /// </summary>
        WaitArrow = 12,
        Grab = 13,
        Grabbing = 14
    }
}