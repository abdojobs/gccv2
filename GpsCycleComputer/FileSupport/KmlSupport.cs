﻿using System;

using System.Globalization;
using System.IO;
using System.Windows.Forms;
using GpsCycleComputer;
using GpsUtils;
using System.Xml;

namespace GpsSample.FileSupport
{
    class KmlSupport : IFileSupport
    {
        CultureInfo IC = CultureInfo.InvariantCulture;

#if false
        public bool Load(string filename, ref Form1.WayPointInfo WayPoints,
            int vector_size, ref float[] dataLat, ref float[] dataLong, ref int[] dataT, out int data_size)
        {
            int Counter = 0;
            bool Status = false;

            data_size = 0;
            WayPoints.WayPointCount = 0;        // No Waypoint Support for KML files yet.
            // set "." as decimal separator for reading the map info
            NumberFormatInfo number_info = new NumberFormatInfo();
            number_info.NumberDecimalSeparator = ".";

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(fs);
                string line = "";

                bool data_load_activated = false;
                bool coordinates_found = false;

                while (true)
                {
                    // read a single word (if we detected the required section), or a whole line
                    if (data_load_activated)
                    {
                        line = LoadWord(ref sr);
                        if (line == null) break;        //EndOfStream

                        // skip blank lines. Look for "<coordinates>" to trigger load values.
                        if (line == "") { continue; }
                        if (line.IndexOf("<coordinates>") >= 0) { coordinates_found = true; }
                        if (line.IndexOf("</coordinates>") >= 0) { data_load_activated = false; }

                        line = line.Replace("<coordinates>", "");
                        line = line.Replace("</coordinates>", "");
                        line = line.Trim();

                        // OK, read the values (lat and long)
                        if (coordinates_found && (line != ""))
                        {
                            string[] words = line.Split(new Char[] { ',' });
                            if (words.Length >= 2)
                            {
                                double tmp_long = Convert.ToDouble(words[0].Trim(), number_info);
                                double tmp_lat = Convert.ToDouble(words[1].Trim(), number_info);

                                // check if we need to decimate arrays
                                if (Counter >= vector_size)
                                {
                                    for (int i = 0; i < vector_size / 2; i++)
                                    {
                                        dataLat[i] = dataLat[i * 2];
                                        dataLong[i] = dataLong[i * 2];
                                    }
                                    Counter /= 2;
                                }

                                dataLat[Counter] = (float)tmp_lat;
                                dataLong[Counter] = (float)tmp_long;
                                dataT[Counter] = 0; // note time is not availble in KML
                                Counter++;
                            }
                        }
                    }
                    else
                    {
                        line = sr.ReadLine();
                        if (line == null) break;        //EndOfStream

                        line = line.Trim();
                        if (line == "</kml>") { break; }
                        else if ((line.StartsWith("<LineString>")) || (line.StartsWith("<Point>")))
                        {
                            data_load_activated = true; coordinates_found = false; continue;
                        }
                        else if ((line.StartsWith("</LineString>")) || (line.StartsWith("</Point>")))
                        {
                            data_load_activated = false; coordinates_found = false; continue;
                        }
                    }
                }
                sr.Close();
                fs.Close();

                data_size = Counter;
                Status = true;
            }
            catch (Exception e)
            {
                Utils.log.Error(" LoadKml ", e);
            }
            Cursor.Current = Cursors.Default;
            return Status;
        }
#else

