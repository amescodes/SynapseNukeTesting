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

        private int processId;
        private IRevitSynapse _revitSynapse;
        private Dictionary<int,MethodInfo> synapseMethodDictionary = new Dictionary<int, MethodInfo>();

        private SynapseRevitService(IRevitSynapse revitSynapse)
        {
            this._revitSynapse = revitSynapse;
        }

        public override Task<SynapseOutput> DoRevit(SynapseRequest request, ServerCallContext context)
        {
            //MethodInfo method = RevitRunnerCommandDictionary[commandEnum];
            if (!synapseMethodDictionary.TryGetValue(request.MethodId, out MethodInfo method))
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

                    synapseMethodDictionary.Add(revitCommandAttribute.MethodIdToRun, method);
                }
            }
        }

        public void ShutdownSynapseRevitService()
        {
            SynapseServerState.RemoveServiceFromServer(serviceDefinition,portNumber);
            
            Process processById = ProcessUtil.GetProcessById(processId);
            processById?.Kill();

            SynapseServerState.GrpcServer.ShutdownAsync();
        }

        public Process StartProcess()
        {
            Process process =  ProcessUtil.StartProcess(_revitSynapse.ProcessPath,portNumber);
            process.Exited += ProcessOnExited;

            return process;
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

        private void ProcessOnExited(object sender, EventArgs e)
        {
            try
            {
                ShutdownSynapseRevitService();
                ProcessUtil.GetProcessById(processId).Exited -= ProcessOnExited;
            }
            catch (Exception ex)
            {
                // should something throw here?
                throw new SynapseRevitException("An error occurred during process close. See InnerException for more details.", ex);
            }
        }
        
        /// <summary>
        /// Use to create a Synapse in the Revit addin component of the application. Once this is started,
        /// use <see cref="StartProcess"/> to start the outer process component of the application (typically the UI).
        /// </summary>
        /// <param name="synapse"></param>
        /// <returns></returns>
        public static SynapseRevitService StartSynapseRevitService(IRevitSynapse synapse)
        {
            SynapseRevitService service = new SynapseRevitService(synapse);
            
            Assembly assembly = Assembly.GetAssembly(synapse.GetType());
            service.MakeRevitCommandRunnerDictionary(assembly);

            Task<(ServerServiceDefinition, int)> serverService = SynapseServerState.AddServiceToServer(service);
            serverService.Wait(TimeSpan.FromSeconds(10));
            (service.serviceDefinition, service.portNumber) = serverService.Result;

            return service;
        }
    }
}