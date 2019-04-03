using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.Output
{
    public class InvariantWriter : StreamWriter
    {
        public InvariantWriter(string path, Encoding encoding)
            : base(path, false, encoding)
        {
        }

        /// <summary>
        /// Use the InvariantCulture for writing files
        /// </summary>
        public override IFormatProvider FormatProvider
        {
            get
            {
                return CultureInfo.InvariantCulture;
            }
        }
    }
}
