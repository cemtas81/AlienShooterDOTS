# AlienShooterDOTS - Working Game Setup Guide

This guide explains how to set up a fully working AlienShooterDOTS game scene.

## Quick Setup for Working Demo

### Option 1: Simple GameObject Bootstrap (Recommended for Testing)

1. Create a new scene
2. Add an empty GameObject and name it "GameManager"
3. Add the `SimpleGameObjectBootstrap` component to it
4. Assign the Player and Enemy prefabs from `Assets/AlienShooterDOTS/Prefabs/`
5. Configure the spawn settings as desired
6. Add a `LegacyPlayerInputSystem` or `PlayerInputSystem` component for input
7. Press Play to start the game

**Controls:**
- WASD: Move
- SPACE: Shoot  
- SHIFT: Dash
- R: Reload

### Option 2: Full DOTS Bootstrap System

1. Create a Bootstrap scene following the Bootstrap README instructions
2. Set up the SubScene with GameSettings
3. Configure entity prefabs in the GameSettingsAuthoring component
4. This approach requires properly converted entity prefabs

## What's Working

✅ **Core Systems:**
- Player movement and controls
- Enemy AI (idle, patrol, chase, attack states)
- Weapon system with firing and reloading
- Combat system with damage and collision detection
- Enemy spawning system
- Game state management

✅ **Features:**
- WASD movement with dash ability
- Shooting with weapon cooldowns
- Enemy AI that detects and chases player
- Health/damage system
- Score tracking
- Dynamic enemy spawning
- Invulnerability frames after taking damage

✅ **Input Systems:**
- Modern Input System integration
- Legacy Input System fallback
- Configurable key bindings

## System Overview

### Player Systems
- `PlayerMovementSystem`: Handles movement, dashing, and state management
- `PlayerInputSystem`/`LegacyPlayerInputSystem`: Input handling and entity updates

### Enemy Systems  
- `EnemyAISystem`: AI state machine (idle, patrol, chase, attack)
- `EnemySpawnerSystem`: Continuous enemy spawning based on settings

### Combat Systems
- `WeaponSystem`: Weapon firing, reloading, projectile spawning
- `CombatSystem`: Collision detection and damage dealing
- `ProjectileCleanupSystem`: Projectile lifetime management

### Management Systems
- `GameManagerSystem`: Game state and wave management
- `BootstrapSystem`: Initial game setup and entity creation

## Prefab Requirements

### Player Prefab
Must have:
- `PlayerAuthoring` component with configured stats
- Appropriate collider for physics
- Visual representation (mesh/sprite)

### Enemy Prefab  
Must have:
- `EnemyAuthoring` component with configured stats
- Appropriate collider for physics
- Visual representation (mesh/sprite)

## Debug Information

The demo includes on-screen debug information showing:
- Current game state
- Entity counts
- Score and enemy kill count
- Active system status
- Control reminders

## Troubleshooting

**Issue: No entities spawning**
- Check that prefabs are assigned in the bootstrap component
- Verify prefabs have the required authoring components
- Check console for error messages

**Issue: Input not working**
- Ensure an input system component is added to the scene
- Check that the player entity exists
- Verify Input System package is installed for modern input

**Issue: Enemies not moving**
- Check enemy prefab has EnemyAuthoring component
- Verify EnemyAISystem is running
- Check patrol settings in enemy configuration

**Issue: No combat/damage**
- Verify CombatSystem is active
- Check weapon prefab configuration
- Ensure projectiles are being spawned

## Performance Notes

- All systems are Burst-compiled for performance
- Entity queries are cached for efficiency
- Projectile cleanup prevents memory leaks
- Death effects are automatically cleaned up

## Next Steps

To extend this demo:
1. Add particle effects for weapons and explosions
2. Implement different enemy types
3. Add power-ups and weapon upgrades
4. Create multiple levels/waves
5. Add sound effects and music
6. Implement save/load functionality