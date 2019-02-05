using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using GroundRouteFinder.AptDat;

namespace GroundRouteFinder
{
    public partial class Form1 : Form
    {
        private Airport _airport;
        private DateTime _start;

        public Form1()
        {
            InitializeComponent();
            Setup();
        }

        public void Setup()
        {
            setXPlaneLocation();
        }

        private void setXPlaneLocation()
        {
            txtXplaneLocation.Text = Settings.XPlaneLocation;
            if (!isXPlaneLocationOk())
            {
                tabControl1.SelectTab(1);
                txtXplaneLocation.BackColor = Color.Red;
            }
            else
            {
                txtXplaneLocation.BackColor = Color.White;
            }
        }

        private bool isXPlaneLocationOk()
        {
            if (!Directory.Exists(Settings.XPlaneLocation))
                return false;

            if (!File.Exists(Path.Combine(Settings.XPlaneLocation, "X-Plane.exe")))
                return false;

            return true;
        }

        private void logElapsed(string message = "")
        {
            rtb.AppendText($"{(DateTime.Now - _start).TotalSeconds:0000.000} {message}\n");
            rtb.Update();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _start = DateTime.Now;
            rtb.Clear();

            _airport = new Airport();
            _airport.LogMessage += _airport_LogMessage;
            _airport.Load("..\\..\\..\\..\\LFPG_Scenery_Pack\\LFPG_Scenery_Pack\\Earth nav data\\apt.dat");
            //_airport.Load("..\\..\\..\\..\\EHAM_Scenery_Pack\\EHAM_Scenery_Pack\\Earth nav data\\apt.dat");
            //_airport.Load("..\\..\\..\\..\\EIDW_Scenery_Pack\\EIDW_Scenery_Pack\\Earth nav data\\apt.dat");
            logElapsed("loading done");

            _airport.WriteParkingDefs();
            logElapsed($"parking defs done");

            _airport.FindOutboundRoutes(rbNormal.Checked);
            logElapsed($"outbound done, max steerpoints {OutboundResults.MaxOutPoints}");

            _airport.FindInboundRoutes(rbNormal.Checked);
            logElapsed($"inbound done, max steerpoints {InboundResults.MaxInPoints}");



        }

        private void _airport_LogMessage(object sender, LogEventArgs e)
        {
            logElapsed(e.Message);
        }

        private static char[] _splitters = { ' ' };

        private bool scanAptDat(string filename, string forIcao)
        {
            //logElapsed($"scanning {filename}");
            bool found = false;

            StreamReader sr = File.OpenText(filename);
            StreamWriter sw = File.CreateText($".\\{forIcao}.dat");
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (!found && line.StartsWith("1 "))
                {
                    string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens[4] == forIcao)
                    {
                        logElapsed("found\n");
                        found = true;
                    }
                }

                if (found)
                {
                    sw.WriteLine(line);
                    if (line.Length == 0)
                    {
                        break;
                    }
                }
            }

            sw.Close();
            sr.Close();
            return found;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            _start = DateTime.Now;
            rtb.Clear();
            bool found = false;

            string icao = txtIcao.Text.ToUpper();

            string customSceneries = Settings.XPlaneLocation + @"\Custom Scenery\scenery_packs.ini";
            string[] customs = File.ReadAllLines(customSceneries);
            foreach (string custom in customs)
            {
                string[] tokens = custom.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                    continue;

                if (tokens[0] == "SCENERY_PACK")
                {
                    string path = Path.Combine(Settings.XPlaneLocation, string.Join(" ", tokens.Skip(1)), "Earth nav data", "apt.dat").Replace("/", "\\");
                    if (!File.Exists(path))
                        continue;

                    found = scanAptDat(path, icao);
                    if (found)
                    {
                        logElapsed($"Found in {path}");
                        break;
                    }
                }
            }

            if (!found)
            {
                found = scanAptDat(Settings.XPlaneLocation + @"\Custom Scenery\Global Airports\Earth nav data\apt.dat", icao);
                if (found)
                {
                    logElapsed($"Found in default apt.dat");
                }
            }

            logElapsed("scan done");

            if (found)
            {
                _airport = new Airport();
                _airport.LogMessage += _airport_LogMessage;
                if (_airport.Analyze($".\\{icao}.dat", icao))
                {
                    logElapsed("airport has taxi nodes, routes and parkings");
                    _airport.preprocess();
                }
                else
                {
                    logElapsed("airport is missing taxi nodes, routes or parkings");
                }
            }

            string depFolder = Path.Combine(Settings.DepartureFolder, icao);
            if (Directory.Exists(depFolder))
            {
                if (Directory.EnumerateFiles(depFolder).Count() > 0)
                {
                    logElapsed("Departure ground routes exist");
                }
                else
                {
                    logElapsed("No departure ground routes exist");
                }
            }
            else
            {
                Directory.CreateDirectory(depFolder);
                logElapsed("No departure ground routes exist");
            }

