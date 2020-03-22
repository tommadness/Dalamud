using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Configuration;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Internal;
using Dalamud.Game.Internal.Gui;
using Dalamud.Interface;

namespace Dalamud.Plugin
{
    /// <summary>
    /// This class acts as an interface to various objects needed to interact with Dalamud and the game.
    /// </summary>
    public class DalamudPluginInterface : IDisposable {
        /// <summary>
        /// The CommandManager object that allows you to add and remove custom chat commands.
        /// </summary>
        public readonly CommandManager CommandManager;

        /// <summary>
        /// The ClientState object that allows you to access current client memory information like actors, territories, etc.
        /// </summary>
        public readonly ClientState ClientState;

        /// <summary>
        /// The Framework object that allows you to interact with the client.
        /// </summary>
        public readonly Framework Framework;

        /// <summary>
		/// A <see cref="UiBuilder">UiBuilder</see> instance which allows you to draw UI into the game via ImGui draw calls.
        /// </summary>
        public readonly UiBuilder UiBuilder;

        /// <summary>
        /// A <see cref="SigScanner">SigScanner</see> instance targeting the main module of the FFXIV process.
        /// </summary>
        public readonly SigScanner TargetModuleScanner;

        /// <summary>
        /// A <see cref="DataManager">DataManager</see> instance which allows you to access game data needed by the main dalamud features.
        /// </summary>
        public readonly DataManager Data;

        private readonly Dalamud dalamud;
        private readonly string pluginName;
        private readonly PluginConfigurations configs;

        /// <summary>
        /// Set up the interface and populate all fields needed.
        /// </summary>
        /// <param name="dalamud"></param>
        public DalamudPluginInterface(Dalamud dalamud, string pluginName, PluginConfigurations configs) {
            this.CommandManager = dalamud.CommandManager;
            this.Framework = dalamud.Framework;
            this.ClientState = dalamud.ClientState;
            this.UiBuilder = new UiBuilder(dalamud.InterfaceManager, pluginName);
            this.TargetModuleScanner = dalamud.SigScanner;
            this.Data = dalamud.Data;

            this.dalamud = dalamud;
            this.pluginName = pluginName;
            this.configs = configs;
        }

        /// <summary>
        /// Unregister your plugin and dispose all references. You have to call this when your IDalamudPlugin is disposed.
        /// </summary>
        public void Dispose() {
            this.UiBuilder.Dispose();
        }

        /// <summary>
        /// Save a plugin configuration(inheriting IPluginConfiguration).
        /// </summary>
        /// <param name="currentConfig">The current configuration.</param>
        public void SavePluginConfig(IPluginConfiguration currentConfig) {
            if (currentConfig == null)
                return;

            this.configs.Save(currentConfig, this.pluginName);
        }

        /// <summary>
        /// Get a previously saved plugin configuration or null if none was saved before.
        /// </summary>
        /// <returns>A previously saved config or null if none was saved before.</returns>
        public IPluginConfiguration GetPluginConfig() {
            // This is done to support json deserialization of plugin configurations
            // even after running an in-game update of plugins, where the assembly version
            // changes.
            // Eventually it might make sense to have a separate method on this class
            // T GetPluginConfig<T>() where T : IPluginConfiguration
            // that can invoke LoadForType() directly instead of via reflection
            // This is here for now to support the current plugin API
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                if (type.GetInterface(typeof(IPluginConfiguration).FullName) != null)
                {
                    var mi = this.configs.GetType().GetMethod("LoadForType");
                    var fn = mi.MakeGenericMethod(type);
                    return (IPluginConfiguration)fn.Invoke(this.configs, new object[] { this.pluginName });
                }
            }

            // this shouldn't be a thing, I think, but just in case
            return this.configs.Load(this.pluginName);
        }

        #region Logging

        /// <summary>
        /// Log a templated message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void Log(string messageTemplate, params object[] values) {
            Serilog.Log.Information(messageTemplate, values);
        }

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void LogError(string messageTemplate, params object[] values)
        {
            Serilog.Log.Error(messageTemplate, values);
        }

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void LogError(Exception exception, string messageTemplate, params object[] values)
        {
            Serilog.Log.Error(exception, messageTemplate, values);
        }

        #endregion
    }
}
