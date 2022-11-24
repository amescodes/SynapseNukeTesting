using System;

namespace Synapse.Revit
{
    public class SynapseRevitMethodAttribute : Attribute
    {
        public string MethodId { get; }

        public SynapseRevitMethodAttribute(string methodId)
        {
            MethodId = methodId;
        }
    }
}