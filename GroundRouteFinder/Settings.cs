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
                RegistryKey key = OpenReg();
                string val = key.GetValue("XPlaneLocation") as string;
                key.Close();
                return val != null ? val : "";
            }

            set
            {
                RegistryKey key = OpenReg(); 
                key.SetValue("XPlaneLocation", value);
                key.Close();
            }
        }

        public static bool OverwriteInbound
        {
            get
            {
                RegistryKey key = OpenReg();
                bool val = (int)key.GetValue("OverwriteInbound", 0) == 0 ? false : true;
                key.Close();
                return val;
            }

            set
            {
                RegistryKey key = OpenReg(); 
                key.SetValue("OverwriteInbound", value, RegistryValueKind.DWord);
                key.Close();
            }
        }

        public static bool OverwriteOutbound
        {
            get
            {
                RegistryKey key = OpenReg();
                bool val = (int)key.GetValue("OverwriteOutbound", 0) == 0 ? false : true;
                key.Close();
                return val;
            }

            set
            {
                RegistryKey key = OpenReg();
                key.SetValue("OverwriteOutbound", value, RegistryValueKind.DWord);
                key.Close();
            }
        }

        public static bool OverwriteParkingDefs
        {
            get
            {
                RegistryKey key = OpenReg();
                bool val = (int)key.GetValue("OverwriteParkingDefs", 0) == 0 ? false : true;
                key.Close();
                return val;
            }

            set
            {
                RegistryKey key = OpenReg();
                key.SetValue("OverwriteParkingDefs", value, RegistryValueKind.DWord);
                key.Close();
            }
        }

        public static bool OverwriteOperations
        {
            get
            {
                RegistryKey key = OpenReg();
                bool val = (int)key.GetValue("OverwriteOperations", 0) == 0 ? false : true;
                key.Close();
                return val;
            }

            set
            {
                RegistryKey key = OpenReg();
                key.SetValue("OverwriteOperations", value, RegistryValueKind.DWord);
                key.Close();
            }
        }

        public static bool GenerateDebugOutput
        {
            get
            {
                RegistryKey key = OpenReg();
                bool val = (int)key.GetValue("GenerateDebugOutput", 0) == 0 ? false : true;
                key.Close();
                return val;
            }

            set
            {
                RegistryKey key = OpenReg();
                key.SetValue("GenerateDebugOutput", value, RegistryValueKind.DWord);
                key.Close();
            }
        }

        public static int MaxSteerpoints
        {
            get
            {
                //RegistryKey key = OpenReg();
                //int val = (int)key.GetValue("MaxSteerpoints", 127);
                //key.Close();
                //return val;
                return 127;
            }

            set
            {
                RegistryKey key = OpenReg();
                key.SetValue("MaxSteerpoints", value, RegistryValueKind.DWord);
                key.Close();
            }
        }


        public static string WorldTrafficLocation { get { return Path.Combine(XPlaneLocation, @"ClassicJetSimUtils\WorldTraffic"); }  }
        public static string WorldTrafficGroundRoutes { get { return Path.Combine(XPlaneLocation, @"ClassicJetSimUtils\WorldTraffic\GroundRoutes"); } }
        public static string WorldTrafficParkingDefs { get { return Path.Combine(XPlaneLocation, @"ClassicJetSimUtils\WorldTraffic\ParkingDefs"); } }
        public static string WorldTrafficOperations { get { return Path.Combine(XPlaneLocation, @"ClassicJetSimUtils\WorldTraffic\AirportOperations"); } }

        private static RegistryKey OpenReg()
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

            string kmlFolder = Path.Combine(DataFolder, "KML");
            if (!Directory.Exists(kmlFolder))
                Directory.CreateDirectory(kmlFolder);

            DepartureFolderKML = Path.Combine(kmlFolder, "Departure");
            ArrivalFolderKML = Path.Combine(kmlFolder, "Arrival");

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
            if (!Directory.Exists(target_dir))
                return;

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
