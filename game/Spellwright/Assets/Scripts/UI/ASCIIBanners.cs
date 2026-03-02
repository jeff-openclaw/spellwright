namespace Spellwright.UI
{
    /// <summary>
    /// Static class with large ASCII art banners for key game moments.
    /// Uses box-drawing characters and block elements.
    /// </summary>
    public static class ASCIIBanners
    {
        public static readonly string Victory = string.Join("\n",
            "\u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
            "\u2551  \u2588\u2588\u2588 WORD FOUND \u2588\u2588\u2588  \u2551",
            "\u2551   \u2605 \u2605 \u2605 \u2605 \u2605 \u2605 \u2605 \u2605   \u2551",
            "\u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d");

        public static readonly string Defeat = string.Join("\n",
            "\u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
            "\u2551  \u2591\u2591\u2591 WORD LOST \u2591\u2591\u2591   \u2551",
            "\u2551   \u2022 \u2022 \u2022 \u2022 \u2022 \u2022 \u2022 \u2022   \u2551",
            "\u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d");

        public static readonly string BossEntrance = string.Join("\n",
            "\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593",
            "\u2593                        \u2593",
            "\u2593   \u2588\u2588\u2588  BOSS  \u2588\u2588\u2588      \u2593",
            "\u2593   ENCOUNTER BEGINS     \u2593",
            "\u2593                        \u2593",
            "\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593");

        public static readonly string RunVictory = string.Join("\n",
            "\u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
            "\u2551                            \u2551",
            "\u2551   \u2605  SPELLWRIGHT  \u2605        \u2551",
            "\u2551      CHAMPION             \u2551",
            "\u2551                            \u2551",
            "\u2551  \u2605 \u2605 \u2605 \u2605 \u2605 \u2605 \u2605 \u2605 \u2605 \u2605  \u2551",
            "\u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d");

        public static readonly string RunDefeat = string.Join("\n",
            "\u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
            "\u2551                            \u2551",
            "\u2551   \u2591  JOURNEY OVER  \u2591      \u2551",
            "\u2551                            \u2551",
            "\u2551   The words have spoken   \u2551",
            "\u2551                            \u2551",
            "\u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d");

        /// <summary>Returns the appropriate result banner for win/loss.</summary>
        public static string GetResultBanner(bool won) => won ? Victory : Defeat;

        /// <summary>Returns the appropriate run end banner for win/loss.</summary>
        public static string GetRunEndBanner(bool won) => won ? RunVictory : RunDefeat;
    }
}
