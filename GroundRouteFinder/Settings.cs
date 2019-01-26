using GroundRouteFinder.AptDat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public static class Settings
    {
        public static Dictionary<int, Runway.RunwayNodeUsage[]> SizeToUsage = new Dictionary<int, Runway.RunwayNodeUsage[]>();

        public static string DepartureFolder = "";
        public static string ArrivalFolder = "";

        static Settings()
        {
            SizeToUsage.Add(0, new Runway.RunwayNodeUsage[] { Runway.RunwayNodeUsage.ExitShort, Runway.RunwayNodeUsage.ExitReduced2 });
            SizeToUsage.Add(1, new Runway.RunwayNodeUsage[] { Runway.RunwayNodeUsage.ExitShort, Runway.RunwayNodeUsage.ExitReduced2 });
            SizeToUsage.Add(2, new Runway.RunwayNodeUsage[] { Runway.RunwayNodeUsage.ExitShort, Runway.RunwayNodeUsage.ExitReduced2, Runway.RunwayNodeUsage.ExitReduced1 });
            SizeToUsage.Add(3, new Runway.RunwayNodeUsage[] { Runway.RunwayNodeUsage.ExitReduced2, Runway.RunwayNodeUsage.ExitReduced1, Runway.RunwayNodeUsage.ExitMax });
            SizeToUsage.Add(4, new Runway.RunwayNodeUsage[] { Runway.RunwayNodeUsage.ExitReduced2, Runway.RunwayNodeUsage.ExitReduced1, Runway.RunwayNodeUsage.ExitMax });
            SizeToUsage.Add(5, new Runway.RunwayNodeUsage[] { Runway.RunwayNodeUsage.ExitReduced2, Runway.RunwayNodeUsage.ExitReduced1, Runway.RunwayNodeUsage.ExitMax });

            if (Directory.Exists(@"D:\SteamLibrary\steamapps\common\X-Plane 11\ClassicJetSimUtils\WorldTraffic\GroundRoutes"))
            {
                DepartureFolder = @"D:\SteamLibrary\steamapps\common\X-Plane 11\ClassicJetSimUtils\WorldTraffic\GroundRoutes\Departure\";
                ArrivalFolder = @"D:\SteamLibrary\steamapps\common\X-Plane 11\ClassicJetSimUtils\WorldTraffic\GroundRoutes\Arrival\";
            }
            else
            {
                DepartureFolder = @"E:\GroundRoutes\Departure\";
                ArrivalFolder = @"E:\GroundRoutes\Arrival\";
            }
        }

        public static List<int> XPlaneCategoryToWTType(char category)
        {
            if (category > 'z')
                category -= (char)('a' - 'A');

            return XPlaneCategoryToWTType(category - 'A');
        }

        public static List<int> XPlaneCategoryToWTType(int category)
        {
            List<int> wtTypes = new List<int>();

            switch (category)
            {
                case 0: // XPlane type A 'wingspan < 15'
                    wtTypes.AddRange(new int[] { 0, 7, 8, 9 }); // Fighter, Light Jet, Light Prop, Helicopter
                    break;
                case 1: // XPlane type B 'wingspan < 24'
                    wtTypes.AddRange(new int[] { 5, 6 }); // Medium Jet, Medium Prop
                    break;
                case 2: // XPlane type C 'wingspan < 36'
                    wtTypes.Add(3); // Large Jet
                    break;
                case 3: // XPlane type D 'wingspan < 52'
                    wtTypes.Add(4); // Large Prop
                    break;
                case 4: // XPlane type E 'wingspan < 65'
                    wtTypes.Add(2); // Heavy Jet
                    break;
                case 5: // XPlane type F 'wingspan < 80'
                default:
                    wtTypes.Add(1); // Supah Heavy Jet
                    break;
            }

            return wtTypes;
        }
    }
}
