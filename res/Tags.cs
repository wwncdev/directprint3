using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tags
{
    public class Tag
    {
        public string barcodeData;
        public string description;
        public long edge1Time;
        public long edge2Time;
        public long readArrivedTime;
        // add the time that readstring arrived was called.

        public Tag(string barcodeData, long edge1Time, long edge2Time, long readArrivedTime)
        {
            this.barcodeData = barcodeData;
            this.edge1Time = edge1Time;
            this.edge2Time = edge2Time;
            this.readArrivedTime = readArrivedTime;
        }

        public long Elapsed()
        {
            return (edge2Time - edge1Time);
        }
    }

}
