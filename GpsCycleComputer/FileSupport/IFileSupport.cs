﻿using System;

using System.Collections.Generic;
using System.Text;
using GpsCycleComputer;

namespace GpsSample.FileSupport
{
    interface IFileSupport
    {
        bool Load(string filename,
            ref Form1.WayPointInfo WayPoints,
            // allocated vector size (for dataX/Y/T)
            int vector_size,          
            // x and y realive to origin, in metres, t in sec ralative to start
            ref float[] dataLat, ref float[] dataLong, ref Int16[] dataZ, ref  Int32[] dataT, ref Int32[] dataD, ref Form1.TrackStatistics ts,
            out int data_size);
    }
}
