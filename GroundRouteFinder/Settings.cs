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
        public static string DepartureFolderKML = "";
        public static string ArrivalFolderKML = "";

        private static string _xplaneLocation = null;
        public static string XPlaneLocation
        {
            get { return getValue("XPlaneLocation", ref _xplaneLocation, ""); }
            set { setValue("XPlaneLocation", ref _xplaneLocation, value); }
        }

        private static bool? _overwriteInbound;
        public static bool OverwriteInbound
        {
            get { return getBool("OverwriteInbound", ref _overwriteInbound); }
            set { setBool("OverwriteInbound", ref _overwriteInbound, value); }
        }

        private static bool? _overwriteOutbound;
        public static bool OverwriteOutbound
        {
            get { return getBool("OverwriteOutbound", ref _overwriteOutbound); }
            set { setBool("OverwriteOutbound", ref _overwriteOutbound, value); }
        }

        private static bool? _overwriteParkingDefs;
        public static bool OverwriteParkingDefs
        {
            get { return getBool("OverwriteParkingDefs", ref _overwriteParkingDefs); }
            set { setBool("OverwriteParkingDefs", ref _overwriteParkingDefs, value); }
        }

        private static bool? _overwriteOperations;
        public static bool OverwriteOperations
        {
            get { return getBool("OverwriteOperations", ref _overwriteOperations); }
            set { setBool("OverwriteOperations", ref _overwriteOperations, value); }
        }

        private static bool? _generateDebugOutput;
        public static bool GenerateDebugOutput
        {
            get { return getBool("GenerateDebugOutput", ref _generateDebugOutput); }
            set { setBool("GenerateDebugOutput", ref _generateDebugOutput, value); }
        }

        private static int? _maxSteerPoints;
        public static int MaxSteerpoints
        {
            get { return getValue("MaxSteerpoints", ref _maxSteerPoints, 127); }
            set { setValue("MaxSteerpoints", ref _maxSteerPoints, value); }
        }

        private static bool? _fixDuplicateParkingNames;
        public static bool FixDuplicateParkingNames
        {
            get { return getBool("FixDuplicateParkingNames", ref _fixDuplicateParkingNames); }
            set { setBool("FixDuplicateParkingNames", ref _fixDuplicateParkingNames, value); }
        }

        private static int? _parkingReference;
        public static int ParkingReference
        {
            get { return getValue("ParkingReference", ref _parkingReference, (int)WorldTrafficParkingReference.NoseWheel); }
            set { setValue("ParkingReference", ref _parkingReference, value); }
        }


        private static bool getBool(string name, ref bool? storage)
        {
            if (storage.HasValue) return storage.Value;

            using (RegistryKey key = OpenReg())
            {
                storage = (int)key.GetValue(name, 0) == 0 ? false : true;
                key.Close();
            }
            return storage.Value;
        }

        private static void setBool(string name, ref bool? storage, bool value)
        {
            if (storage.HasValue && storage.Value == value)
                return;

            using (RegistryKey key = OpenReg())
            {
                storage = value;
                key.SetValue(name, value, RegistryValueKind.DWord);
                key.Close();
            }
        }

        private static T getValue<T>(string name, ref T storage, T fallback) where T : class
        {
            if (storage != null) return storage;

            using (RegistryKey key = OpenReg())
            {
                storage = (T)key.GetValue(name, fallback);
                key.Close();
            }
            return storage;
        }

        private static void setValue<T>(string name, ref T storage, T value) where T : class
        {
            if (storage != null && EqualityComparer<T>.Default.Equals(storage, value))
                return;

            using (RegistryKey key = OpenReg())
            {
                storage = value;

                if (storage is string) // probably the only actual valid and usable case
                    key.SetValue(name, value, RegistryValueKind.String);
                else
                    key.SetValue(name, value, RegistryValueKind.DWord);

                key.Close();
            }
        }

        private static T getValue<T>(string name, ref T? storage, T fallback) where T : struct
        {
            if (storage.HasValue) return storage.Value;

            using (RegistryKey key = OpenReg())
            {
                storage = (T)key.GetValue(name, fallback);
                key.Close();
            }
            return storage.Value;
        }

        private static void setValue<T>(string name, ref T? storage, T value) where T : struct
        {
            if (storage.HasValue && EqualityComparer<T>.Default.Equals(storage.Value, value))
                return;

            using (RegistryKey key = OpenReg())
            {
                storage = value;
                key.SetValue(name, value, RegistryValueKind.DWord);
                key.Close();
            }
        }


        public static string WorldTrafficLocation { get { return Path.Combine(XPlaneLocation, "ClassicJetSimUtils", "WorldTraffic"); }  }
        public static string WorldTrafficGroundRoutes { get { return Path.Combine(XPlaneLocation, "ClassicJetSimUtils", "WorldTraffic", "GroundRoutes"); } }
        public static string WorldTrafficParkingDefs { get { return Path.Combine(XPlaneLocation, "ClassicJetSimUtils", "WorldTraffic", "ParkingDefs"); } }
        public static string WorldTrafficOperations { get { return Path.Combine(XPlaneLocation, "ClassicJetSimUtils", "WorldTraffic", "AirportOperations"); } }

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