        public bool Load(string filename, ref Form1.WayPointInfo WayPoints,
                    int vector_size, ref float[] dataLat, ref float[] dataLong, ref Int16[] dataZ, ref Int32[] dataT, ref Int32[] dataD, ref Form1.TrackStatistics ts, out int data_size)
        {
            bool Status = false;
            data_size = 0;
            WayPoints.WayPointCount = 0;
            ts.Clear();

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreWhitespace = true;
                StreamReader sr = new StreamReader(filename, System.Text.Encoding.UTF8);        //use StreamReader to overwrite encoding ISO-8859-1, which is not supported by .NETCF (no speed drawback)
                XmlReader reader = XmlReader.Create(sr, settings);
                //reader.MoveToContent();
                
                //reader.Read();
                while (reader.ReadToFollowing("Placemark"))
                {
                    string tmpname = null;
                    string tmpdescription = null;

                    reader.Read();
                    while (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "name")
                        {
                            tmpname = reader.ReadElementString();       //prepared for later use
                        }
                        else if (reader.Name == "description")
                        {
                            tmpdescription = reader.ReadElementString();       //prepared for later use
                        }
                        else if (reader.Name == "LineString")
                        {
                            reader.Read();
                            while (reader.NodeType == XmlNodeType.Element)
                            {
                                if (reader.Name == "coordinates")
                                {
                                    reader.ReadStartElement();
                                    while (true)
                                    {
                                        string line = "";
                                        char[] buf = new char[1];
                                        int count = 1;
                                        for (int i = 0; i < 100; i++)       //read one line
                                        {
                                            count = reader.ReadValueChunk(buf, 0, 1);
                                            if (buf[0] == '\n' || buf[0] == ' ' || buf[0] == '\r' || buf[0] == '\t' || count == 0)
                                                break;
                                            line += buf[0];
                                        }
                                        if (data_size >= vector_size)     // check if we need to decimate arrays
                                        {
                                            for (int i = 0; i < vector_size / 2; i++)
                                            {
                                                dataLat[i] = dataLat[i * 2];
                                                dataLong[i] = dataLong[i * 2];
                                                dataT[i] = dataT[i * 2];
                                            }
                                            data_size = vector_size / 2;
                                        }
                                        
                                        string[] numbers = line.Split(',');
                                        if (numbers.Length >= 2)            //read data
                                        {
                                            dataLong[data_size] = (float)Convert.ToDouble(numbers[0], IC);
                                            dataLat[data_size] = (float)Convert.ToDouble(numbers[1], IC);
                                            dataT[data_size] = 0;           //clear data in case there is no <time> field
                                            data_size++;
                                        }
                                        if (count == 0)
                                            break;
                                    }
                                    reader.Skip();      //advances reader to the next (End) Element
                                }
                                else
                                    reader.Skip();
                            }
                            reader.ReadEndElement();
                        }
                        else if (reader.Name == "Point")
                        {
                            WayPoints.name[WayPoints.WayPointCount] = tmpname;
                            reader.Read();
                            while (reader.NodeType == XmlNodeType.Element)
                            {
                                if (reader.Name == "coordinates")
                                {
                                    string line = reader.ReadString();
                                    string[] numbers = line.Split(',');
                                    if (numbers.Length >= 2)
                                    {
                                        WayPoints.lon[WayPoints.WayPointCount] = (float)Convert.ToDouble(numbers[0], IC);
                                        WayPoints.lat[WayPoints.WayPointCount] = (float)Convert.ToDouble(numbers[1], IC);
                                        WayPoints.WayPointCount++;
                                    }
                                }
                                else
                                    reader.Skip();
                            }
                            reader.ReadEndElement();
                        }
                        else
                            reader.Skip();
                    }
                    reader.ReadEndElement();    // /LineString or ..
                    //reader.ReadEndElement();    // /Placemark   do not read here, because ReadToFollowing would not find consecutive Placemark if it is also on it
                }

                reader.Close();
                Status = true;
            }
            catch (Exception e)
            {
                Utils.log.Error(" LoadKml ", e);
            }
            Cursor.Current = Cursors.Default;
            return Status;
        }

