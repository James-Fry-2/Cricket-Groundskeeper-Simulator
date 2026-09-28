namespace Groundsman.Core.Content
{
    public sealed class RollerSettings
    {
        public RollerSettings(string id, string name, double compactionPerHour, double wetDamagePerHour, double overuseAbove, double overuseDamagePerHour)
        {
            var path = $"rollers[{id}]";
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ContentException("rollers: every roller needs an id.");
            }
            CheckNotNegative(path, "compactionPerHour", compactionPerHour);
            CheckNotNegative(path, "wetDamagePerHour", wetDamagePerHour);
            CheckNotNegative(path, "overuseDamagePerHour", overuseDamagePerHour);
            if (overuseAbove < 0 || overuseAbove > 1)
            {
                throw new ContentException($"{path}.overuseAbove ({overuseAbove}) must be from 0 to 1.");
            }

            Id = id;
            Name = name;
            CompactionPerHour = compactionPerHour;
            WetDamagePerHour = wetDamagePerHour;
            OveruseAbove = overuseAbove;
            OveruseDamagePerHour = overuseDamagePerHour;
        }

        public string Id { get; }
        public string Name { get; }

        /// <summary>Share of the remaining room to full compaction closed by an hour's rolling in the window.</summary>
        public double CompactionPerHour { get; }

        /// <summary>Structure damage per hour when rolling a surface wetter than the window.</summary>
        public double WetDamagePerHour { get; }

        /// <summary>Compaction above which this roller starts doing harm; 1 for rollers light enough never to.</summary>
        public double OveruseAbove { get; }

        public double OveruseDamagePerHour { get; }

        private static void CheckNotNegative(string path, string field, double value)
        {
            if (value < 0)
            {
                throw new ContentException($"{path}.{field} ({value}) can't be negative.");
            }
        }
    }
}
