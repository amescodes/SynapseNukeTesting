using System;

namespace Synapse.Revit
{
    public class SynapseRevitMethodAttribute : Attribute
    {
        public string MethodToRun { get; }
        public Type[] InputVariableTypes { get; }
        public Type OutputVariableType { get; }

        public SynapseRevitMethodAttribute(string methodToRun, Type outputVariableType, params Type[] inputVariableTypes)
        {
            MethodToRun = methodToRun;
            OutputVariableType = outputVariableType;
            InputVariableTypes = inputVariableTypes;
        }
    }
}