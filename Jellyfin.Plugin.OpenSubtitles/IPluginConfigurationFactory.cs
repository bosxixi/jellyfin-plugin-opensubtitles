using Jellyfin.Plugin.OpenSubtitles.Configuration;

namespace Jellyfin.Plugin.OpenSubtitles
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPluginConfigurationFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        PluginConfiguration GetOption();
    }
}
