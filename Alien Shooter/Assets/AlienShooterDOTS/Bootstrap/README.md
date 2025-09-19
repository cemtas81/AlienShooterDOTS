# DOTS Bootstrap System

This directory contains the bootstrap system for AlienShooterDOTS that handles initial game setup using Unity DOTS (Data-Oriented Technology Stack).

## Setup Instructions

### 1. Create Bootstrap Scene

1. Create a new scene called `Bootstrap.unity` in `Assets/Scenes/`
2. In the scene, create an empty GameObject called "WorldSubScene"
3. Add a `SubScene` component to this GameObject
4. Create a new subscene by clicking "New SubScene..." in the SubScene component
5. Enter the SubScene by clicking "Open SubScene for Editing"
6. Inside the SubScene, create a GameObject called "GameSettings"
7. Add the `GameSettingsAuthoring` component to this GameObject

### 2. Configure GameSettings

In the GameSettingsAuthoring component inspector, configure:

- **Player Prefab**: Assign a prefab with `PlayerAuthoring` component
- **Enemy Prefab**: Assign a prefab with `EnemyAuthoring` component  
- **Spawn Interval**: Time between enemy spawn waves (default: 1.0s)
- **Initial Enemy Count**: Enemies spawned immediately (default: 5)
- **Max Alive Enemies**: Maximum concurrent enemies (default: 20)
- **Batch Size**: Enemies spawned per wave (default: 3)
- **Spawn Area Center**: World position for spawning (default: 0,0,0)
- **Spawn Area Radius**: Radius for random enemy placement (default: 10)
- **Level Scene**: Optional scene to load on bootstrap (leave empty if not needed)

### 3. Usage

1. Set `Bootstrap.unity` as your main scene
2. Player will spawn at the center of the spawn area
3. Initial enemies spawn immediately in a circle around the spawn area
4. Additional enemies spawn continuously based on the configured interval
5. If a Level Scene is assigned, it will be loaded automatically

## Components

- **GameSettingsAuthoring**: MonoBehaviour for editor configuration
- **GameSettings**: IComponentData singleton with bootstrap settings
- **BootstrapSystem**: InitializationSystemGroup system that runs once on startup
- **EnemySpawnerSystem**: SimulationSystemGroup system for continuous enemy spawning
- **EnemySpawnSettings**: IComponentData for spawner configuration
- **BootstrapDone**: Tag component to prevent re-running bootstrap

## Technical Details

- Uses Entities 1.2.0 APIs and patterns
- Burst-compiled for performance
- SceneAsset references converted to Hash128 for runtime use
- EntityCommandBuffer used for structural changes
- Follows DOTS best practices with proper system groups and update ordering