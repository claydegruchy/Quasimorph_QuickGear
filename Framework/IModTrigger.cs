namespace QuasimorphHelloWorld.Framework
{
    /// <summary>
    /// Represents a named trigger/handler that executes when a condition is met.
    /// Implement this to define mod behaviors.
    /// </summary>
    public interface IModTrigger
    {
        string TriggerName { get; }
        void Execute();
    }
}
