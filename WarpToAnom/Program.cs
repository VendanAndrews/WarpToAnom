using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Reflection;
using System.IO;

namespace ConsoleApplication1
{
    using DirectEve;
    using LavishScriptAPI;

    class Program
    {
        private static DirectEve _directEve;
        bool Finished = false;
        string State = "Open";
        private static DateTime _lastPulse;

        private static System.Collections.Generic.List<string> Difficulties = new System.Collections.Generic.List<string>();


        private void OpenScanner()
        {
            var scanner = _directEve.Windows.OfType<DirectScannerWindow>().FirstOrDefault();
            if (scanner == null)
            {
                _directEve.ExecuteCommand(DirectCmd.OpenScanner);
            }
        }
    
        private void DoSystemScan()
        {
            var scanner = _directEve.Windows.OfType<DirectScannerWindow>().FirstOrDefault();
            if (scanner != null & scanner.IsReady)
            {
                scanner.Analyze();
            }
        }

        private void WarpTo()
        {
            var scanner = _directEve.Windows.OfType<DirectScannerWindow>().FirstOrDefault();
            if (scanner != null & scanner.IsReady)
            {
                System.Collections.Generic.List<DirectSystemScanResult> Results = new System.Collections.Generic.List<DirectSystemScanResult>();

                foreach (string Difficulty in Difficulties)
                {
                    foreach (var result in scanner.SystemScanResults)
                    {
                        if (result.SignalStrength > 99)
                        {
                            if (result.TypeName.Contains(Difficulty))
                            {
                                Results.Add(result);                     
                            }
                        }
                    }
                }
                

                foreach (var result in Results)
                {
                    if (!Properties.Settings.Default.UsedScans.Contains(result.Id))
                    {
                        Properties.Settings.Default.UsedScans.Add(result.Id);
                        InnerSpaceAPI.InnerSpace.Echo("Warping to " + result.TypeName);
                        result.WarpTo();
                        return;
                    }
                }
                Properties.Settings.Default.UsedScans.Clear();

                Properties.Settings.Default.UsedScans.Add(Results[0].Id);
                InnerSpaceAPI.InnerSpace.Echo("Warping to " + Results[0].TypeName);
                Results[0].WarpTo();
          
            }            
        }

        private void CloseScanner()
        {
            var scanner = _directEve.Windows.OfType<DirectScannerWindow>().FirstOrDefault();
            if (scanner != null & scanner.IsReady)
            {
                scanner.Close();
            }

        }

        void OnFrame(object sender, EventArgs eventArgs)
        {

            if (State == "Open")
            {
                this.OpenScanner();
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
                this.DoSystemScan();
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
                    this.WarpTo();
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
                if (DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < 1500)
                {
                    return;
                }
                this.CloseScanner();
                State = "Finished";
                _lastPulse = DateTime.UtcNow;
                return;
            }
            this.Finished = true;
        }




        void Run()
        {

            try
            {
                _directEve = new DirectEve();
                if (_directEve.HasSupportInstances())
                {
                    _directEve.OnFrame += OnFrame;
                    while (!Finished)
                    {
                        System.Threading.Thread.Sleep(50);
                    }

                    _directEve.Dispose();
                }
                else
                {
                    InnerSpaceAPI.InnerSpace.Echo("You do not have any support instances, closing");
                    _directEve.Dispose();
                    return;
                }
            }
            catch
            {
                InnerSpaceAPI.InnerSpace.Echo("Something strange happened");
            }

        }



        static void Main(string[] args)
        {
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

            Program prog = new Program();
            prog.Run();

            Properties.Settings.Default.Save();
        }
    }
}
