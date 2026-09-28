using System.Collections.Generic;

namespace Groundsman.Core.Content
{
    /// <summary>A bowling attack's make-up, which decides how a pitch is worn and exploited.</summary>
    public sealed class AttackProfile
    {
        public AttackProfile(double seam, double leftArm, double heavyFooted)
        {
            Seam = seam;
            LeftArm = leftArm;
            HeavyFooted = heavyFooted;
        }

        /// <summary>Share of overs bowled by seamers, 0 to 1; spinners bowl the rest.</summary>
        public double Seam { get; }

        public double Spin => 1 - Seam;

        /// <summary>Share of overs bowled left-arm, 0 to 1, which puts rough on the other side.</summary>
        public double LeftArm { get; }

        /// <summary>How heavily the seamers land, 0 to 1: heavier bowlers dig deeper footholes.</summary>
        public double HeavyFooted { get; }
    }

    public sealed class TeamSettings
    {
        public TeamSettings(string id, string name, double batting, double bowling, AttackProfile attack)
        {
            var path = $"teams[{id}]";
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ContentException("teams: every team needs an id.");
            }
            CheckStrength(path, "batting", batting);
            CheckStrength(path, "bowling", bowling);
            CheckShare(path, "seam", attack.Seam);
            CheckShare(path, "leftArm", attack.LeftArm);
            CheckShare(path, "heavyFooted", attack.HeavyFooted);

            Id = id;
            Name = name;
            Batting = batting;
            Bowling = bowling;
            Attack = attack;
        }

        public string Id { get; }
        public string Name { get; }

        /// <summary>0 to 100.</summary>
        public double Batting { get; }

        /// <summary>0 to 100.</summary>
        public double Bowling { get; }

        public AttackProfile Attack { get; }

        private static void CheckStrength(string path, string field, double value)
        {
            if (value < 0 || value > 100)
            {
                throw new ContentException($"{path}.{field} ({value}) must be from 0 to 100.");
            }
        }

        private static void CheckShare(string path, string field, double value)
        {
            if (value < 0 || value > 1)
            {
                throw new ContentException($"{path}.attack.{field} ({value}) must be from 0 to 1.");
            }
        }
    }

    public sealed class TeamsSettings
    {
        public TeamsSettings(IReadOnlyList<TeamSettings> teams, string homeId)
        {
            var ids = new HashSet<string>();
            foreach (var team in teams)
            {
                if (!ids.Add(team.Id))
                {
                    throw new ContentException($"teams: two teams have the id \"{team.Id}\".");
                }
            }
            if (!ids.Contains(homeId))
            {
                throw new ContentException($"teams.home (\"{homeId}\") isn't one of the teams.");
            }

            Teams = new List<TeamSettings>(teams).AsReadOnly();
            HomeId = homeId;
        }

        public IReadOnlyList<TeamSettings> Teams { get; }
        public string HomeId { get; }

        public TeamSettings? Find(string id)
        {
            foreach (var team in Teams)
            {
                if (team.Id == id)
                {
                    return team;
                }
            }
            return null;
        }
    }
}
