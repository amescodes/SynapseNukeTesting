using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Revit
{
    internal static class ProcessUtil
    {
        public static Process StartProcess(string processPath, int portNumber)
        {
            // execute the browser window process
            Process process = new Process();
            process.StartInfo.FileName = processPath;
            process.StartInfo.Arguments = portNumber.ToString(); // pass the gRPC port number to the process as a command line argument

            process.Start();
            
            return process;
        }
        
        public static bool ActivateProcessAndMakeForeground(Process p)
        {
            if (p == null)
            {
                throw new SynapseRevitException("Process is null.");
            }

            IntPtr windowHandle = p.MainWindowHandle;
            return SetForegroundWindow(windowHandle);
        }

        public static Process GetProcessById(int id)
        {
            Process[] processes = Process.GetProcesses();

            foreach (Process p in processes)
            {
                if (p.Id == id)
                {
                    return p;
                }
            }

            return null;
        }
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
