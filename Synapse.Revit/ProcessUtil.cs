using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Revit
{
    public static class ProcessUtil
    {
        internal static Process GetProcessById(int id)
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
        
        internal static Process StartProcess(string processPath)
        {
            // execute the browser window process
            Process process = new Process();
            process.StartInfo.FileName = processPath;
            //process.StartInfo.Arguments = portNumber.ToString(); // pass the gRPC port number to the process as a command line argument

            process.Start();
            
            return process;
        }

        internal static bool ActivateProcessAndMakeForeground(Process p)
        {
            if (p == null)
            {
                throw new SynapseRevitException("Process is null.");
            }

            IntPtr windowHandle = p.MainWindowHandle;
            return SetForegroundWindow(windowHandle);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);


    }
}
