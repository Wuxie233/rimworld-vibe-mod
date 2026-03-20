using System.Collections.Generic;

namespace VibePlaying
{
    /// <summary>
    /// A building template is a predefined arrangement of buildings
    /// that the AI can place as a unit, avoiding the need for the LLM
    /// to reason about individual cell coordinates.
    /// </summary>
    public class BuildingTemplate
    {
        public string Name;
        public string Description;
        public int Width;
        public int Height;
        public List<TemplateEntry> Entries = new List<TemplateEntry>();
    }

    public class TemplateEntry
    {
        /// <summary>Offset from template origin (top-left).</summary>
        public int DX;
        public int DZ;
        public string BuildingDef;
        public int Rotation; // 0=N, 1=E, 2=S, 3=W
        public string StuffDef; // nullable — defaults to best available
    }

    /// <summary>
    /// Built-in template library. Templates are compact structural patterns;
    /// the AI picks one, chooses origin + stuff, and the executor places it.
    /// </summary>
    public static class TemplateLibrary
    {
        private static readonly Dictionary<string, BuildingTemplate> templates = new Dictionary<string, BuildingTemplate>();

        static TemplateLibrary()
        {
            Register(Bedroom3x4());
            Register(Barracks5x7());
            Register(Kitchen4x4());
            Register(Hospital4x5());
            Register(Killbox11x5());
            Register(Storage5x5());
            Register(ResearchRoom4x4());
        }

        public static BuildingTemplate Get(string name)
        {
            templates.TryGetValue(name.ToLower(), out var t);
            return t;
        }

        public static IEnumerable<string> Names => templates.Keys;

