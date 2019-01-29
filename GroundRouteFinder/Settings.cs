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
        public static string DepartureFolder = "";
        public static string ArrivalFolder = "";
        public static string ParkingDefFolder = "";

        public static string DepartureFolderKML = @"E:\GroundRoutes\Departure\";
        public static string ArrivalFolderKML = @"E:\GroundRoutes\Arrival\";

        static Settings()
        {
            if (Directory.Exists(@"X:\SteamLibrary\steamapps\common\X-Plane 11\ClassicJetSimUtils\WorldTraffic\GroundRoutes"))
            {
                DepartureFolder = @"X:\SteamLibrary\steamapps\common\X-Plane 11\ClassicJetSimUtils\WorldTraffic\GroundRoutes\Departure\";
                ArrivalFolder = @"X:\SteamLibrary\steamapps\common\X-Plane 11\ClassicJetSimUtils\WorldTraffic\GroundRoutes\Arrival\";
                ParkingDefFolder = @"X:\SteamLibrary\steamapps\common\X-Plane 11\ClassicJetSimUtils\WorldTraffic\ParkingDefs\";
            }
            else
            {
                DepartureFolder = @"E:\GroundRoutes\Departure\";
                ArrivalFolder = @"E:\GroundRoutes\Arrival\";
                ParkingDefFolder = @"E:\GroundRoutes\ParkingDefs\";
            }
        }

        public static void DeleteDirectoryContents(string target_dir, bool deleteDirAsWell = false)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectoryContents(dir, true);
            }

            if (deleteDirAsWell)
                Directory.Delete(target_dir, false);
        }

    }
}
