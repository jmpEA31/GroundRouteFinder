using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
    public class WindLimts
    {
        public int MinDir;
        public int MaxDir;
        public int MaxSpeed;

        public WindLimts(int minDir, int maxDir, int maxSpeed)
        {
            MinDir = minDir;
            MaxDir = maxDir;
            MaxSpeed = maxSpeed;
        }
    }

    public class TimeLimts
    {
        public int From;
        public int Until;

        public TimeLimts(int from, int until)
        {
            From = from;
            Until = until;
        }
    }

    public class RunwayUse
    {
        public string Designator;
        public bool Arrivals;
        public bool Departures;
        public List<XPlaneAircraftType> XpTypes;

        public RunwayUse(string designator)
        {
            Designator = designator;
            Arrivals = false;
            Departures = false;
            XpTypes = new List<XPlaneAircraftType>();
        }
    }

    public class TrafficRule
    {
        private static char[] _splitters = { ' ' };

        public int MinCeiling;
        public double MinVisibility;

        public List<WindLimts> WindLimits;
        public List<TimeLimts> TimeLimits;
        public List<RunwayUse> RunwayUses;

        public TrafficRule()
        {
            MinCeiling = 0;
            MinVisibility = 0;
            WindLimits = new List<WindLimts>();
            TimeLimits = new List<TimeLimts>();
            RunwayUses = new List<RunwayUse>();
        }

        public RunwayUse GetUse(string designator)
        {
            RunwayUse ru = RunwayUses.SingleOrDefault(rwu => rwu.Designator == designator);
            if (ru == null)
            {
                ru = new RunwayUse(designator);
                RunwayUses.Add(ru);
            }
            return ru;
        }

        public void ParseVisibilityRule(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            MinVisibility = double.Parse(tokens[2]);
        }

        public void ParseCeilingRule(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            MinCeiling = int.Parse(tokens[2]);
        }

        public void ParseWindRule(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            WindLimits.Add(new WindLimts(int.Parse(tokens[2]), int.Parse(tokens[3]), int.Parse(tokens[4])));
        }

        public void ParseTimeRule(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            TimeLimits.Add(new TimeLimts(int.Parse(tokens[2]), int.Parse(tokens[3])));
        }

        public void ParseRunwayUse(string line, List<Runway> runways)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            RunwayUse ru = GetUse(tokens[1]);
            ru.Arrivals |= (tokens[3].Contains("arr"));
            ru.Departures |= (tokens[3].Contains("dep"));
            ru.XpTypes.AddRange(AircraftTypeConverter.XPlaneTypesFromStrings(tokens[4].Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)));

            Runway r = runways.SingleOrDefault(rw => rw.Designator == ru.Designator);
            if (r != null)
            {
                if (ru.Arrivals)
                    r.AvailableForLanding = true;

                if (ru.Departures)
                    r.AvailableForTakeOff = true;
            }
        }

        public void ParseRunwayVfrUse(string line, List<Runway> runways)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            RunwayUse ru = GetUse(tokens[1]);
            ru.Arrivals = true;
            ru.Departures = true;
            ru.XpTypes.Add(XPlaneAircraftType.Prop); // Very cautious, though types from other rules for this runway will be added as well.

            Runway r = runways.SingleOrDefault(rw => rw.Designator == tokens[1]);
            if (r != null)
                r.AvailableForVFR = true;
        }

    }

    public class TrafficFlow
    {
        public List<TrafficRule> TrafficRules;
        private TrafficRule _currentRule;
        private bool _flowRulesFound;

        public TrafficFlow()
        {
            TrafficRules = new List<TrafficRule>();
            _currentRule = null;
            _flowRulesFound = false;
        }

        public bool ParseInfo(string line, List<Runway> runways)
        {
            if (line.Length > 4)
            {
                switch (line.Substring(0, 4))
                {
                    case "1000":
                        _currentRule = new TrafficRule();
                        TrafficRules.Add(_currentRule);

                        if (!_flowRulesFound)
                        {
                            foreach (Runway rw in runways)
                            {
                                rw.AvailableForLanding = false;
                                rw.AvailableForTakeOff = false;
                                rw.AvailableForVFR = false;
                            }
                            _flowRulesFound = true;
                        }
                        return true;
                    case "1001":
                        _currentRule.ParseWindRule(line);
                        return true;
                    case "1002":
                        _currentRule.ParseCeilingRule(line);
                        return true;
                    case "1003":
                        _currentRule.ParseVisibilityRule(line);
                        return true;
                    case "1004":
                        _currentRule.ParseTimeRule(line);
                        return true;
                    case "1100":
                        if (line.StartsWith("1100 Gener"))
                            return false;
                        _currentRule.ParseRunwayUse(line, runways);
                        return true;
                    case "1101":
                        _currentRule.ParseRunwayVfrUse(line, runways);
                        return true;
                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        private List<string> _operations;
        private List<string> _runwayOps;

        public void Analyze()
        {
            // Todo: check whether the result has a fallback and covers all winddirections
            // Todo: handle multiple visibility options
            _operations = new List<string>();
            _runwayOps = new List<string>();

            int ruleIdx = 0;
            IEnumerable<Tuple<int, double>> cvKeys = TrafficRules.Select(tr => new Tuple<int, double>(tr.MinCeiling, tr.MinVisibility)).Distinct();
            if (cvKeys.Count() == 0)
                return;

            Tuple<int, double> cvKey = cvKeys.First();
            //            foreach (Tuple<int, double> cvKey in cvKeys)
            {
                Console.WriteLine($"Rules for Ceiling>{cvKey.Item1} Vis>{cvKey.Item2}");
                IEnumerable<TrafficRule> rules = TrafficRules.Where(tr => tr.MinCeiling == cvKey.Item1 && tr.MinVisibility == cvKey.Item2);

                int lowWind = 0;
                foreach (TrafficRule rule in rules.OrderBy(r => r.WindLimits.Min(wl => wl.MaxSpeed))) // Sorting by min MaxWindSpeed
                {
                    foreach (WindLimts windLimit in rule.WindLimits.OrderBy(wl => wl.MaxSpeed)) // Sorting by min MaxWindSpeed again, trying to 'force' the low wind speed rule to be processed first
                    {
                        if (rule.TimeLimits.Count > 0)
                        {
                            foreach (TimeLimts timeLimit in rule.TimeLimits)
                            {
                                string startTime = $"{timeLimit.From / 100}:{timeLimit.From % 100}";
                                string endTime = $"{timeLimit.Until / 100}:{timeLimit.Until % 100}";
                                writeOperation(ruleIdx, lowWind, windLimit, startTime, endTime);
                                writeRunways(rule.RunwayUses, ruleIdx++, startTime, endTime);
                            }
                        }
                        else
                        {
                            writeOperation(ruleIdx, lowWind, windLimit, "00:00", "24:00");
                            writeRunways(rule.RunwayUses, ruleIdx++, "00:00", "24:00");
                        }

                        // Should catch most cases, but needs a proper solution
                        if (windLimit.MinDir == 0 && windLimit.MaxDir == 360)
                            lowWind = windLimit.MaxSpeed;
                    }
                }
            }
        }

        private void writeOperation(int index, int windMinSpeed, WindLimts windLimit, string startTime, string endTime)
        {
            _operations.Add($"{index,-7} {windMinSpeed,-17} {windLimit.MaxSpeed,-18} {windLimit.MinDir,-14} {windLimit.MaxDir,-15} {startTime}  {endTime}");
        }

        private void writeRunways(List<RunwayUse> runwayUses, int index, string startTime, string endTime)
        {
            foreach (RunwayUse ru in runwayUses)
            {
                if (ru.Arrivals)
                    _runwayOps.Add($"{index,-8} {ru.Designator,-19} 1          {startTime}  {endTime}");
                if (ru.Departures)
                    _runwayOps.Add($"{index,-8} {ru.Designator,-19} 2          {startTime}  {endTime}");
            }
        }

        internal void Write(string icao)
        {
            string operationFile = Path.Combine(Settings.WorldTrafficOperations, $"{icao}.txt");
            using (StreamWriter sw = File.CreateText(operationFile))
            {
                sw.WriteLine("                                                                            Start  End");
                sw.WriteLine("INDEX   Low Wind Speed    High Wind Speed    Low Wind Dir   High Wind Dir   Time   Time Comments (not parsed)");
                sw.WriteLine("---------------------------------------------------------------------------------------------------------------------");
                sw.WriteLine("START_OPERATIONS");
                foreach (string op in _operations)
                {
                    sw.WriteLine(op);
                }
                sw.WriteLine("END_OPERATIONS\n\n");

                sw.WriteLine("Ops                           1   2     Start End");
                sw.WriteLine("Index    Active Runway       Arr Dep    Time   Time   Comments (not parsed)");
                sw.WriteLine("-------------------------------------------------------------------------------------------------");
                sw.WriteLine("START_RUNWAY_OPS");
                foreach (string rwop in _runwayOps)
                {
                    sw.WriteLine(rwop);
                }
                sw.WriteLine("END_RUNWAY_OPS\n\n");

                sw.WriteLine("        Supported AC Types    Supported Approaches");
                sw.WriteLine("Runway  0 1 2 3 4 5 6 7 8 9     1 2 3 4 5 6 7 8");
                sw.WriteLine("---------------------------------------------------------------------------");
                sw.WriteLine("START_RUNWAYS");


                IEnumerable <IGrouping<string, RunwayUse>> uses = TrafficRules.SelectMany(tr => tr.RunwayUses).GroupBy(ru => ru.Designator);
                foreach (IGrouping<string, RunwayUse> use in uses)
                {
                    IEnumerable<XPlaneAircraftType> types = use.SelectMany(u => u.XpTypes).Distinct();
                    IEnumerable<WorldTrafficAircraftType> wtTypes = AircraftTypeConverter.WTTypesFromXPlaneTypes(types);
                    List<int> onOffs = new List<int>();
                    for (WorldTrafficAircraftType t = WorldTrafficAircraftType.Fighter; t < WorldTrafficAircraftType.Ground; t++)
                    {
                        onOffs.Add(wtTypes.Contains(t) ? 1 : 0);
                    }

                    sw.WriteLine($"{use.Key,-7} {string.Join(" ", onOffs)}     1 1 1 1 1 1 1 0");
                }

                sw.WriteLine("END_RUNWAYS");
            }
        }
    }
}