        public static string SummaryForPrompt()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kv in templates)
            {
                var t = kv.Value;
                sb.Append($"- {kv.Key}: {t.Description} ({t.Width}x{t.Height})\n");
            }
            return sb.ToString();
        }

        private static void Register(BuildingTemplate t)
        {
            templates[t.Name.ToLower()] = t;
        }

        // ==================== Templates ====================

        /// <summary>3x4 single bedroom: walls, door, double bed, end table, dresser.</summary>
        private static BuildingTemplate Bedroom3x4()
        {
            var t = new BuildingTemplate { Name = "bedroom", Description = "Single bedroom (bed, end table, dresser)", Width = 3, Height = 4 };
            // Walls around perimeter
            AddWallRect(t, 0, 0, 3, 4);
            // Door on south wall center
            t.Entries.Add(new TemplateEntry { DX = 1, DZ = 0, BuildingDef = "Door", Rotation = 0 });
            // Remove wall where door is
            t.Entries.RemoveAll(e => e.BuildingDef == "Wall" && e.DX == 1 && e.DZ == 0);
            // Bed against north wall
            t.Entries.Add(new TemplateEntry { DX = 1, DZ = 2, BuildingDef = "Bed", Rotation = 0 });
            // End table
            t.Entries.Add(new TemplateEntry { DX = 1, DZ = 3, BuildingDef = "EndTable", Rotation = 0 });
            return t;
        }

        /// <summary>5x7 barracks: 4 beds, 2 end tables.</summary>
        private static BuildingTemplate Barracks5x7()
        {
            var t = new BuildingTemplate { Name = "barracks", Description = "4-bed barracks", Width = 5, Height = 7 };
            AddWallRect(t, 0, 0, 5, 7);
            t.Entries.Add(new TemplateEntry { DX = 2, DZ = 0, BuildingDef = "Door", Rotation = 0 });
            t.Entries.RemoveAll(e => e.BuildingDef == "Wall" && e.DX == 2 && e.DZ == 0);
            // 4 beds in 2 rows
            t.Entries.Add(new TemplateEntry { DX = 1, DZ = 2, BuildingDef = "Bed", Rotation = 0 });
            t.Entries.Add(new TemplateEntry { DX = 3, DZ = 2, BuildingDef = "Bed", Rotation = 0 });
            t.Entries.Add(new TemplateEntry { DX = 1, DZ = 5, BuildingDef = "Bed", Rotation = 0 });
            t.Entries.Add(new TemplateEntry { DX = 3, DZ = 5, BuildingDef = "Bed", Rotation = 0 });
            return t;
        }

        /// <summary>4x4 kitchen: electric stove + butcher table.</summary>
        private static BuildingTemplate Kitchen4x4()
        {
            var t = new BuildingTemplate { Name = "kitchen", Description = "Kitchen (stove + butcher table)", Width = 4, Height = 4 };
            AddWallRect(t, 0, 0, 4, 4);
            t.Entries.Add(new TemplateEntry { DX = 1, DZ = 0, BuildingDef = "Door", Rotation = 0 });
            t.Entries.RemoveAll(e => e.BuildingDef == "Wall" && e.DX == 1 && e.DZ == 0);
            t.Entries.Add(new TemplateEntry { DX = 1, DZ = 2, BuildingDef = "ElectricStove", Rotation = 0 });
            t.Entries.Add(new TemplateEntry { DX = 2, DZ = 2, BuildingDef = "TableButcher", Rotation = 0 });
            return t;
        }

        /// <summary>4x5 hospital: 2 hospital beds + vitals monitor.</summary>
        private static BuildingTemplate Hospital4x5()
        {
            var t = new BuildingTemplate { Name = "hospital", Description = "Hospital (2 beds + vitals)", Width = 4, Height = 5 };
            AddWallRect(t, 0, 0, 4, 5);
            t.Entries.Add(new TemplateEntry { DX = 1, DZ = 0, BuildingDef = "Door", Rotation = 0 });
            t.Entries.RemoveAll(e => e.BuildingDef == "Wall" && e.DX == 1 && e.DZ == 0);
            t.Entries.Add(new TemplateEntry { DX = 1, DZ = 2, BuildingDef = "HospitalBed", Rotation = 0 });
            t.Entries.Add(new TemplateEntry { DX = 2, DZ = 2, BuildingDef = "HospitalBed", Rotation = 0 });
            t.Entries.Add(new TemplateEntry { DX = 1, DZ = 4, BuildingDef = "VitalsMonitor", Rotation = 0 });
            return t;
        }

        /// <summary>11x5 killbox corridor: walls on sides, sandbags at entrance, turrets inside.</summary>
        private static BuildingTemplate Killbox11x5()
        {
            var t = new BuildingTemplate { Name = "killbox", Description = "Killbox corridor (turrets + sandbags)", Width = 11, Height = 5 };
            // Long walls on both sides
            for (int x = 0; x < 11; x++)
            {
                t.Entries.Add(new TemplateEntry { DX = x, DZ = 0, BuildingDef = "Wall", Rotation = 0 });
                t.Entries.Add(new TemplateEntry { DX = x, DZ = 4, BuildingDef = "Wall", Rotation = 0 });
            }
            // Entrance sandbags
            t.Entries.Add(new TemplateEntry { DX = 0, DZ = 1, BuildingDef = "Sandbags", Rotation = 0 });
            t.Entries.Add(new TemplateEntry { DX = 0, DZ = 2, BuildingDef = "Sandbags", Rotation = 0 });
            t.Entries.Add(new TemplateEntry { DX = 0, DZ = 3, BuildingDef = "Sandbags", Rotation = 0 });
            // Turrets at far end
            t.Entries.Add(new TemplateEntry { DX = 9, DZ = 1, BuildingDef = "Turret_MiniTurret", Rotation = 3 });
            t.Entries.Add(new TemplateEntry { DX = 9, DZ = 3, BuildingDef = "Turret_MiniTurret", Rotation = 3 });
            return t;
        }

        /// <summary>5x5 storage room.</summary>
        private static BuildingTemplate Storage5x5()
        {
            var t = new BuildingTemplate { Name = "storage", Description = "Storage room 5x5", Width = 5, Height = 5 };
            AddWallRect(t, 0, 0, 5, 5);
            t.Entries.Add(new TemplateEntry { DX = 2, DZ = 0, BuildingDef = "Door", Rotation = 0 });
            t.Entries.RemoveAll(e => e.BuildingDef == "Wall" && e.DX == 2 && e.DZ == 0);
            return t;
        }

        /// <summary>4x4 research room: hi-tech bench + multi-analyzer.</summary>
        private static BuildingTemplate ResearchRoom4x4()
        {
            var t = new BuildingTemplate { Name = "research", Description = "Research room (bench + multi-analyzer)", Width = 4, Height = 4 };
            AddWallRect(t, 0, 0, 4, 4);
            t.Entries.Add(new TemplateEntry { DX = 1, DZ = 0, BuildingDef = "Door", Rotation = 0 });
            t.Entries.RemoveAll(e => e.BuildingDef == "Wall" && e.DX == 1 && e.DZ == 0);
            t.Entries.Add(new TemplateEntry { DX = 1, DZ = 2, BuildingDef = "HiTechResearchBench", Rotation = 0 });
            t.Entries.Add(new TemplateEntry { DX = 2, DZ = 2, BuildingDef = "MultiAnalyzer", Rotation = 0 });
            return t;
        }

        // ==================== Helpers ====================

        private static void AddWallRect(BuildingTemplate t, int x, int z, int w, int h)
        {
            for (int dx = 0; dx < w; dx++)
            {
                t.Entries.Add(new TemplateEntry { DX = x + dx, DZ = z, BuildingDef = "Wall", Rotation = 0 });
                t.Entries.Add(new TemplateEntry { DX = x + dx, DZ = z + h - 1, BuildingDef = "Wall", Rotation = 0 });
            }
            for (int dz = 1; dz < h - 1; dz++)
            {
                t.Entries.Add(new TemplateEntry { DX = x, DZ = z + dz, BuildingDef = "Wall", Rotation = 0 });
                t.Entries.Add(new TemplateEntry { DX = x + w - 1, DZ = z + dz, BuildingDef = "Wall", Rotation = 0 });
            }
        }
    }
}
