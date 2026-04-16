#if TOOLS
using Godot;

[Tool]
public partial class ServiceLocatorPlugin : EditorPlugin
{
    private const string AutoloadName = "ServiceLocator";
    private const string AutoloadPath = "res://addons/service-locator/ServiceLocator.cs";

    public override void _EnterTree()
    {
        // Register ServiceLocator as an Autoload singleton
        AddAutoloadSingleton(AutoloadName, AutoloadPath);
    }

    public override void _ExitTree()
    {
        // Remove Autoload when disabling the plugin
        RemoveAutoloadSingleton(AutoloadName);
    }
}
#endif