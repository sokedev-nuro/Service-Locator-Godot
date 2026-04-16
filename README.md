# Godot C# Service Locator

A lightweight, centralized service registry (Dependency Injection container) for Godot 4 C# projects. It supports interface-based registration, scoped lifetimes (Global and Scene), and lazy factory resolution.

## Features

- **Global & Scene Scopes**: Keep persistent services alive across the entire game, or bind temporary systems to a specific scene that are cleared on transition.
- **Interface-based Decoupling**: Register services by their interfaces (e.g., `IGameManager`) to keep components completely decoupled from concrete implementations.
- **Lazy Factories**: Use `RegisterFactory<T>` to defer the creation of a service until it is actually requested for the first time.
- **Plug-and-Play**: Built as an Editor Plugin. Activating it automatically registers the `ServiceLocator` as an Autoload (Singleton).

## Installation

1. Copy the `addons/service-locator` folder into your Godot project's `addons/` directory.
2. Build your .NET solution (`MSBuild` / `dotnet build`) so Godot can compile the plugin scripts.
3. Open Godot and go to **Project -> Project Settings -> Plugins**.
4. Find **Service Locator** in the list and check **Enable**.
   *(This will automatically add `ServiceLocator` to your AutoLoad list).*

## Usage

### 1. Registering Services

You can register services from any Node (for example, in `_Ready` or `_EnterTree`), but it's best to register global services in an initialization script.

```csharp
using Godot;

public partial class MyBootstrapper : Node
{
    public override void _Ready()
    {
        // 1. Register a previously instantiated Node/Object (Global scope by default)
        var audioManager = GetNode<AudioManager>("AudioManager");
        ServiceLocator.RegisterService<IAudioManager>(audioManager);

        // 2. Register a factory that creates the service only when requested
        ServiceLocator.RegisterFactory<INetworkClient>(() => new NetworkClient());

        // 3. Register a Scene-scoped service (will be cleared later)
        var levelController = new LevelController();
        ServiceLocator.RegisterService<ILevelController>(levelController, ServiceLocator.Scope.Scene);
    }
}
```

### 2. Resolving Services

Any script in your game can easily access the registered services without needing Node paths (GetNode(...)) or tight coupling.

```csharp
using Godot;

public partial class Player : CharacterBody3D
{
    private IAudioManager _audioManager;
    private ILevelController _levelController;

    public override void _Ready()
    {
        // Retrieve the registered services
        _audioManager = ServiceLocator.GetService<IAudioManager>();
        _levelController = ServiceLocator.GetService<ILevelController>();
    }

    private void Jump()
    {
        _audioManager.PlaySound("jump");
    }
}
```

### 3. Managing Scene Scope

When switching scenes, you should clear services that were registered with Scope.Scene to prevent memory leaks or calling invalid objects.

You can do this right before unloading the current scene or loading the new one:

```csharp
public void LoadNextLevel()
{
    // Clear all services registered with Scope.Scene
    // Global services and Factories remain intact!
    ServiceLocator.ClearSceneServices();

    GetTree().ChangeSceneToFile("res://levels/level_2.tscn");
}
```

## API Overview

- `void RegisterService<T>(T service, Scope scope = Scope.Global)`: Registers an existing instance.
- `void RegisterFactory<T>(Func<T> factory)`: Registers a delegate to create the service upon first request. Caches in Global scope.
- `T GetService<T>()`: Retrieves the service. Throws an exception or returns null (depending on your implementation) if not found.
- `void ClearSceneServices()`: Removes all services registered under `Scope.Scene`.