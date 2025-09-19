# AlienShooterDOTS - Complete Working Game

A fully functional top-down alien shooter game built with Unity DOTS (Data-Oriented Technology Stack). This project demonstrates a complete working game with all systems implemented and ready to play.

## 🎮 READY TO PLAY - Working Game Features

✅ **Fully Playable Game Now**
- Complete player movement with WASD controls
- Shooting mechanics with weapon cooldowns and reloading
- Intelligent enemy AI with multiple states (idle, patrol, chase, attack)
- Combat system with health, damage, and collision detection
- Dynamic enemy spawning system
- Score tracking and game state management
- Dash ability with cooldown and invulnerability frames

✅ **Professional DOTS Implementation**
- Burst-compiled systems for maximum performance
- Entity Component System architecture
- Job-based parallel processing for scalability
- Efficient memory management and entity lifecycle
- Proper separation of concerns

## 🚀 Quick Start - Play Immediately

### Instant Demo (30 seconds to playable game)
1. **Use Editor Tool**: Menu → `AlienShooterDOTS > Create Working Demo Scene`
2. **Press Play** - Game is immediately playable!

### Manual Setup
1. Create new scene → Add empty GameObject "Game Manager"
2. Add `SimpleGameObjectBootstrap` component
3. Assign Player/Enemy prefabs from `Assets/AlienShooterDOTS/Prefabs/`
4. Add `LegacyPlayerInputSystem` component → Press Play!

**Controls**: WASD (move), SPACE (shoot), SHIFT (dash), R (reload)

## Features

### Core DOTS Systems
- **Player Movement System**: Smooth movement with dash mechanics using ECS
- **Enemy AI System**: Multi-state AI (Idle, Patrol, Chase, Attack, Dead) 
- **Weapon System**: Comprehensive firing mechanics with ammo management and reloading
- **Game Manager**: Wave-based gameplay with scoring and enemy spawning

### ECS Components
- **Player Components**: Input handling, stats, state management, damage system
- **Enemy Components**: AI states, combat behavior, patrol logic, death handling
- **Weapon Components**: Fire rates, ammo, projectiles, weapon types, effects

### Integration Stubs
- **GPU Animation Entities**: Sample integration for high-performance character animation
- **Agents Navigation**: Pathfinding and navigation system integration
- **Unity Physics**: Collision detection and physics-based interactions
- **Input System**: Modern Unity Input System integration with DOTS

### Authoring & Tools
- **Authoring Components**: Easy GameObject-to-Entity conversion
- **Custom Editor Tools**: Development utilities and configuration windows
- **Sample Scenes**: Ready-to-use example implementations

## Requirements

- **Unity Version**: 6.2.0 or later
- **Entities Package**: 1.2.0 or later
- **Unity Physics**: 1.3.0+ (included)
- **Input System**: 1.0.0+ (optional, for advanced input)

### Recommended Packages
- **Entities Graphics**: For rendering DOTS entities
- **GPU Animation Entities**: For high-performance character animation
- **Agents Navigation**: For advanced pathfinding (Project Dawn Navigation)

## Project Structure

```
Assets/AlienShooterDOTS/
├── Core/
│   ├── Components/          # ECS component definitions
│   │   ├── PlayerComponents.cs
│   │   ├── EnemyComponents.cs
│   │   └── WeaponComponents.cs
│   └── Systems/             # Core ECS systems
│       ├── PlayerMovementSystem.cs
│       ├── EnemyAISystem.cs
│       └── WeaponSystem.cs
├── Gameplay/                # Game logic and management
│   └── GameManager.cs       # Wave system, scoring, game state
├── Integration/             # Third-party package integrations
│   ├── GPUAnimationIntegration.cs
│   ├── AgentsNavigationIntegration.cs
│   ├── PhysicsIntegration.cs
│   └── InputSystem/
│       └── PlayerInputSystem.cs
├── Authoring/               # GameObject-to-Entity conversion
│   └── PlayerAuthoring.cs   # Authoring components for editor
├── Examples/                # Sample scenes and prefabs
│   └── README.md           # Example usage documentation
└── Editor/                  # Custom editor tools
    └── AlienShooterEditorTools.cs
```

## Quick Start

### 1. Basic Setup
1. Clone this repository
2. Open the project in Unity 6.2+
3. Ensure all required packages are installed
4. Open the AlienShooterDOTS configuration window: `Window > AlienShooterDOTS > Configuration Window`

### 2. Create Your First Scene
1. Create a new scene
2. Use the configuration window to add:
   - Player entity with `PlayerAuthoring` component
   - Enemy entities with `EnemyAuthoring` components
   - Spawn points with `SpawnPointAuthoring` components
3. Add input handling with `PlayerInputSystem` component on a GameObject

