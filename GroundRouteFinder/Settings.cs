using GroundRouteFinder.AptDat;
using Microsoft.Win32;
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
        public static string DataFolder = "";

        public static string DepartureFolderKML = @"E:\GroundRoutes\Departure\";
        public static string ArrivalFolderKML = @"E:\GroundRoutes\Arrival\";

        public static string XPlaneLocation
        {
            get
            {
                RegistryKey key = openReg();
                string val = key.GetValue("XPlaneLocation") as string;
                key.Close();
                return val != null ? val : "";
            }

            set
            {
                RegistryKey key = openReg(); ;
                key.SetValue("XPlaneLocation", value);
                key.Close();
            }
        }

        public static string WorldTrafficLocation { get { return Path.Combine(XPlaneLocation, @"ClassicJetSimUtils\WorldTraffic"); }  }
        public static string WorldTrafficGroundRoutes { get { return Path.Combine(XPlaneLocation, @"ClassicJetSimUtils\WorldTraffic\GroundRoutes"); } }
        public static string WorldTrafficParkingDefs { get { return Path.Combine(XPlaneLocation, @"ClassicJetSimUtils\WorldTraffic\ParkingDefs"); } }

        private static RegistryKey openReg()
        {
            return Registry.CurrentUser.OpenSubKey(@"Software\Vortex\GRF", true);
        }

        static Settings()
        {
            // Setup and clean datafolder
            DataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vortex");
            if (!Directory.Exists(DataFolder))
                Directory.CreateDirectory(DataFolder);
            DataFolder = Path.Combine(DataFolder, "GRF");
            if (!Directory.Exists(DataFolder))
                Directory.CreateDirectory(DataFolder);

            IEnumerable<string> tmpFiles = Directory.EnumerateFiles(DataFolder, "*.tmp");
            foreach (string tmpFile in tmpFiles)
            {
                File.Delete(tmpFile);
            }

            // Setup registry
            RegistryKey software = Registry.CurrentUser.OpenSubKey("Software", true);
            RegistryKey vortex = software.OpenSubKey(@"Vortex\GRF", true);
            if (vortex == null)
            {
                vortex = software.CreateSubKey(@"Vortex\GRF", true);
            }
            vortex.Close();
            software.Close();
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
