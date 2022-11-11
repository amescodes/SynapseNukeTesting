using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Synapse
{
    public class SynapseClient
    {
        private Channel channel;
        private RevitRunner.RevitRunnerClient revitRunner;

        private SynapseClient(){}

        public static SynapseClient StartSynapseClient()
        {
            SynapseClient synapseClient = new SynapseClient();
            
            synapseClient.channel = new Channel($"127.0.0.1:6902", ChannelCredentials.Insecure);
            synapseClient.revitRunner = new RevitRunner.RevitRunnerClient(synapseClient.channel);

            return synapseClient;
        }

        public SynapseOutput DoRevit(SynapseRequest request)
        {
            return revitRunner.DoRevit(request);
        }

        public async Task<SynapseOutput> DoRevitAsync(SynapseRequest request)
        {
            return await revitRunner.DoRevitAsync(request);
        }

        public void Shutdown()
        {
            channel.ShutdownAsync();
        }
    }
}
