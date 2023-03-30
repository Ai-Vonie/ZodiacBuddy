using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.DutyState;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.IoC;
using Dalamud.Plugin;
using ZodiacBuddy.BonusLight;

namespace ZodiacBuddy
{
    /// <summary>
    /// Dalamud and plugin services.
    /// </summary>
    internal class Service
    {
        /// <summary>
        /// Gets or sets the plugin.
        /// </summary>
        internal static ZodiacBuddyPlugin Plugin { get; set; } = null!;

        /// <summary>
        /// Gets or sets the plugin configuration.
        /// </summary>
        internal static PluginConfiguration Configuration { get; set; } = null!;

        /// <summary>
        /// Gets or sets the plugin bonus light manager.
        /// </summary>
        internal static BonusLightManager BonusLightManager { get; set; } = null!;

        /// <summary>
        /// Gets the Dalamud plugin interface.
        /// </summary>
        [PluginService]
        internal static DalamudPluginInterface Interface { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud chat gui.
        /// </summary>
        [PluginService]
        internal static ChatGui ChatGui { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud client state.
        /// </summary>
        [PluginService]
        internal static ClientState ClientState { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud duty state.
        /// </summary>
        [PluginService]
        internal static DutyState DutyState { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud command manager.
        /// </summary>
        [PluginService]
        internal static CommandManager CommandManager { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud condition.
        /// </summary>
        [PluginService]
        internal static Condition Condition { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud data manager.
        /// </summary>
        [PluginService]
        internal static DataManager DataManager { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud framework manager.
        /// </summary>
        [PluginService]
        internal static Framework Framework { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud game gui.
        /// </summary>
        [PluginService]
        internal static GameGui GameGui { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud job gauges.
        /// </summary>
        [PluginService]
        internal static JobGauges JobGauges { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud target manager.
        /// </summary>
        [PluginService]
        internal static TargetManager TargetManager { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud toast manager.
        /// </summary>
        [PluginService]
        internal static ToastGui Toasts { get; private set; } = null!;
    }
}
