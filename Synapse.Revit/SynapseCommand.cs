using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Revit
{
    public class SynapseCommand
    {
        public string Name { get; }

        public SynapseCommand(string name)
        {
            Name = name;
        }
    }
}
