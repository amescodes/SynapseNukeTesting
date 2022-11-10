using System;

namespace Synapse.Revit
{
    public class SynapseRevitMethodAttribute : Attribute
    {
        public int MethodIdToRun { get; }
        public Type[] InputVariableTypes { get; }
        public Type OutputVariableType { get; }

        public SynapseRevitMethodAttribute(int methodIdToRun, Type outputVariableType, params Type[] inputVariableTypes)
        {
            MethodIdToRun = methodIdToRun;
            OutputVariableType = outputVariableType;
            InputVariableTypes = inputVariableTypes;
        }
    }
}