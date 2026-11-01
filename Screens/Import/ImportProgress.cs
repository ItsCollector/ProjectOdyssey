namespace ProjectOdyssey.Screens.Import
{
    public static class ImportProgress
    {
        private static readonly object gate = new();
        private static readonly List<string> rows = new();
        private static bool importing;

        public static bool IsImporting
        {
            get { lock (gate) return importing; }
        }

        // Returns false if an import is already running; true means the caller now owns the flag.
        public static bool TryBeginImport()
        {
            lock (gate)
            {
                if (importing) return false;
                importing = true;
                rows.Clear();
                return true;
            }
        }

        public static void EndImport()
        {
            lock (gate) importing = false;
        }

        // Import thread: replaces the last row if it's the same set's progress, otherwise adds a new one.
        public static void Report(string text, bool newRow)
        {
            lock (gate)
            {
                if (newRow || rows.Count == 0) rows.Add(text);
                else rows[^1] = text;

                // The view only shows the newest 9 rows, so cap the log
                if (rows.Count > 200) rows.RemoveRange(0, rows.Count - 200);
            }
        }

        // Render thread: copy out so it never enumerates the live list.
        public static bool Snapshot(List<string> into)
        {
            lock (gate)
            {
                into.Clear();
                into.AddRange(rows);
                return importing;
            }
        }
    }
}