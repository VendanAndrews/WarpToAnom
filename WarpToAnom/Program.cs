using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Reflection;
using System.IO;
using EveCom;
using InnerSpaceAPI;
using LavishScriptAPI;

namespace WarpToAnom
{
    class Program
    {
        static bool Finished = false;
        static string State = "Open";
        static DateTime _lastPulse;
        static public int Distance = 0;

        static System.Collections.Generic.List<string> Difficulties = new System.Collections.Generic.List<string>();

        static void OpenScanner()
        {
            if (Window.Scanner == null)
            {
                ScannerWindow.Open();
            }


        }

        static void DoSystemScan()
        {
            if (Window.Scanner != null)
            {
                Window.Scanner.Analyze();
            }
        }

        static void WarpTo()
        {
            if (Window.Scanner != null)
            {
                List<SystemScanResult> ScanResults = Window.Scanner.ScanResults.ToList();
                List<SystemScanResult> Results = new List<SystemScanResult>();

                foreach (string Difficulty in Difficulties)
                {
                    foreach (SystemScanResult result in ScanResults)
                    {
                        if (result.Certainty > .99 && !Properties.Settings.Default.UsedScans.Contains(result.ID))
                        {
                            InnerSpace.Echo("Certainty: " + result.Certainty + " Name: " + result.DungeonName + " ScanGroupID: " + result.ScanGroupID);
                            if (result.DungeonName.Contains(Difficulty))
                            {
                                Results.Add(result);
                            }
                        }
                    }
                }

                InnerSpace.Echo("Warping to " + Results[0].DungeonName + " at distance of " + Distance.ToString());
                Properties.Settings.Default.UsedScans.Add(Results[0].ID);

                if (Session.InFleet && Fleet.Members.First(member => member.ID == Me.CharID).Role != FleetRole.SquadMember)
                {
                    Results[0].WarpFleetTo(Distance);
                }
                else
                {
                    Results[0].WarpTo(Distance);
                }
            }
        }

        static void CloseScanner()
        {
            if (Window.Scanner != null)
            {
                Window.Scanner.Close();
            }

        }


        static void OnFrame(object sender, EventArgs eventArgs)
        {
            if (!Session.Safe || Session.NextSessionChange > Session.Now)
            {
                return;
            }
            if (State == "Open")
            {
                OpenScanner();
                State = "Scan";
                _lastPulse = DateTime.UtcNow;
                return;
            }
            if (State == "Scan")
            {
                if (DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < 1500)
                {
                    return;
                }
                DoSystemScan();
                State = "Warp";
                _lastPulse = DateTime.UtcNow;
                return;
            }
            if (State == "Warp")
            {
                if (DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < 15000)
                {
                    return;
                }
                try
                {
                    WarpTo();
                }
                catch
                {

                }
                State = "Close";
                _lastPulse = DateTime.UtcNow;
                return;
            }
            if (State == "Close")
            {
                if (DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < 10000)
                {
                    return;
                }
                CloseScanner();
                State = "Finished";
                _lastPulse = DateTime.UtcNow;
                return;
            }
            Finished = true;
        }

        static void Run()
        {

            EveCom.EVEFrame.OnFrame += OnFrame;

            while (!Finished)
            {
                System.Threading.Thread.Sleep(50);
            }

        }



        static void Main(string[] args)
        {
            InnerSpace.Echo("WarpToAnom Loaded");
            if (Properties.Settings.Default.UsedScans == null)
            {
                Properties.Settings.Default.UsedScans = new System.Collections.Specialized.StringCollection();
            }

            try
            {
                XmlTextReader reader = new XmlTextReader(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\WarpToAnom.xml");
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Text)
                    {
                        Difficulties.Add(reader.Value);
                    }
                }
                reader.Close();
            }
            catch
            {
                InnerSpaceAPI.InnerSpace.Echo("Problem reading WarpToAnom.xml");
            }

            try
            {
                Distance = int.Parse(args[0]);
            }
            catch { }
            /* 0
             * 10
             * 20
             * 30
             * 50
             * 70
             * 100
             */
            if (Distance < 5000)
            {
                Distance = 0;
            }
            else if(Distance < 15000)
            {
                Distance = 10000;
            }
            else if (Distance < 25000)
            {
                Distance = 20000;
            }
            else if (Distance < 35000)
            {
                Distance = 30000;
            }
            else if (Distance < 60000)
            {
                Distance = 50000;
            }
            else if (Distance < 85000)
            {
                Distance = 70000;
            }
            else
            {
                Distance = 100000;
            }

            Run();

            Properties.Settings.Default.Save();
        }
    }
}
