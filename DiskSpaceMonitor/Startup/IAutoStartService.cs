namespace DiskSpaceMonitor.Startup
{
    /// <summary>Manages whether the app launches automatically when the user logs in.</summary>
    public interface IAutoStartService
    {
        /// <summary>True if the app is currently registered to start at login.</summary>
        bool IsEnabled();

        /// <summary>Register or unregister the app to start at login.</summary>
        void SetEnabled(bool enabled);
    }
}
