namespace QuasimorphHelloWorld.Framework
{
    /// <summary>
    /// Generic interface for mod configurations.
    /// Implement this with your specific config class.
    /// </summary>
    public interface IModConfig
    {
        void LoadFromPath(string path);
        void SaveToPath(string path);
    }
}