#endif

        // read one word
        public string LoadWord(ref StreamReader sr)
        {
            string word = "";
            int character;
            try
            {
                while (true)
                {
                    character = sr.Read();
                    if (character == -1) return null;

                    char c = (char)character;
                    if ((c != ' ') && (c != '\t') && (c != '\r') && (c != '\n'))
                    { word += c; }
                    else
                    {
                        // break if any chars has been loaded, otherwise simply skip the blanks
                        if (word != "") { break; }
                    }
                }
            }
            catch (Exception e)
            {
                Utils.log.Error(" LoadWord ", e);
            }

            return word;
        }


        public void Write(String kml_file, int CheckPointCount,
            Form1.CheckPointInfo[] CheckPoints,
            float[] PlotLat, float[] PlotLong, int PlotCount,
            short[] PlotS, int[] PlotT, short[] PlotZ, DateTime StartTime,
            ComboBox comboBoxKmlOptColor, CheckBox checkKmlAlt,
            string distUnit, string speedUnit, string altUnit, string exstop_info,
            string dist, string speedCur, string speed_avg, string speedMax, string run_time_label, string last_sample_time, string altitude, string battery,
            double StartLat, double StartLong,
            ComboBox comboBoxKmlOptWidth
            )
        {
            FileStream fs = null;
            StreamWriter wr = null;
            try
            {
                fs = new FileStream(kml_file, FileMode.Create);
                wr = new StreamWriter(fs);

                // write KML header
                wr.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                wr.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");

                wr.WriteLine(" <Document>");
                wr.WriteLine("  <name><![CDATA[" + StartTime + "]]></name>");

                // Write the checkpoints
                if (CheckPointCount != 0)
                {
                    wr.WriteLine("  <Folder> <name>Waypoints</name>");
                    for (int chk = 0; chk < CheckPointCount; chk++)
                    {
                        // need to replave chars not supported by XML
                        string chkName = CheckPoints[chk].name.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");

                        wr.WriteLine("  <Placemark><name>" + chkName
                                    + "</name><Point><altitudeMode>clampToGround</altitudeMode><coordinates>"
                                    + CheckPoints[chk].lon.ToString("0.##########", IC)
                                    + ","
                                    + CheckPoints[chk].lat.ToString("0.##########", IC)
                                    + ",0.000000</coordinates></Point></Placemark>");
                    }
                    wr.WriteLine("  </Folder>");
                }

                wr.WriteLine(" <Folder> <name>Tracks</name>");
                wr.WriteLine("  <Placemark>");
                wr.WriteLine("    <name>" + StartTime + "</name>");

                wr.WriteLine("    <Style id=\"yellowLineGreenPoly\">");
                wr.WriteLine("      <LineStyle>");

                // Colors : blue - red - green- yellow- white - black
                if (comboBoxKmlOptColor.SelectedIndex == 0) { wr.WriteLine("        <color>ffff0000</color>"); }
                else if (comboBoxKmlOptColor.SelectedIndex == 1) { wr.WriteLine("        <color>ff0000ff</color>"); }
                else if (comboBoxKmlOptColor.SelectedIndex == 2) { wr.WriteLine("        <color>ff00ff00</color>"); }
                else if (comboBoxKmlOptColor.SelectedIndex == 3) { wr.WriteLine("        <color>ff00ffff</color>"); }
                else if (comboBoxKmlOptColor.SelectedIndex == 4) { wr.WriteLine("        <color>ffffffff</color>"); }
                else if (comboBoxKmlOptColor.SelectedIndex == 5) { wr.WriteLine("        <color>ff000000</color>"); }
                else if (comboBoxKmlOptColor.SelectedIndex == 6) { wr.WriteLine("        <color>ffc0c0c0</color>"); }
                else if (comboBoxKmlOptColor.SelectedIndex == 7) { wr.WriteLine("        <color>ff0080ff</color>"); }
                else if (comboBoxKmlOptColor.SelectedIndex == 8) { wr.WriteLine("        <color>ffff8000</color>"); }
                else if (comboBoxKmlOptColor.SelectedIndex == 9) { wr.WriteLine("        <color>ff000080</color>"); }
                else if (comboBoxKmlOptColor.SelectedIndex == 10) { wr.WriteLine("        <color>ff800080</color>"); }
                else if (comboBoxKmlOptColor.SelectedIndex == 11) { wr.WriteLine("        <color>ffff0080</color>"); }

                wr.WriteLine("        <width>" + ((comboBoxKmlOptWidth.SelectedIndex + 1) * 2) + "</width>");

                wr.WriteLine("      </LineStyle>");
                wr.WriteLine("      <PolyStyle>");
                wr.WriteLine("        <color>7f00ff00</color>");
                wr.WriteLine("      </PolyStyle>");
                wr.WriteLine("    </Style>");

                wr.WriteLine("      <description>");

                // write description for this trip
                wr.WriteLine(dist + " " + distUnit + " " + run_time_label + " " + exstop_info);
                wr.WriteLine(speedCur + " " + speed_avg + " " + speedMax + " " + speedUnit);
                wr.WriteLine("battery " + battery);

                wr.WriteLine("	</description>");
                wr.WriteLine("      <styleUrl>#yellowLineGreenPoly</styleUrl>");

                wr.WriteLine("	    <LookAt>");
                wr.WriteLine("			<longitude>" + StartLong.ToString("0.##########", IC) + "</longitude>");
                wr.WriteLine("			<latitude>" + StartLat.ToString("0.##########", IC) + "</latitude>");
                wr.WriteLine("			<altitude>0</altitude>");
                wr.WriteLine("			<range>3000</range>");
                wr.WriteLine("			<tilt>0</tilt>");
                wr.WriteLine("			<heading>0</heading>");
                wr.WriteLine("		</LookAt>");

                wr.WriteLine("      <LineString>");
                if (checkKmlAlt.Checked) { wr.WriteLine("      <altitudeMode>absolute</altitudeMode>"); }
                wr.WriteLine("        <coordinates>");

                // here write coordinates
                for (int i = 0; i < PlotCount; i++)
                {
                    if (checkKmlAlt.Checked && PlotZ[i] != Int16.MinValue)      //ignore invalid value
                    {
                        wr.WriteLine(PlotLong[i].ToString("0.##########", IC) + "," + PlotLat[i].ToString("0.##########", IC) + "," + PlotZ[i]);
                    }
                    else
                    {
                        wr.WriteLine(PlotLong[i].ToString("0.##########", IC) + "," + PlotLat[i].ToString("0.##########", IC));
                    }
                }

                // write end of the KML file
                wr.WriteLine("        </coordinates>");
                wr.WriteLine("      </LineString>");
                wr.WriteLine("    </Placemark>");
                wr.WriteLine("   </Folder>");
                wr.WriteLine(" </Document>");
                wr.WriteLine("</kml>");

            }
            catch (Exception ee)
            {
                Utils.log.Error(" buttonSaveKML_Click", ee);
            }
            finally
            {
                if (wr != null) wr.Close();
                if (fs != null) fs.Close();
            }
        }
    }
}
