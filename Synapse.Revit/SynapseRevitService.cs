using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Grpc.Core;

using Newtonsoft.Json;

namespace Synapse.Revit
{
    public class SynapseRevitService : RevitRunner.RevitRunnerBase
    {
        private int portNumber;
        private ServerServiceDefinition serviceDefinition;

        private IRevitSynapse _revitSynapse;
        private int processId;

        public Dictionary<int,MethodInfo> SynapseMethodDictionary { get; } =
            new Dictionary<int, MethodInfo>();

        private SynapseRevitService(IRevitSynapse revitSynapse)
        {
            this._revitSynapse = revitSynapse;
        }

        public override Task<SynapseOutput> DoRevit(SynapseRequest request, ServerCallContext context)
        {
            //MethodInfo method = RevitRunnerCommandDictionary[commandEnum];
            if (!SynapseMethodDictionary.TryGetValue(request.MethodId, out MethodInfo method))
            {
                throw new SynapseRevitException("Method not found in SynapseMethodDictionary!");
            }

            if (method.GetCustomAttribute<SynapseRevitMethodAttribute>() is not SynapseRevitMethodAttribute revitCommandAttribute)
            {
                throw new SynapseRevitException("Command registered without RevitCommandAttribute!");
            }

            Type[] inputVariableTypes = revitCommandAttribute.InputVariableTypes;
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != inputVariableTypes.Length)
            {
                throw new SynapseRevitException(
                    $"Number of input arguments ({inputVariableTypes.Length}) from the attribute on method {method.Name} does not match the number needed by the method ({method.GetGenericArguments().Length}).");
            }
            
            object[] commandInputsAsArray = JsonConvert.DeserializeObject<object[]>(request.MethodInputJson);
            
            object output = method.Invoke(_revitSynapse, commandInputsAsArray);
            string jsonOutput = JsonConvert.SerializeObject(output);
            
            return Task.FromResult(new SynapseOutput()
            {
                MethodOutputJson = jsonOutput
            });
        }

        private void MakeRevitCommandRunnerDictionary(Assembly assembly)
        {
            Type[] exportedTypes = assembly.GetExportedTypes();
            foreach (Type t in exportedTypes)
            {
                MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (MethodInfo method in methods)
                {
                    if (method.GetCustomAttribute<SynapseRevitMethodAttribute>() is not SynapseRevitMethodAttribute revitCommandAttribute)
                    {
                        continue;
                    }

                    SynapseMethodDictionary.Add(revitCommandAttribute.MethodIdToRun, method);
                }
            }
        }
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        public Process StartProcess(IntPtr revitWindowHandle)
        {
            // if the browser window/process is already open, activate it instead of opening a new process 
            if (processId != 0)
            {
                // the following line could be enough, but rather activate the window thru the process
                //SetForegroundWindow(processHwnd);

                return GetProcessById(processId);
            }

            // execute the browser window process
            Process process = new Process();
            process.StartInfo.FileName = _revitSynapse.ProcessPath;
            process.StartInfo.Arguments = revitWindowHandle.ToString(); // pass the MessageHandler's window handle the the process as a command line argument
            process.Start();
            
            processId = process.Id; // grab the PID so we can kill the process if required;
            
            return process;
        }

        public bool ActivateProcessAndMakeForeground(Process p)
        {
            if (p == null)
            {
                throw new SynapseRevitException("Process is null.");
            }

            IntPtr windowHandle = p.MainWindowHandle;
            return SetForegroundWindow(windowHandle);
        }

        private Process GetProcessById(int id)
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
        
        public void ShutdownSynapseRevitService()
        {
            SynapseRevitState.RemoveServiceFromServer(serviceDefinition,portNumber);
        }
        
        public static SynapseRevitService StartSynapseRevitService(IRevitSynapse synapse)
        {
            SynapseRevitService service = new SynapseRevitService(synapse);

            Assembly assembly = Assembly.GetAssembly(typeof(IRevitSynapse));
            service.MakeRevitCommandRunnerDictionary(assembly);

            Task<(ServerServiceDefinition, int)> serverService = SynapseRevitState.AddServiceToServer(service);
            serverService.Wait();
            (service.serviceDefinition, service.portNumber) = serverService.Result;

            return service;
        }
    }
}