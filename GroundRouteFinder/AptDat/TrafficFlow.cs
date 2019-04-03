using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroundRouteFinder.LogSupport;
using GroundRouteFinder.Output;

namespace GroundRouteFinder.AptDat
{
    public class SimpleFlowRule
    {
        public string Description;
        public double MinVis;
        public double MinCeil;
        public string StartTime;
        public string EndTime;
        public int MinDir;
        public int MaxDir;
        public int MaxSpeed;

        public override string ToString()
        {
            return $"{MinVis:00.0} {MinCeil,5:0} {StartTime} {EndTime} {MaxSpeed,2} {MinDir:000} {MaxDir:000} {Description}";
        }
    }


    public class WindLimits
    {
        public int MinDir;
        public int MaxDir;
        public int MaxSpeed;

        public WindLimits(int minDir, int maxDir, int maxSpeed)
        {
            MinDir = minDir;
            MaxDir = maxDir;
            MaxSpeed = maxSpeed;
        }
    }

    public class TimeLimts
    {
        public string From;
        public string Until;
        public int FromInt;
        public int UntilInt;

        public TimeLimts(int from, int until)
        {
            FromInt = from;
            From = $"{from / 100:00}:{from % 100:00}";

            if (until == 2359)
                until = 2400;

            UntilInt = until;
            Until = $"{until / 100:00}:{until % 100:00}";
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
        private static readonly char[] _splitters = { ' ' };

        public int MinCeiling;
        public double MinVisibility;

        public List<WindLimits> WindLimits;
        public List<TimeLimts> TimeLimits;
        public List<RunwayUse> RunwayUses;

        public string Description = "";

        public TrafficRule(string source)
        {
            MinCeiling = 0;
            MinVisibility = 0;
            WindLimits = new List<WindLimits>();
            TimeLimits = new List<TimeLimts>();
            RunwayUses = new List<RunwayUse>();

            if (source.Length > 5)
            {
                Description = source.Substring(5);
            }
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
            MinVisibility = VortexMath.Parse(tokens[2]);
        }

        public void ParseCeilingRule(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            MinCeiling = int.Parse(tokens[2]);
        }

        public void ParseWindRule(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            WindLimits.Add(new WindLimits(int.Parse(tokens[2]), int.Parse(tokens[3]), int.Parse(tokens[4])));
        }

        public void ParseTimeRule(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            TimeLimits.Add(new TimeLimts(int.Parse(tokens[1]), int.Parse(tokens[2])));
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
        public List<SimpleFlowRule> FlowRules = new List<SimpleFlowRule>();


        public List<TrafficRule> TrafficRules;
        private TrafficRule _currentRule;
        private bool _flowRulesFound;
        public bool RuleSetOk = true;

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
                        _currentRule = new TrafficRule(line);
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
        private bool _noOverlap;

        public bool Analyze(StringBuilder report)
        {
            report.AppendLine("\nTraffic Flow Analysis");

            foreach (TrafficRule rule in TrafficRules)
            {
                if (rule.WindLimits.Count == 0)
                {
                    rule.WindLimits.Add(new WindLimits(0, 360, 99));
                }

                if (rule.TimeLimits.Count == 0)
                {
                    rule.TimeLimits.Add(new TimeLimts(0, 2359));
                }
            }


            // Todo: check whether the result has a fallback and covers all winddirections
            // Todo: handle multiple visibility options
            _operations = new List<string>();
            _runwayOps = new List<string>();

            int ruleIdx = 0;
            IEnumerable<Tuple<int, double>> cvKeys = TrafficRules.Select(tr => new Tuple<int, double>(tr.MinCeiling, tr.MinVisibility)).Distinct();
            if (cvKeys.Count() == 0)
            {
                report.AppendLine(" apt.dat has no usable traffic flow rules.");
                return false;
            }

            foreach (TrafficRule rule in TrafficRules)
            {
                foreach (TimeLimts interval in rule.TimeLimits)
                {
                    foreach (WindLimits winds in rule.WindLimits)
                    {
                        SimpleFlowRule sfr = new SimpleFlowRule();
                        sfr.MinVis = rule.MinVisibility;
                        sfr.MinCeil = rule.MinCeiling;
                        sfr.StartTime = interval.From;
                        sfr.EndTime = interval.Until;
                        sfr.MinDir = winds.MinDir;
                        sfr.MaxDir = winds.MaxDir;
                        sfr.MaxSpeed = winds.MaxSpeed;
                        sfr.Description = rule.Description;
                        FlowRules.Add(sfr);
                    }
                }
            }

            foreach (SimpleFlowRule sfr in FlowRules)
            {
                report.AppendLine(sfr.ToString());
            }


            Tuple<int, double> cvKey = cvKeys.First();
            //            foreach (Tuple<int, double> cvKey in cvKeys)
            {
                Logger.Log($"Using rules for Ceiling>{cvKey.Item1} Vis>{cvKey.Item2}");
                IEnumerable<TrafficRule> rules = TrafficRules.Where(tr => tr.MinCeiling == cvKey.Item1 && tr.MinVisibility == cvKey.Item2);

                // First pass: Verify the rules (and even try to align XP format to WT format)
                _noOverlap = false;

                int currentMinWindSpeed = -1;
                int currentMaxWindSpeed = -1;

                List<Tuple<int, int>> coveredDirections = new List<Tuple<int, int>>();
                foreach (TrafficRule rule in rules.OrderBy(r => r.WindLimits.Min(wl => wl.MaxSpeed))) // Sorting by min MaxWindSpeed
                {
                    foreach (WindLimits windLimit in rule.WindLimits.OrderBy(wl => wl.MaxSpeed)) // Sorting by min MaxWindSpeed again, trying to 'force' the low wind speed rule to be processed first
                    {
                        if (currentMaxWindSpeed == -1)
                        {
                            currentMinWindSpeed = 0;
                            currentMaxWindSpeed = windLimit.MaxSpeed;
                        }
                        else if (currentMaxWindSpeed != windLimit.MaxSpeed)
                        {
                            RuleSetOk &= VerifyRuleCoverage(currentMinWindSpeed, currentMaxWindSpeed, coveredDirections);
                            coveredDirections.Clear();
                            currentMinWindSpeed = currentMaxWindSpeed;
                            currentMaxWindSpeed = windLimit.MaxSpeed;
                        }

                        coveredDirections.Add(new Tuple<int, int>(windLimit.MinDir, windLimit.MaxDir));
                    }
                }
                RuleSetOk &= VerifyRuleCoverage(currentMinWindSpeed, currentMaxWindSpeed, coveredDirections);

                // Second pass... actually generate the rules
                currentMinWindSpeed = -1;
                currentMaxWindSpeed = -1;

                foreach (TrafficRule rule in rules.OrderBy(r => r.WindLimits.Min(wl => wl.MaxSpeed))) // Sorting by min MaxWindSpeed
                {
                    foreach (WindLimits windLimit in rule.WindLimits.OrderBy(wl => wl.MaxSpeed)) // Sorting by min MaxWindSpeed again, trying to 'force' the low wind speed rule to be processed first
                    {
                        if (currentMaxWindSpeed == -1)
                        {
                            currentMinWindSpeed = 0;
                            currentMaxWindSpeed = windLimit.MaxSpeed;
                        }
                        else if (currentMaxWindSpeed != windLimit.MaxSpeed)
                        {
                            currentMinWindSpeed = currentMaxWindSpeed;
                            currentMaxWindSpeed = windLimit.MaxSpeed;
                        }

                        if (rule.TimeLimits.Count > 0)
                        {
                            foreach (TimeLimts timeLimit in rule.TimeLimits)
                            {
                                GenerateOperations(ref ruleIdx, rule, windLimit, currentMinWindSpeed, currentMaxWindSpeed, timeLimit.From, timeLimit.Until);
                            }
                        }
                        else
                        {
                            GenerateOperations(ref ruleIdx, rule, windLimit, currentMinWindSpeed, currentMaxWindSpeed, "00:00", "24:00");
                        }
                    }
                }
            }
            return RuleSetOk;
        }

        /// <summary>
        /// Add operations and runways for the generated operations
        /// </summary>
        /// <param name="ruleIdx"></param>
        /// <param name="rule"></param>
        /// <param name="windLimit"></param>
        /// <param name="currentMinWindSpeed"></param>
        /// <param name="currentMaxWindSpeed"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        private void GenerateOperations(ref int ruleIdx, TrafficRule rule, WindLimits windLimit, int currentMinWindSpeed, int currentMaxWindSpeed, string startTime, string endTime)
        {
            // Extend the upper limit of the wind rule to make it match WT behavior
            if (_noOverlap)
                windLimit.MaxDir++;
            windLimit.MaxDir = Math.Min(360, windLimit.MaxDir);

            // Write the operation, and track whether it actually resulted in two operations
            bool splitOperation = GenerateOperation(ruleIdx, currentMinWindSpeed, windLimit, startTime, endTime, rule.Description);

            Logger.Log($"{currentMinWindSpeed,3}-{currentMaxWindSpeed,3} kts {windLimit.MinDir:000}-{windLimit.MaxDir:000} {startTime} {endTime}");

            // Write the runways for the (or both) operation(s)
            GenerateRunways(rule.RunwayUses, ruleIdx++, startTime, endTime, rule.Description);
            if (splitOperation)
                GenerateRunways(rule.RunwayUses, ruleIdx++, startTime, endTime, rule.Description);
        }

        /// <summary>
        /// Check if the rules for a certain windspeed, visibility and ceiling range do cover the whole windrose from 0-360 degrees
        /// Todo: Check the definition X Plane uses. Value are int, but do they use min < wind < max or min < wind <= max ?
        /// </summary>
        /// <param name="currentMinWindSpeed"></param>
        /// <param name="currentMaxWindSpeed"></param>
        /// <param name="coveredDirections"></param>
        private bool VerifyRuleCoverage(int currentMinWindSpeed, int currentMaxWindSpeed, List<Tuple<int, int>> coveredDirections)
        {
            Tuple<int, int> covered = coveredDirections.First();
            Tuple<int, int> linked = null;

            do
            {
                linked = coveredDirections.FirstOrDefault(cd => cd.Item1 == covered.Item2);
                if (linked == null)
                {
                    linked = coveredDirections.FirstOrDefault(cd => cd.Item1 == covered.Item2 + 1);
                    if (linked != null)
                        _noOverlap = true;
                }

                if (linked == null)
                {
                    linked = coveredDirections.FirstOrDefault(cd => cd.Item2 == covered.Item1);
                    if (linked == null)
                    {
                        linked = coveredDirections.FirstOrDefault(cd => cd.Item2 + 1 == covered.Item1);
                        if (linked != null)
                            _noOverlap = true;
                    }

                    if (linked != null)
                    {
                        if (linked.Item1 > linked.Item2)
                            covered = new Tuple<int, int>(0, covered.Item2);
                        else
                            covered = new Tuple<int, int>(linked.Item1, covered.Item2);
                    }
                }
                else
                {
                    if (linked.Item2 < linked.Item1)
                        covered = new Tuple<int, int>(covered.Item1, 360);
                    else
                        covered = new Tuple<int, int>(covered.Item1, linked.Item2);
                }
            }
            while (linked != null);

            Logger.Log($"{currentMinWindSpeed,3}-{currentMaxWindSpeed,3} kts coverage: {covered.Item1:000}-{covered.Item2:000}");
            if (covered.Item1 != 0 || covered.Item2 != 360)
            {
                Logger.Log($"{currentMinWindSpeed,3}-{currentMaxWindSpeed,3} kts does not have full 360 coverage!");
                return false;
            }
            else
                return true;
        }

        /// <summary>
        /// Generate a line for a wind/time elated operation rule
        /// </summary>
        /// <param name="index">index number of the operation</param>
        /// <param name="windMinSpeed"></param>
        /// <param name="windLimit"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns>true if the operation needed to be split into at 0 degrees</returns>
        private bool GenerateOperation(int index, int windMinSpeed, WindLimits windLimit, string startTime, string endTime, string description)
        {
            if (windLimit.MaxDir < windLimit.MinDir)
            {
                _operations.Add($"{index,-7} {windMinSpeed,-17} {windLimit.MaxSpeed,-18} {windLimit.MinDir,-14} {360,-15} {startTime}  {endTime} {description}");
                _operations.Add($"{index+1,-7} {windMinSpeed,-17} {windLimit.MaxSpeed,-18} {0,-14} {windLimit.MaxDir,-15} {startTime}  {endTime}");
                return true;
            }
            else
            {
                _operations.Add($"{index,-7} {windMinSpeed,-17} {windLimit.MaxSpeed,-18} {windLimit.MinDir,-14} {windLimit.MaxDir,-15} {startTime}  {endTime} {description}");
                return false;
            }
        }

        private void GenerateRunways(List<RunwayUse> runwayUses, int index, string startTime, string endTime, string description)
        {
            foreach (RunwayUse ru in runwayUses)
            {
                if (ru.Arrivals)
                    _runwayOps.Add($"{index,-8} {ru.Designator,-19} 1          {startTime}  {endTime}  {description}");
                if (ru.Departures)
                    _runwayOps.Add($"{index,-8} {ru.Designator,-19} 2          {startTime}  {endTime}  {description}");
            }
        }

        internal void Write(string icao)
        {
            string operationFile = Path.Combine(Settings.WorldTrafficOperations, $"{icao}.txt");
            using (InvariantWriter sw = new InvariantWriter(operationFile, Encoding.ASCII))
            {
                sw.WriteLine("                                                                            Start  End");
                sw.WriteLine("INDEX   Low Wind Speed    High Wind Speed    Low Wind Dir   High Wind Dir   Time   Time  Comments (not parsed)");
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
