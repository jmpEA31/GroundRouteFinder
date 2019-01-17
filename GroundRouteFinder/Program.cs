using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GroundRouteFinder
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            double la = 0, lo = 0;
            StartPoint.Intersection(51.8853 * Math.PI / 180.0, 0.2545 * Math.PI / 180.0, 108.547 * Math.PI / 180.0,
                                    49.0034 * Math.PI / 180.0, 2.5735 * Math.PI / 180.0, 32.435 * Math.PI / 180.0,
                                    ref la, ref lo);


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
