using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
    public enum XPlaneAircraftType
    {
        Fighter,
        Helo,
        Prop,
        TurboProp,
        Jet,
        HeavyJet
    }

    public enum XPlaneAircraftCategory : int
    {
        A = 0,
        B = 1,
        C = 2,
        D = 3,
        E = 4,
        F = 5,
        Max = 6
    }

    public enum WorldTrafficAircraftType : int
    {
        Fighter = 0,
        SuperHeavy = 1,
        HeavyJet = 2,
        LargeJet = 3,
        LargeProp = 4,
        MediumJet = 5,
        MediumProp = 6,
        LightJet = 7,
        LightProp = 8,
        Helo = 9,
        Ground = 10,
    }

    public enum OperationType
    {
        None,
        GeneralAviation,
        Airline,
        Cargo,
        Military
    }

    public enum StartUpLocationType
    {
        Gate,
        Hangar,
        Misc,
        TieDown
    }

    public static class StartUpLocationTypeConverter
    {
        public static StartUpLocationType FromString(string locationType)
        {
            switch (locationType)
            {
                case "gate":
                    return StartUpLocationType.Gate;
                case "hangar":
                    return StartUpLocationType.Hangar;
                case "misc":
                    return StartUpLocationType.Misc;
                case "tie_down":
                    return StartUpLocationType.TieDown;
                default:
                    throw new NotSupportedException($"LocationType <{locationType}> is not supported.");
            }
        }

    }

    public static class OperationTypeConverter
    {
        public static OperationType FromString(string operationType)
        {
            switch (operationType)
            {
                case "none":
                    return OperationType.None;
                case "general_aviation":
                    return OperationType.GeneralAviation;
                case "airline":
                    return OperationType.Airline;
                case "cargo":
                    return OperationType.Cargo;
                case "military":
                    return OperationType.Military;
                default:
                    throw new NotSupportedException($"OperationType <{operationType}> is not supported.");
            }
        }
    }

    public static class AircraftTypeConverter
    {

        static AircraftTypeConverter()
        {
        }

        public static IEnumerable<XPlaneAircraftType> XPlaneTypesFromStrings(string[] stringTypes)
        {
            List<XPlaneAircraftType> converted = new List<XPlaneAircraftType>();
            foreach (string xpString in stringTypes)
            {
                switch (xpString)
                {
                    case "heavy":
                        converted.Add(XPlaneAircraftType.HeavyJet);
                        break;
                    case "jets":
                        converted.Add(XPlaneAircraftType.Jet);
                        break;
                    case "turboprops":
                        converted.Add(XPlaneAircraftType.TurboProp);
                        break;
                    case "props":
                        converted.Add(XPlaneAircraftType.Prop);
                        break;
                    case "helos":
                        converted.Add(XPlaneAircraftType.Helo);
                        break;
                    case "fighters":
                        converted.Add(XPlaneAircraftType.Fighter);
                        break;
                    case "all":
                        converted.Add(XPlaneAircraftType.HeavyJet);
                        converted.Add(XPlaneAircraftType.Jet);
                        converted.Add(XPlaneAircraftType.TurboProp);
                        converted.Add(XPlaneAircraftType.Prop);
                        converted.Add(XPlaneAircraftType.Helo);
                        converted.Add(XPlaneAircraftType.Fighter);
                        break;
                    default:
                        // todo: report warning
                        break;
                }
            }
            return converted.Distinct();
        }

        public static IEnumerable<WorldTrafficAircraftType> WTTypesFromXPlaneLimits(XPlaneAircraftCategory minCategory, XPlaneAircraftCategory maxCategory, OperationType operationType)
        {
            List<WorldTrafficAircraftType> wtTypes = new List<WorldTrafficAircraftType>() { WorldTrafficAircraftType.Fighter };

            for (XPlaneAircraftCategory cat = minCategory; cat <= maxCategory; cat++)
            {
                switch (cat)
                {
                    case XPlaneAircraftCategory.A:
                        switch (operationType)
                        {
                            case OperationType.Airline:
                            case OperationType.Cargo:
                            case OperationType.GeneralAviation:
                                wtTypes.AddRange(new WorldTrafficAircraftType[] { WorldTrafficAircraftType.LightJet, WorldTrafficAircraftType.LightProp });
                                break;
                            case OperationType.Military:
                                wtTypes.AddRange(new WorldTrafficAircraftType[] { WorldTrafficAircraftType.LightJet, WorldTrafficAircraftType.LightProp, WorldTrafficAircraftType.Fighter });
                                break;
                            case OperationType.None:
                            default:
                                break;
                        }
                        break;
                    case XPlaneAircraftCategory.B:
                        switch (operationType)
                        {
                            case OperationType.Airline:
                            case OperationType.Cargo:
                            case OperationType.GeneralAviation:
                                wtTypes.AddRange(new WorldTrafficAircraftType[] { WorldTrafficAircraftType.MediumJet, WorldTrafficAircraftType.MediumProp, WorldTrafficAircraftType.LightJet, WorldTrafficAircraftType.LightProp });
                                break;
                            case OperationType.Military:
                                wtTypes.AddRange(new WorldTrafficAircraftType[] { WorldTrafficAircraftType.MediumJet, WorldTrafficAircraftType.MediumProp, WorldTrafficAircraftType.LightJet, WorldTrafficAircraftType.LightProp, WorldTrafficAircraftType.Fighter });
                                break;
                            case OperationType.None:
                            default:
                                break;
                        }
                        break;
                    case XPlaneAircraftCategory.C:
                        switch (operationType)
                        {
                            case OperationType.Airline:
                            case OperationType.Cargo:
                                wtTypes.AddRange(new WorldTrafficAircraftType[] { WorldTrafficAircraftType.LargeJet, WorldTrafficAircraftType.LargeProp });
                                break;
                            case OperationType.GeneralAviation:
                            case OperationType.Military:
                                wtTypes.AddRange(new WorldTrafficAircraftType[] { WorldTrafficAircraftType.MediumJet, WorldTrafficAircraftType.MediumProp, WorldTrafficAircraftType.LargeJet, WorldTrafficAircraftType.LargeProp });
                                break;
                            case OperationType.None:
                            default:
                                break;
                        }
                        break;
                    case XPlaneAircraftCategory.D:
                        switch (operationType)
                        {
                            case OperationType.Airline:
                            case OperationType.Cargo:
                            case OperationType.GeneralAviation:
                            case OperationType.Military:
                                wtTypes.AddRange(new WorldTrafficAircraftType[] { WorldTrafficAircraftType.LargeJet, WorldTrafficAircraftType.HeavyJet, WorldTrafficAircraftType.LargeProp });
                                break;
                            case OperationType.None:
                            default:
                                break;
                        }
                        break;
                    case XPlaneAircraftCategory.E:
                        switch (operationType)
                        {
                            case OperationType.Airline:
                            case OperationType.Cargo:
                            case OperationType.GeneralAviation:
                            case OperationType.Military:
                                wtTypes.AddRange(new WorldTrafficAircraftType[] { WorldTrafficAircraftType.HeavyJet, WorldTrafficAircraftType.LargeProp });
                                break;
                            case OperationType.None:
                            default:
                                break;
                        }
                        break;
                    case XPlaneAircraftCategory.F:
                        switch (operationType)
                        {
                            case OperationType.Airline:
                            case OperationType.Cargo:
                            case OperationType.GeneralAviation:
                            case OperationType.Military:
                                wtTypes.AddRange(new WorldTrafficAircraftType[] { WorldTrafficAircraftType.HeavyJet, WorldTrafficAircraftType.SuperHeavy });
                                break;
                            case OperationType.None:
                            default:
                                break;
                        }
                        break;
                }
            }
            return wtTypes.Distinct();
        }

        public static IEnumerable<WorldTrafficAircraftType> WTTypesFromXPlaneTypes(IEnumerable<XPlaneAircraftType> xpTypes)
        {
            List<WorldTrafficAircraftType> wtTypes = new List<WorldTrafficAircraftType>();

            foreach (XPlaneAircraftType xpType in xpTypes)
            {
                switch (xpType)
                {
                    case XPlaneAircraftType.Fighter:
                        wtTypes.Add(WorldTrafficAircraftType.Fighter);
                        break;
                    case XPlaneAircraftType.Helo:
                        wtTypes.Add(WorldTrafficAircraftType.Helo);
                        break;
                    case XPlaneAircraftType.Prop:
                        wtTypes.Add(WorldTrafficAircraftType.LightProp);
                        wtTypes.Add(WorldTrafficAircraftType.LightJet);
                        break;
                    case XPlaneAircraftType.TurboProp:
                        wtTypes.Add(WorldTrafficAircraftType.MediumProp);
                        break;
                    case XPlaneAircraftType.Jet:
                        wtTypes.Add(WorldTrafficAircraftType.MediumJet);
                        wtTypes.Add(WorldTrafficAircraftType.LargeJet);
                        wtTypes.Add(WorldTrafficAircraftType.LargeProp);
                        break;
                    case XPlaneAircraftType.HeavyJet:
                        wtTypes.Add(WorldTrafficAircraftType.HeavyJet);
                        wtTypes.Add(WorldTrafficAircraftType.SuperHeavy);
                        break;
                    default:
                        break;
                }
            }
            return wtTypes.Distinct();
        }
    }
}
