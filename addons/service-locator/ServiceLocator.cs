using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Centralized service registry with interface-based registration,
/// scoped lifetime (Global / Scene), and lazy factory resolution.
/// </summary>
/// <remarks>
/// Usage:
///   RegisterService&lt;IGameManager&gt;(this)           — explicit interface key
///   RegisterFactory&lt;IAudioManager&gt;(() => new ...)  — created on first GetService call
///   ClearSceneServices()                            — call on scene unload
/// </remarks>
public partial class ServiceLocator : Node
{
    // ── Enums ─────────────────────────────────────────────────────────────────

    /// <summary>Determines how long the service lives in the registry.</summary>
    public enum Scope
    {
        /// <summary>Survives scene changes. Cleared only when ServiceLocator is destroyed.</summary>
        Global,
        /// <summary>Cleared on <see cref="ClearSceneServices"/>. Use for per-level systems.</summary>
        Scene
    }

    // ── Private Fields ────────────────────────────────────────────────────────

    private static ServiceLocator _instance;

    private readonly Dictionary<Type, object> _globalServices = new();
    private readonly Dictionary<Type, object> _sceneServices = new();
    private readonly Dictionary<Type, Func<object>> _factories = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void _EnterTree()
    {
        if (_instance != null && _instance != this)
        {
            QueueFree();
            GD.PushWarning("ServiceLocator: second instance detected — using the existing one.");
            return;
        }

        _instance = this;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete && _instance == this)
        {
            _globalServices.Clear();
            _sceneServices.Clear();
            _factories.Clear();
            _instance = null;
        }
    }

    // ── Public Methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Registers <paramref name="service"/> under type <typeparamref name="T"/>.
    /// Specify an interface as <typeparamref name="T"/> to decouple consumers from
    /// the concrete implementation: <c>RegisterService&lt;IGameManager&gt;(this)</c>.
    /// </summary>
    /// <param name="scope">
    /// <see cref="Scope.Global"/> — survives scene changes (default).<br/>
    /// <see cref="Scope.Scene"/>  — cleared by <see cref="ClearSceneServices"/>.
    /// </param>
    public static void RegisterService<T>(T service, Scope scope = Scope.Global) where T : class
    {
        if (!ValidateInstance("RegisterService")) return;

        Dictionary<Type, object> registry = scope == Scope.Global
            ? _instance._globalServices
            : _instance._sceneServices;

        bool overwriting = registry.ContainsKey(typeof(T));

#if DEBUG
        if (overwriting)
            GD.Print($"[ServiceLocator] '{typeof(T).Name}' overwritten ({scope}).");
        else
            GD.Print($"[ServiceLocator] '{typeof(T).Name}' registered ({scope}).");
#endif

        registry[typeof(T)] = service;
    }

    /// <summary>
    /// Registers a factory delegate that creates the service on the first
    /// <see cref="GetService{T}"/> call. The result is cached in Global scope.
    /// </summary>
    public static void RegisterFactory<T>(Func<T> factory) where T : class
    {
        if (!ValidateInstance("RegisterFactory")) return;
        if (factory == null) { GD.PushError("ServiceLocator: factory delegate is null."); return; }

        _instance._factories[typeof(T)] = () => factory();

#if DEBUG
        GD.Print($"[ServiceLocator] Factory for '{typeof(T).Name}' registered.");
#endif
    }
    /// <summary>
    /// Resolves a service by type <typeparamref name="T"/>.
    /// Resolution order: Scene → Global → Factory (lazy, cached as Global).
    /// Returns <c>null</c> and logs an error if not found.
    /// </summary>
    public static T GetService<T>() where T : class
    {
        if (!ValidateInstance("GetService")) return null;

        Type type = typeof(T);

        if (_instance._sceneServices.TryGetValue(type, out object sceneService))
            return sceneService as T;

        if (_instance._globalServices.TryGetValue(type, out object globalService))
            return globalService as T;

        if (_instance._factories.TryGetValue(type, out Func<object> factory))
        {
            T created = factory() as T;
            _instance._globalServices[type] = created;

#if DEBUG
            GD.Print($"[ServiceLocator] '{type.Name}' created via factory (lazy).");
#endif
            return created;
        }

        GD.PushError($"ServiceLocator: service of type '{type.Name}' is not registered.");
        return null;
    }

    /// <summary>
    /// Removes all Scene-scoped services. Call this when unloading a level.
    /// </summary>
    public static void ClearSceneServices()
    {
        if (!ValidateInstance("ClearSceneServices")) return;

#if DEBUG
        GD.Print($"[ServiceLocator] Scene services cleared ({_instance._sceneServices.Count} entries).");
#endif

        _instance._sceneServices.Clear();
    }

    /// <summary>
    /// Manually unregisters a service or factory by type from all scopes.
    /// </summary>
    public static void UnregisterService<T>() where T : class
    {
        if (!ValidateInstance("UnregisterService")) return;

        Type type = typeof(T);
        _instance._globalServices.Remove(type);
        _instance._sceneServices.Remove(type);
        _instance._factories.Remove(type);

#if DEBUG
        GD.Print($"[ServiceLocator] '{type.Name}' unregistered.");
#endif
    }

    // ── Private Methods ───────────────────────────────────────────────────────

    private static bool ValidateInstance(string callerName)
    {
        if (_instance != null) return true;
        GD.PushError($"ServiceLocator.{callerName}: instance is null — ensure ServiceLocator is in the scene tree.");
        return false;
    }
}