### 3. Customize Components
```csharp
// Example: Modify player stats
var playerAuthoring = playerGameObject.GetComponent<PlayerAuthoring>();
playerAuthoring.MaxHealth = 150f;
playerAuthoring.MoveSpeed = 8f;
playerAuthoring.DashSpeed = 20f;
```

### 4. Create Custom Weapons
```csharp
// Example: Setup a custom weapon
var weaponAuthoring = weaponGameObject.GetComponent<WeaponAuthoring>();
weaponAuthoring.WeaponType = WeaponTypeEnum.Rifle;
weaponAuthoring.Damage = 35f;
weaponAuthoring.FireRate = 8f;
weaponAuthoring.IsAutomatic = true;
```

## System Overview

### Player Movement System
- WASD/Arrow key movement with normalization
- Dash mechanics with cooldown system
- State management (Idle, Moving, Dashing, Dead)
- Physics-based movement integration

### Enemy AI System
- **Idle**: Rest state with transition timers
- **Patrol**: Random movement within defined radius
- **Chase**: Pursue player when in detection range
- **Attack**: Combat when in range with cooldown
- **Dead**: Death state handling

### Weapon System
- Semi-automatic and automatic firing modes
- Ammo management with reloading
- Multiple projectiles per shot (shotgun-style)
- Accuracy system with spread calculation
- Muzzle flash and effect integration

### Game Manager
- Wave-based enemy spawning
- Difficulty scaling per wave
- Score tracking and bonuses
- Game state management

## Integration Examples

### GPU Animation Integration
```csharp
// Play animation on entity
GPUAnimationUtils.PlayAnimation(entityManager, entity, CommonAnimationStates.RUN);

// Transition between animations
GPUAnimationUtils.TransitionToAnimation(entityManager, entity, 
    CommonAnimationStates.IDLE, CommonAnimationStates.ATTACK, 0.3f);
```

### Navigation Integration
```csharp
// Setup navigation agent
NavigationUtils.SetupNavigationAgent(entityManager, entity, maxSpeed: 5f);

// Set destination
NavigationUtils.SetDestination(entityManager, entity, targetPosition);

// Check if reached destination
bool hasArrived = NavigationUtils.HasReachedDestination(entityManager, entity);
```

### Physics Integration
```csharp
// Create explosion effect
PhysicsUtils.CreateExplosion(entityManager, explosionPosition, radius: 5f, damage: 50f);

// Setup health component
PhysicsUtils.SetupPhysicsHealth(entityManager, entity, maxHealth: 100f);
```

## Customization Guide

### Adding New Enemy Types
1. Extend the `EnemyTypeEnum` in `EnemyComponents.cs`
2. Create new authoring prefab with `EnemyAuthoring` component
3. Modify `GameManagerSystem` to include new enemy in spawn logic
4. Add custom AI behaviors in `EnemyAISystem` if needed

### Creating Custom Weapons
1. Add new weapon type to `WeaponTypeEnum`
2. Create weapon prefab with `WeaponAuthoring` component
3. Configure stats (damage, fire rate, ammo, etc.)
4. Customize projectile behavior in `WeaponSystem`

### Extending Player Abilities
1. Add new input actions to `PlayerInput` component
2. Extend `PlayerMovementSystem` for new movement abilities
3. Create additional state types in `PlayerStateType` enum
4. Update input handling in `PlayerInputSystem`

## Performance Considerations

### DOTS Best Practices
- Components are structs implementing `IComponentData`
- Systems use `IJobEntity` for parallel processing
- Burst compilation enabled for performance-critical systems
- Efficient queries using `SystemAPI`

### Memory Management
- Uses Unity's native collections for temporary data
- Proper disposal of query results and arrays
- Blob assets for complex data structures
- Entity command buffers for structural changes

### Scalability
- Systems designed for thousands of entities
- Parallel job scheduling where possible
- Minimal garbage collection through value types
- Efficient spatial queries and collision detection

## Troubleshooting

### Common Issues
1. **Missing Components**: Ensure all required packages are installed
2. **Input Not Working**: Check `PlayerInputSystem` is attached to a GameObject
3. **Entities Not Spawning**: Verify authoring components are properly configured
4. **Performance Issues**: Enable Burst compilation and use Entity Debugger

### Debug Tools
- Use the Entity Debugger window: `Window > Entities > Debugger`
- Scene view overlay shows entity counts in play mode
- Configuration window provides validation tools
- Console logging in systems for debugging

## Contributing

This template is designed to be extended and modified for your specific needs. Feel free to:
- Add new systems and components
- Integrate additional packages
- Optimize for your target platform
- Share improvements with the community

## License

This project is provided as a template for educational and commercial use. Modify and distribute as needed for your projects.

## Support

For questions and support:
- Check the Examples folder for usage patterns
- Review component documentation in code
- Use Unity's Entity forums for DOTS-specific questions
- Reference Unity's DOTS documentation for advanced topics
