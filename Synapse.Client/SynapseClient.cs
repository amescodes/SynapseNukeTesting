using System;

using Grpc.Core;

namespace Synapse
{
    public class SynapseClient
    {
        private Channel channel;

        public RevitRunner.RevitRunnerClient Start(string appName)
        {
            //channel = new Channel($"127.0.0.1:{port}", ChannelCredentials.Insecure);
            channel = new Channel($"synapse-{appName}:7221", ChannelCredentials.Insecure);
            return new RevitRunner.RevitRunnerClient(channel);

        }

        public void Shutdown()
        {
            channel.ShutdownAsync();
        }
    }
}
