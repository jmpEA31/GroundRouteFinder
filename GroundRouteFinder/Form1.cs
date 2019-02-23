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
    public partial class MainForm : Form
    {
        private Airport _airport;
        private DateTime _start;

        private bool _hasExistingInboundRoutes;
        private bool _hasExistingOutboundRoutes;
        private bool _hasExistingParkingDefs;
        private bool _hasExistingAirportOperations;    

        public MainForm()
        {
            InitializeComponent();
            Setup();
        }

        public void Setup()
        {
            SetXPlaneLocation();
            btnGenerate.Enabled = false;

            cbxOwInboundDefault.Checked = Settings.OverwriteInbound;
            cbxOverwriteInboundRoutes.Checked = Settings.OverwriteInbound;

            cbxOwOutboundDefault.Checked = Settings.OverwriteOutbound;
            cbxOverwriteOutboundRoutes.Checked = Settings.OverwriteOutbound;

            cbxOwParkingDefsDefault.Checked = Settings.OverwriteParkingDefs;
            cbxOverwriteParkingDefs.Checked = Settings.OverwriteParkingDefs;

            cbxOwOperationsDefault.Checked = Settings.OverwriteOperations;
            cbxOverwriteAirportOperations.Checked = Settings.OverwriteOperations;

            cbxGenerateDebugFiles.Checked = Settings.GenerateDebugOutput;
        }

        private void SetXPlaneLocation()
        {
            txtXplaneLocation.Text = Settings.XPlaneLocation;
            if (!IsXPlaneLocationOk())
            {
                tabControl1.SelectTab(1);
                txtXplaneLocation.BackColor = Color.Red;
            }
            else
            {
                txtXplaneLocation.BackColor = Color.White;
            }
        }

        private bool IsXPlaneLocationOk()
        {
            if (!Directory.Exists(Settings.XPlaneLocation))
                return false;

            if (!File.Exists(Path.Combine(Settings.XPlaneLocation, "X-Plane.exe")))
                return false;

            return true;
        }

        private void LogElapsed(string message = "")
        {
            string elapsed = $"<{(DateTime.Now - _start).TotalSeconds:000.000}>".Replace(".", "s").Replace(",", "s");
            rtb.AppendText($"{elapsed} {message}\n");
            rtb.ScrollToCaret();
            rtb.Update();
        }

        private void _airport_LogMessage(object sender, LogEventArgs e)
        {
            LogElapsed(e.Message);
        }

        private static char[] _splitters = { ' ' };

        private void btnAnalyze_Click(object sender, EventArgs e)
        {
            try
            {
                _start = DateTime.Now;
                rtb.Clear();
                string icao = txtIcao.Text.ToUpper();

                bool found = ScanCustomSceneries(icao);
                if (found)
                {
                    _airport = new Airport();
                    if (!_airport.Analyze(Path.Combine(Settings.DataFolder, $"{icao}.tmp"), icao))
                    {
                        rtb.AppendText("Customized airport does not contain the minimal data requiered to generate routes (parkings + atc taxi network).\n");
                        found = false;
                        _airport = null;
                    }
                }

                if (!found)
                {
                    found = scanDefaultAptDat(icao);
                    if (found)
                    {
                        _airport = new Airport();
                        if (!_airport.Analyze(Path.Combine(Settings.DataFolder, $"{icao}.tmp"), icao))
                        {
                            rtb.AppendText("Default airport does not contain the minimal data requiered to generate routes (parkings + atc taxi network).\n");
                            found = false;
                            _airport = null;
                            return;
                        }
                    }
                }

                if (!found)
                {
                    return;
                }

                rtb.AppendText("Parkings and ATC taxi network are present.\n");

                var ps = _airport.Parkings.GroupBy(p => p.Name);
                foreach (var psv in ps)
                {
                    if (psv.Count() > 1)
                        rtb.AppendText($"WARNING Duplicate parking names in apt source: <{psv.First().Name}> occurs {psv.Count()} times.\n");
                }


                if (CheckWorldTrafficFolders(icao, out _hasExistingInboundRoutes, out _hasExistingOutboundRoutes, out _hasExistingParkingDefs, out _hasExistingAirportOperations))
                {
                    btnGenerate.Enabled = true;
                    cbxOverwriteAirportOperations.ForeColor = _hasExistingAirportOperations ? Color.Red : Color.Black;
                    cbxOverwriteInboundRoutes.ForeColor = _hasExistingInboundRoutes ? Color.Red : Color.Black;
                    cbxOverwriteOutboundRoutes.ForeColor = _hasExistingOutboundRoutes ? Color.Red : Color.Black;
                    cbxOverwriteParkingDefs.ForeColor = _hasExistingParkingDefs ? Color.Red : Color.Black;
                }
                else
                {
                    btnGenerate.Enabled = false;
                    cbxOverwriteAirportOperations.ForeColor = Color.Gray;
                    cbxOverwriteInboundRoutes.ForeColor = Color.Gray;
                    cbxOverwriteOutboundRoutes.ForeColor = Color.Gray;
                    cbxOverwriteParkingDefs.ForeColor = Color.Gray;
                }
            }
            catch (Exception ex)
            {
                rtb.AppendText($"\nAn exception occurred during analysis:\n{ex}");
            }
        }

        private bool CheckWorldTrafficFolders(string icao, out bool hasInboundRoutes, out bool hasOutboundRoutes, out bool hasParkingDefs, out bool hasAirportOperations)
        {
            hasInboundRoutes = false;
            hasOutboundRoutes = false;
            hasParkingDefs = false;
            hasAirportOperations = false;

            if (!Directory.Exists(Settings.WorldTrafficLocation))
            {
                rtb.AppendText("World Traffic folder not found.\n");
                return false;
            }

            if (!Directory.Exists(Path.Combine(Settings.WorldTrafficGroundRoutes, "Departure", icao)))
            {
                Directory.CreateDirectory(Path.Combine(Settings.WorldTrafficGroundRoutes, "Departure", icao));
            }
            else if (Directory.EnumerateFiles(Path.Combine(Settings.WorldTrafficGroundRoutes, "Departure", icao)).Count() != 0)
            {
                rtb.AppendText($"* Departure ground routes already exist for {icao}!\n");
                hasOutboundRoutes = true;
            }

            if (!Directory.Exists(Path.Combine(Settings.WorldTrafficGroundRoutes, "Arrival", icao)))
            {
                Directory.CreateDirectory(Path.Combine(Settings.WorldTrafficGroundRoutes, "Arrival", icao));
            }
            else if (Directory.EnumerateFiles(Path.Combine(Settings.WorldTrafficGroundRoutes, "Arrival", icao)).Count() != 0)
            {
                rtb.AppendText($"* Arrival ground routes already exist for {icao}!\n");
                hasInboundRoutes = true;
            }

            if (!Directory.Exists(Path.Combine(Settings.WorldTrafficParkingDefs, icao)))
            {
                Directory.CreateDirectory(Path.Combine(Settings.WorldTrafficParkingDefs, icao));
            }
            else if (Directory.EnumerateFiles(Path.Combine(Settings.WorldTrafficParkingDefs, icao)).Count() != 0)
            {
                rtb.AppendText($"* Parking definitions already exist for {icao}!\n");
                hasParkingDefs = true;
            }

            if (!Directory.Exists(Settings.WorldTrafficOperations))
            {
                Directory.CreateDirectory(Settings.WorldTrafficOperations);
            }
            else if (File.Exists(Path.Combine(Settings.WorldTrafficOperations, $"{icao}.txt")))
            {
                rtb.AppendText($"* Operations already exist for {icao}!\n");
                hasAirportOperations = true;
            }

            return true;
        }

        private bool ScanCustomSceneries(string icao)
        {
            bool found = false;
            string customSceneries = Settings.XPlaneLocation + @"\Custom Scenery\scenery_packs.ini";

            if (!File.Exists(customSceneries))
            {
                rtb.AppendText("scenery_packs.ini not found, customized airports will not be checked.\n");
                return false;
            }

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

                    found = ScanAirportFile(path, icao);
                    if (found)
                    {
                        rtb.AppendText($"{icao} found in {path}.\n");
                        break;
                    }
                }
            }
            return found;
        }

        private bool scanDefaultAptDat(string icao)
        {
            string defaultAptDat = Settings.XPlaneLocation + @"\Custom Scenery\Global Airports\Earth nav data\apt.dat";
            if (!File.Exists(defaultAptDat))
            {
                rtb.AppendText($"Default apt.dat not found. Unable to find {icao}.\n");
                return false;
            }

            bool found = ScanAirportFile(defaultAptDat, icao);
            if (found)
            {
                rtb.AppendText($"{icao} found in default apt.dat.\n");
            }
            else
            {
                rtb.AppendText($"{icao} not found in default apt.dat.\n");
            }
            return found;
        }

        private bool ScanAirportFile(string filename, string forIcao)
        {
            bool found = false;

            StreamReader sr = File.OpenText(filename);
            StreamWriter sw = File.CreateText(Path.Combine(Settings.DataFolder, $"{forIcao}.tmp"));
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (!found && line.StartsWith("1 "))
                {
                    string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens[4] == forIcao)
                    {
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


        private void btnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                rtb.Clear();
                _start = DateTime.Now;
                LogElapsed($"Starting generation.");

                _airport.LogMessage += _airport_LogMessage;
                _airport.Process();

                if (!rbNormal.Checked)
                {
                    if (!Directory.Exists(Path.Combine(Settings.DepartureFolderKML, _airport.ICAO)))
                    {
                        Directory.CreateDirectory(Path.Combine(Settings.DepartureFolderKML, _airport.ICAO));
                    }
                    if (!Directory.Exists(Path.Combine(Settings.ArrivalFolderKML, _airport.ICAO)))
                    {
                        Directory.CreateDirectory(Path.Combine(Settings.ArrivalFolderKML, _airport.ICAO));
                    }
                }

                if (rbNormal.Checked)
                {
                    if (!_hasExistingParkingDefs || cbxOverwriteParkingDefs.Checked)
                    {
                        int count = _airport.WriteParkingDefs();
                        LogElapsed($"parking defs done ({count} generated)");
                    }
                    else
                    {
                        LogElapsed($"not overwriting parking defs");
                    }
                }
                else
                {
                    LogElapsed($"skipping parkings defs in KML mode");
                }

                if (rbNormal.Checked)
                {
                    if (!_hasExistingAirportOperations || cbxOverwriteAirportOperations.Checked)
                    {
                        _airport.WriteOperations();
                        LogElapsed($"operations done");
                    }
                    else
                    {
                        LogElapsed($"not overwriting operations");
                    }
                }
                else
                {
                    LogElapsed($"skipping operations in KML mode");
                }


                if (!_hasExistingOutboundRoutes || cbxOverwriteOutboundRoutes.Checked)
                {
                    int count = _airport.FindOutboundRoutes(rbNormal.Checked);
                    LogElapsed($"outbound routes done, {count} generated, max steerpoints {OutboundResults.MaxOutPoints}");
                }
                else
                {
                    LogElapsed($"not overwriting outbound routes");
                }

                if (!_hasExistingInboundRoutes || cbxOverwriteInboundRoutes.Checked)
                {
                    int count = _airport.FindInboundRoutes(rbNormal.Checked);
                    LogElapsed($"inbound routes done, {count} generated, max steerpoints {InboundResults.MaxInPoints}");
                }
                else
                {
                    LogElapsed($"not overwriting inbound routes");
                }

                _airport.LogMessage -= _airport_LogMessage;
                LogElapsed($"done");

                if (!rbNormal.Checked)
                {
                    rtb.AppendText($"\nKML files can be found in:\n {Settings.ArrivalFolderKML} and\n {Settings.DepartureFolderKML}");
                }

                if (Settings.GenerateDebugOutput)
                {
                    _airport.DebugParkings();
                    rtb.AppendText($"Debug csv files can be found in: {Settings.DataFolder}");
                }
            }
            catch (Exception ex)
            {
                rtb.AppendText($"\nAn exception occurred during generation:\n{ex}");
            }
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
                SetXPlaneLocation();
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

                rtbAircraft.AppendText($"{details.Key} Wingspan: mn {minWingSpan:0.0} <{SpanToCat(minWingSpan)}> av {averageWingSpan:0.0} mx: {maxWingSpan:0.0} <{SpanToCat(maxWingSpan)}>\n");
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

        private string SpanToCat(double span)
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

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cbxOwInboundDefault_CheckedChanged(object sender, EventArgs e)
        {
            Settings.OverwriteInbound = cbxOwInboundDefault.Checked;
        }

        private void cbxOwOutboundDefault_CheckedChanged(object sender, EventArgs e)
        {
            Settings.OverwriteOutbound = cbxOwOutboundDefault.Checked;
        }

        private void cbxOwParkingDefsDefault_CheckedChanged(object sender, EventArgs e)
        {
            Settings.OverwriteParkingDefs = cbxOwParkingDefsDefault.Checked;
        }

        private void cbxOwOperationsDefault_CheckedChanged(object sender, EventArgs e)
        {
            Settings.OverwriteOperations = cbxOwOperationsDefault.Checked;
        }

        private void chkGenerateDebugFiles_CheckedChanged(object sender, EventArgs e)
        {
            Settings.GenerateDebugOutput = cbxGenerateDebugFiles.Checked;
        }
    }
}
