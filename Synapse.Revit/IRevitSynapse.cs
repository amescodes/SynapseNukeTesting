namespace Synapse.Revit
{
    public interface IRevitSynapse
    {
        public string Id { get; }
        /// <summary>
        /// Path of the process to open and connect with this synapse.
        /// </summary>
        string ProcessPath { get; }
    }
}
