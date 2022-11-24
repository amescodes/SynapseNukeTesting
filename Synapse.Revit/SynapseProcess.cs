﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Revit
{
    public class SynapseProcess
    {
        private int processId;

        public IRevitSynapse Synapse { get; }
        public string Id => Synapse.Id;
        public string ProcessPath => Synapse.ProcessPath;

        public SynapseProcess(IRevitSynapse synapse)
        {
            Synapse = synapse;
        }

        public Process Start()
        {
            if (string.IsNullOrEmpty(ProcessPath))
            {
                throw new SynapseRevitException($"ProcessPath for synapse {Synapse.GetType()} is missing.");
            }

            try
            {
                Process process = ProcessUtil.StartProcess(ProcessPath);
                process.EnableRaisingEvents = true;

                process.Exited += (_, _) => SynapseRevitService.DeregisterSynapse(Synapse);

                processId = process.Id;
                
                return process;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public bool ActivateProcess()
        {
            Process process = ProcessUtil.GetProcessById(processId);
            if (process == null)
            {
                throw new SynapseRevitException("process is null!");
            }

            return ProcessUtil.ActivateProcessAndMakeForeground(process);
        }

        public void Close()
        {
            Process processById = ProcessUtil.GetProcessById(processId);
            processById?.CloseMainWindow();
        }

        public bool IsOpen()
        {
            Process process = ProcessUtil.GetProcessById(processId);
            if (process != null &&
                process.Id == processId)
            {
                return true;
            }

            return false;
        }
    }
}
