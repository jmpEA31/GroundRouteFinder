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
        Ground = 10
    }
   

    public static class AircraftTypeConverter
    {
        private static Dictionary<XPlaneAircraftCategory, Dictionary<XPlaneAircraftType, WorldTrafficAircraftType>> _typeMapping;

        static AircraftTypeConverter()
        {
            _typeMapping = new Dictionary<XPlaneAircraftCategory, Dictionary<XPlaneAircraftType, WorldTrafficAircraftType>>();

            _typeMapping[XPlaneAircraftCategory.A] = new Dictionary<XPlaneAircraftType, WorldTrafficAircraftType>();
            _typeMapping[XPlaneAircraftCategory.A][XPlaneAircraftType.Jet] = WorldTrafficAircraftType.LightJet;
            _typeMapping[XPlaneAircraftCategory.A][XPlaneAircraftType.TurboProp] = WorldTrafficAircraftType.LightProp;
            _typeMapping[XPlaneAircraftCategory.A][XPlaneAircraftType.Prop] = WorldTrafficAircraftType.LightProp;
            _typeMapping[XPlaneAircraftCategory.A][XPlaneAircraftType.Helo] = WorldTrafficAircraftType.Helo;
            _typeMapping[XPlaneAircraftCategory.A][XPlaneAircraftType.Fighter] = WorldTrafficAircraftType.Fighter;

            _typeMapping[XPlaneAircraftCategory.B] = new Dictionary<XPlaneAircraftType, WorldTrafficAircraftType>();
            _typeMapping[XPlaneAircraftCategory.B][XPlaneAircraftType.Jet] = WorldTrafficAircraftType.MediumJet;
            _typeMapping[XPlaneAircraftCategory.B][XPlaneAircraftType.TurboProp] = WorldTrafficAircraftType.MediumProp;

            _typeMapping[XPlaneAircraftCategory.C] = new Dictionary<XPlaneAircraftType, WorldTrafficAircraftType>();
            _typeMapping[XPlaneAircraftCategory.C][XPlaneAircraftType.Jet] = WorldTrafficAircraftType.LargeJet;

            _typeMapping[XPlaneAircraftCategory.D] = new Dictionary<XPlaneAircraftType, WorldTrafficAircraftType>();
            _typeMapping[XPlaneAircraftCategory.D][XPlaneAircraftType.HeavyJet] = WorldTrafficAircraftType.HeavyJet;
            _typeMapping[XPlaneAircraftCategory.D][XPlaneAircraftType.TurboProp] = WorldTrafficAircraftType.LargeProp;

            _typeMapping[XPlaneAircraftCategory.E] = new Dictionary<XPlaneAircraftType, WorldTrafficAircraftType>();
            _typeMapping[XPlaneAircraftCategory.E][XPlaneAircraftType.HeavyJet] = WorldTrafficAircraftType.HeavyJet;

            _typeMapping[XPlaneAircraftCategory.F] = new Dictionary<XPlaneAircraftType, WorldTrafficAircraftType>();
            _typeMapping[XPlaneAircraftCategory.F][XPlaneAircraftType.HeavyJet] = WorldTrafficAircraftType.SuperHeavy;
        }

        public static IEnumerable<XPlaneAircraftType> XPlaneTypesFromStrings(string [] stringTypes)
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

        public static IEnumerable<WorldTrafficAircraftType> WTTypesFromXPlaneLimits(XPlaneAircraftCategory minCategory, XPlaneAircraftCategory maxCategory, IEnumerable<XPlaneAircraftType> xPlaneTypes = null)
        {
            List<WorldTrafficAircraftType> wtTypes = new List<WorldTrafficAircraftType>();

            for (XPlaneAircraftCategory cat = minCategory; cat <= maxCategory; cat++)
            {
                if (!_typeMapping.ContainsKey(cat))
                    continue;

                if (xPlaneTypes != null)
                {
                    foreach (XPlaneAircraftType xplaneType in xPlaneTypes)
                    {
                        if (!_typeMapping[cat].ContainsKey(xplaneType))
                            continue;

                        wtTypes.Add(_typeMapping[cat][xplaneType]);
                    }
                }
                else
                {
                    foreach (var vcvx in _typeMapping[cat])
                    {
                        wtTypes.Add(vcvx.Value);
                    }
                }
            }
            return wtTypes.Distinct();
        }
    }
}
