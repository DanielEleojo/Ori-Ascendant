namespace OriAscendant.Audio
{
    /// <summary>
    /// Pure mapping from game state to BGM theme slot (GAMEPLAY §3.2 / TECH_DESIGN
    /// §4 "reads the player's path to select the correct musical theme"). Theme
    /// array layout: [0] default/path-less, [1] Ane, [2] Sango, [3] Osun.
    /// Headless-testable; the MonoBehaviour owns the AudioSources.
    /// </summary>
    public static class AudioTrackSelector
    {
        public const int ThemeCount = 4;

        /// <summary>currentPath -1 (path-less) -> default theme; 0/1/2 -> path theme.</summary>
        public static int ThemeIndexForPath(int pathIndex)
        {
            if (pathIndex < 0 || pathIndex > 2) return 0;
            return pathIndex + 1;
        }
    }
}
