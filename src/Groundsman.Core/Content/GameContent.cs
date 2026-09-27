using System.Collections.Generic;

namespace Groundsman.Core.Content
{
    public sealed class GameContent
    {
        private readonly Dictionary<string, LoamSettings> _loamsById = new Dictionary<string, LoamSettings>();

        public GameContent(
            CalendarSettings calendar,
            ClimateSettings climate,
            GroundSettings ground,
            IReadOnlyList<LoamSettings> loams,
            MoistureSettings moisture,
            TaskSettings tasks,
            ReadingSettings readings)
        {
            foreach (var loam in loams)
            {
                if (_loamsById.ContainsKey(loam.Id))
                {
                    throw new ContentException($"loams: two loams have the id \"{loam.Id}\".");
                }
                _loamsById.Add(loam.Id, loam);
            }

            // Checks that span files: each strip's loam exists and its moisture fits that loam.
            for (var i = 0; i < ground.Strips.Count; i++)
            {
                var strip = ground.Strips[i];
                if (!_loamsById.TryGetValue(strip.LoamId, out var loam))
                {
                    throw new ContentException($"ground.strips[{i}].loam (\"{strip.LoamId}\") isn't in loams.json.");
                }
                if (strip.SurfaceMoisture > loam.Saturation || strip.SubsurfaceMoisture > loam.Saturation)
                {
                    throw new ContentException($"ground.strips[{i}] starts wetter than its loam's saturation ({loam.Saturation}).");
                }
            }

            Calendar = calendar;
            Climate = climate;
            Ground = ground;
            Loams = new List<LoamSettings>(loams).AsReadOnly();
            Moisture = moisture;
            Tasks = tasks;
            Readings = readings;
        }

        public CalendarSettings Calendar { get; }
        public ClimateSettings Climate { get; }
        public GroundSettings Ground { get; }
        public IReadOnlyList<LoamSettings> Loams { get; }
        public MoistureSettings Moisture { get; }
        public TaskSettings Tasks { get; }
        public ReadingSettings Readings { get; }

        public LoamSettings Loam(string id) => _loamsById[id];
    }
}
