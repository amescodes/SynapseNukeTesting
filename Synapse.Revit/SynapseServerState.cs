using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace Synapse.Revit
{
    internal static class SynapseServerState
    {
        internal static Server GrpcServer { get; private set; }

        private static void StartGrpcServer(ServerServiceDefinition serviceDefinition,string host, int port)
        {
            // start grpc server
            GrpcServer = new Server
            {
                Services = { serviceDefinition },                
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };

            GrpcServer.Start(); 
        }

        internal static async Task<(ServerServiceDefinition,int)> AddServiceToServer(SynapseRevitService service)
        {
            int port = 7221;
            ServerServiceDefinition serviceDefinition = RevitRunner.BindService(service);

            if (GrpcServer == null)
            {
                StartGrpcServer(serviceDefinition,"localhost",port);
            }
            else
            {
                await GrpcServer.KillAsync();

                port = GrpcServer.Ports.Last().BoundPort;

                GrpcServer.Ports.Add(new ServerPort("localhost", ++port, ServerCredentials.Insecure));
                GrpcServer.Services.Add(serviceDefinition);
                
                GrpcServer.Start(); 
            }

            return (serviceDefinition,port);
        }

        internal static async void RemoveServiceFromServer(ServerServiceDefinition service, int portToClose)
        {
            Server.ServiceDefinitionCollection serviceDefinitionCollection = GrpcServer.Services;
            Server.ServerPortCollection serverPortCollection = GrpcServer.Ports;
            
            await GrpcServer.KillAsync();
            
            GrpcServer = new Server();

            IEnumerable<ServerServiceDefinition> serverServiceDefinitions = serviceDefinitionCollection.Where(s => !s.Equals(service));
            foreach (ServerServiceDefinition serverServiceDefinition in serverServiceDefinitions)
            {
                GrpcServer.Services.Add(serverServiceDefinition);
            }

            IEnumerable<ServerPort> serverPorts = serverPortCollection.Where(p => !p.BoundPort.Equals(portToClose));
            foreach (ServerPort serverPort in serverPorts)
            {
                GrpcServer.Ports.Add(serverPort);
            }

            GrpcServer.Start(); 
        }
    }
}