            string arrFolder = Path.Combine(Settings.ArrivalFolder, icao);
            if (Directory.Exists(arrFolder))
            {
                if (Directory.EnumerateFiles(arrFolder).Count() > 0)
                {
                    logElapsed("Arrival ground routes exist");
                }
                else
                {
                    logElapsed("No Arrival ground routes exist");
                }
            }
            else
            {
                Directory.CreateDirectory(arrFolder);
                logElapsed("No Arrival ground routes exist");
            }

            string parkingFolder = Path.Combine(Settings.ParkingDefFolder, icao);
            if (Directory.Exists(parkingFolder))
            {
                if (Directory.EnumerateFiles(parkingFolder).Count() > 0)
                {
                    logElapsed("Parking Defs exist");
                }
                else
                {
                    logElapsed("No Parking Defs exist");
                }
            }
            else
            {
                Directory.CreateDirectory(parkingFolder);
                logElapsed("No parking defs exist");
            }

        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            _airport.WriteParkingDefs();
            logElapsed($"parking defs done");

            _airport.FindOutboundRoutes(rbNormal.Checked);
            logElapsed($"outbound done, max steerpoints {OutboundResults.MaxOutPoints}");

            _airport.FindInboundRoutes(rbNormal.Checked);
            logElapsed($"inbound done, max steerpoints {InboundResults.MaxInPoints}");


        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.ShowNewFolderButton = false;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    Settings.XPlaneLocation = fbd.SelectedPath;
                }
                setXPlaneLocation();
            }
        }

        private class AircraftBase
        {
            public double WingSpan;
            public double TakeOffDist;
            public double LandingDist;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string aircraftFolder = Settings.XPlaneLocation + @"\ClassicJetSimUtils\WorldTraffic\AircraftTypes";
            IEnumerable<string> baseAircraft = Directory.EnumerateFiles(aircraftFolder, "*_BASE.txt");
            rtbAircraft.Clear();
            rtbAircraft.AppendText($"Found {baseAircraft.Count()} 'base' aircraft.\n");

            int currentType = -1;

            Dictionary<int, List<AircraftBase>> aircraft = new Dictionary<int, List<AircraftBase>>();
            foreach (string basecraft in baseAircraft)
            {
                AircraftBase aircraftBase = null;
                string[] lines = File.ReadAllLines(basecraft);
                bool startFound = false;

                foreach (string line in lines)
                {
                    string[] tokens = line.ToLower().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length < 1)
                        continue;

                    if (!startFound)
                    {
                        if (tokens[0] != "start")
                        {
                            continue;
                        }
                        else
                        {
                            startFound = true;
                            continue;
                        }
                    }
                
                    switch (tokens[0])
                    {
                        case "type":
                            currentType = int.Parse(tokens[1]);
                            aircraftBase = new AircraftBase();
                            break;
                        case "wingspan":
                            aircraftBase.WingSpan = double.Parse(tokens[1]);
                            break;
                        case "takeoffdistatmtow":
                            aircraftBase.TakeOffDist = double.Parse(tokens[1]);
                            break;
                        case "landingdist":
                            aircraftBase.LandingDist = double.Parse(tokens[1]);
                            break;
                        default:
                            break;
                    }

                }

                if (!aircraft.ContainsKey(currentType))
                    aircraft.Add(currentType, new List<AircraftBase>());

                aircraft[currentType].Add(aircraftBase);
            }

            foreach (KeyValuePair<int, List<AircraftBase>> details in aircraft.OrderBy(ac => ac.Key))
            {
                double averageWingSpan = details.Value.Average(a => a.WingSpan);
                double minWingSpan = details.Value.Min(a => a.WingSpan);
                double maxWingSpan = details.Value.Max(a => a.WingSpan);

                rtbAircraft.AppendText($"{details.Key} Wingspan: mn {minWingSpan:0.0} <{spanToCat(minWingSpan)}> av {averageWingSpan:0.0} mx: {maxWingSpan:0.0} <{spanToCat(maxWingSpan)}>\n");
            }

            foreach (KeyValuePair<int, List<AircraftBase>> details in aircraft.OrderBy(ac => ac.Key))
            {
                double averageLength = details.Value.Average(a => a.TakeOffDist);
                double minLength = details.Value.Min(a => a.TakeOffDist);
                double maxLength = details.Value.Max(a => a.TakeOffDist);

                rtbAircraft.AppendText($"{details.Key} TakeOffDist: mn {minLength:0} av {averageLength:0} mx: {maxLength:0}\n");
            }

            foreach (KeyValuePair<int, List<AircraftBase>> details in aircraft.OrderBy(ac => ac.Key))
            {
                double averageLength = details.Value.Average(a => a.LandingDist);
                double minLength = details.Value.Min(a => a.LandingDist);
                double maxLength = details.Value.Max(a => a.LandingDist);

                rtbAircraft.AppendText($"{details.Key} LandingDist: mn {minLength:0} av {averageLength:0} mx: {maxLength:0}\n");
            }

        }

        private string spanToCat(double span)
        {
            if (span < 15)
                return "A";
            else if (span < 24)
                return "B";
            else if (span < 36)
                return "C";
            else if (span < 52)
                return "D";
            else if (span < 65)
                return "E";
            else //if (span < 24)
                return "F";

        }
    }
}
