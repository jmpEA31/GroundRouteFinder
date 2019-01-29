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
        public static Dictionary<XPlaneAircraftCategory, Runway.RunwayNodeUsage[]> SizeToUsage = new Dictionary<XPlaneAircraftCategory, Runway.RunwayNodeUsage[]>();

        public static string DepartureFolder = "";
        public static string ArrivalFolder = "";

        public static string DepartureFolderKML = @"E:\GroundRoutes\Departure\";
        public static string ArrivalFolderKML = @"E:\GroundRoutes\Arrival\";

        static Settings()
        {
            SizeToUsage.Add(XPlaneAircraftCategory.A, new Runway.RunwayNodeUsage[] { Runway.RunwayNodeUsage.ExitShort, Runway.RunwayNodeUsage.ExitReduced2 });
            SizeToUsage.Add(XPlaneAircraftCategory.B, new Runway.RunwayNodeUsage[] { Runway.RunwayNodeUsage.ExitShort, Runway.RunwayNodeUsage.ExitReduced2 });
            SizeToUsage.Add(XPlaneAircraftCategory.C, new Runway.RunwayNodeUsage[] { Runway.RunwayNodeUsage.ExitShort, Runway.RunwayNodeUsage.ExitReduced2, Runway.RunwayNodeUsage.ExitReduced1 });
            SizeToUsage.Add(XPlaneAircraftCategory.D, new Runway.RunwayNodeUsage[] { Runway.RunwayNodeUsage.ExitReduced2, Runway.RunwayNodeUsage.ExitReduced1, Runway.RunwayNodeUsage.ExitMax });
            SizeToUsage.Add(XPlaneAircraftCategory.E, new Runway.RunwayNodeUsage[] { Runway.RunwayNodeUsage.ExitReduced2, Runway.RunwayNodeUsage.ExitReduced1, Runway.RunwayNodeUsage.ExitMax });
            SizeToUsage.Add(XPlaneAircraftCategory.F, new Runway.RunwayNodeUsage[] { Runway.RunwayNodeUsage.ExitReduced2, Runway.RunwayNodeUsage.ExitReduced1, Runway.RunwayNodeUsage.ExitMax });

            if (Directory.Exists(@"X:\SteamLibrary\steamapps\common\X-Plane 11\ClassicJetSimUtils\WorldTraffic\GroundRoutes"))
            {
                DepartureFolder = @"X:\SteamLibrary\steamapps\common\X-Plane 11\ClassicJetSimUtils\WorldTraffic\GroundRoutes\Departure\";
                ArrivalFolder = @"X:\SteamLibrary\steamapps\common\X-Plane 11\ClassicJetSimUtils\WorldTraffic\GroundRoutes\Arrival\";
            }
            else
            {
                DepartureFolder = @"E:\GroundRoutes\Departure\";
                ArrivalFolder = @"E:\GroundRoutes\Arrival\";
            }
        }
    }
}
