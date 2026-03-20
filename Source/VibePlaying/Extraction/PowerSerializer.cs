using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace VibePlaying
{
    /// <summary>
    /// L2 serializer — detailed power grid status per power net.
    /// </summary>
    public static class PowerSerializer
    {
        public static void Append(StringBuilder sb, Map map)
        {
            sb.Append("\"powerGrid\":{");

            var nets = map.powerNetManager.AllNetsListForReading;
            sb.Append($"\"netCount\":{nets.Count},");
            sb.Append("\"nets\":[");

            bool first = true;
            int idx = 0;
            foreach (var net in nets)
            {
                if (idx >= 10) break; // cap at 10 nets
                if (!first) sb.Append(',');

                float stored = 0, production = 0, consumption = 0;
                int batteries = 0;
                var generators = new Dictionary<string, int>();
                var consumers = new Dictionary<string, int>();

                foreach (var comp in net.batteryComps)
                {
                    stored += comp.StoredEnergy;
                    batteries++;
                }
                foreach (var comp in net.powerComps)
                {
                    var defName = comp.parent.def.defName;
                    if (comp.PowerOutput > 0)
                    {
                        production += comp.PowerOutput;
                        if (generators.ContainsKey(defName)) generators[defName]++;
                        else generators[defName] = 1;
                    }
                    else if (comp.PowerOutput < 0)
                    {
                        consumption += -comp.PowerOutput;
                        if (consumers.ContainsKey(defName)) consumers[defName]++;
                        else consumers[defName] = 1;
                    }
                }

                sb.Append('{');
                sb.Append($"\"batteries\":{batteries},");
                sb.Append($"\"stored\":{stored:F0},");
                sb.Append($"\"production\":{production:F0},");
                sb.Append($"\"consumption\":{consumption:F0},");
                sb.Append($"\"surplus\":{production - consumption:F0},");

                // Top generators
                sb.Append("\"generators\":{");
                bool gFirst = true;
                foreach (var kv in generators.OrderByDescending(kv => kv.Value).Take(5))
                {
                    if (!gFirst) sb.Append(',');
                    sb.Append($"\"{kv.Key}\":{kv.Value}");
                    gFirst = false;
                }
                sb.Append("},");

                // Top consumers
                sb.Append("\"consumers\":{");
                bool cFirst = true;
                foreach (var kv in consumers.OrderByDescending(kv => kv.Value).Take(5))
                {
                    if (!cFirst) sb.Append(',');
                    sb.Append($"\"{kv.Key}\":{kv.Value}");
                    cFirst = false;
                }
                sb.Append('}');

                sb.Append('}');
                first = false;
                idx++;
            }

            sb.Append("]}");
        }
    }
}
