using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Grpc.Core;
using Synapse;

using Newtonsoft.Json;

namespace Synapse.Revit
{
    public class SynapseRevitService : RevitRunner.RevitRunnerBase
    {
        private IRevitSynapse _revitSynapse;

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
        
        private Server StartRevitRunnerServer(string host, int port)
        {
            // start grpc server
            Server server = new Server
            {
                Services = { RevitRunner.BindService(this) },                
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };
            server.Start();

            return server;
        }

        public static SynapseRevitService StartSynapseRevitService(IRevitSynapse synapse, Assembly assembly)
        {
            SynapseRevitService service = new SynapseRevitService(synapse);
            service.MakeRevitCommandRunnerDictionary(assembly);
            service.StartRevitRunnerServer($"localhost",7221);
            return service;
        }
    }
}