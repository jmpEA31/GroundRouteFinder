using System;
using System.Collections.Generic;
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

        public void ParseRunwayUse(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            RunwayUse ru = new RunwayUse();
            ru.Designator = tokens[1];
            ru.Arrivals = (tokens[3].Contains("arr"));
            ru.Departures = (tokens[3].Contains("dep"));
            RunwayUses.Add(ru);
        }
    }

    public class TrafficFlow
    {
        public List<TrafficRule> TrafficRules = new List<TrafficRule>();
        private TrafficRule _currentRule = null;

        public TrafficFlow()
        {
        }

        public bool ParseInfo(string line)
        {
            if (line.Length > 4)
            {
                switch (line.Substring(0,4))
                {
                    case "1000":
                        _currentRule = new TrafficRule();
                        TrafficRules.Add(_currentRule);
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

                        _currentRule.ParseRunwayUse(line);
                        return true;
                    //case "1101":
                    //    addVFR(tokens);
                    //    return true;
                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void Analyze()
        {
            int ruleIdx = 0;
            IEnumerable<Tuple<int, double>> cvKeys = TrafficRules.Select(tr => new Tuple<int, double>(tr.MinCeiling, tr.MinVisibility)).Distinct();
            foreach (Tuple<int, double> cvKey in cvKeys)
            {
                Console.WriteLine($"Rules for Ceiling>{cvKey.Item1} Vis>{cvKey.Item2}");
                IEnumerable<TrafficRule> rules = TrafficRules.Where(tr => tr.MinCeiling == cvKey.Item1 && tr.MinVisibility == cvKey.Item2);
                foreach (TrafficRule rule in rules)
                {
                    int lowWind = 0;
                    foreach (WindLimts windLimit in rule.WindLimits.OrderBy(wl=>wl.MaxSpeed))
                    {
                        if (rule.TimeLimits.Count > 0)
                        {
                            foreach (TimeLimts timeLimit in rule.TimeLimits)
                            {
                                string startTime = $"{timeLimit.From / 100}:{timeLimit.From % 100}";
                                string endTime = $"{timeLimit.Until / 100}:{timeLimit.Until % 100}";
                                Console.WriteLine($"{ruleIdx,-8} {lowWind,-8} {windLimit.MaxSpeed,-8} {windLimit.MinDir,-8} {windLimit.MaxDir,-8} {startTime}  {endTime}");
                                writeRunways(rule.RunwayUses, ruleIdx++, startTime, endTime);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{ruleIdx,-8} {lowWind,-8} {windLimit.MaxSpeed,-8} {windLimit.MinDir,-8} {windLimit.MaxDir,-8} 00:00  24:00");
                            writeRunways(rule.RunwayUses, ruleIdx++, "00:00", "24:00");
                        }
                        lowWind = windLimit.MaxSpeed;
                    }
                }
            }
        }

        private void writeRunways(List<RunwayUse> runwayUses, int index, string startTime, string endTime)
        {
            foreach (RunwayUse ru in runwayUses)
            {
                if (ru.Arrivals)
                    Console.WriteLine($"{index,-8} {ru.Designator} 1 {startTime}  {endTime}");
                if (ru.Departures)
                    Console.WriteLine($"{index,-8} {ru.Designator} 2 {startTime}  {endTime}");
            }
        }
    }
}
