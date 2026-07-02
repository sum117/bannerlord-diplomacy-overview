using TaleWorlds.Library;

namespace DiplomacyOverview
{
    /// <summary>
    /// Breadcrumbs into rgl_log via <see cref="Debug.Print"/> — the only channel that survives a
    /// user's session without asking them to install anything. Every degraded path in this mod
    /// (provider skip, rebuild failure, edge-widget self-disable) MUST leave a note here: the #6
    /// in-game pass produced a wrong screen and a completely silent log, which turned a one-run
    /// diagnosis into archaeology. Capped so a per-frame failure cannot flood the log (rule 6:
    /// diagnostics must never hurt the game).
    /// </summary>
    internal static class Diagnostics
    {
        private const int MaxNotes = 40;
        private static int _count;

        public static void Note(string message)
        {
            if (_count >= MaxNotes)
            {
                return;
            }

            _count++;
            try
            {
                Debug.Print("DiplomacyOverview: " + message);
            }
            catch
            {
                // Logging must never become the crash it exists to prevent.
            }
        }
    }
}
