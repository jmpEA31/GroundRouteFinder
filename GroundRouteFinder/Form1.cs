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
        }

        private void logElapsed(string message = "")
        {
            rtb.AppendText($"{(DateTime.Now - _start).TotalSeconds:00.000} {message}\n");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _start = DateTime.Now;
            rtb.Clear();

            _airport = new Airport();
            _airport.Load("..\\..\\..\\..\\LFPG_Scenery_Pack\\LFPG_Scenery_Pack\\Earth nav data\\apt.dat");
            //_airport.Load("..\\..\\..\\..\\EHAM_Scenery_Pack\\EHAM_Scenery_Pack\\Earth nav data\\apt.dat");
            //_airport.Load("..\\..\\..\\..\\EIDW_Scenery_Pack\\EIDW_Scenery_Pack\\Earth nav data\\apt.dat");
            logElapsed("loading done");

            _airport.FindInboundRoutes(rbNormal.Checked);
            logElapsed($"inbound done, max steerpoints {InboundResults.MaxInPoints}");

            _airport.FindOutboundRoutes(rbNormal.Checked);
            logElapsed($"outbound done, max steerpoints {OutboundResults.MaxOutPoints}");


        }
    }
}
