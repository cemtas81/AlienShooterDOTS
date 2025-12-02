# AlienShooterDOTS

A small, working top‑down 3D alien shooter built with Unity DOTS (Entities, Jobs, Burst). This repository is a DOTS-based example/demo showing ECS architecture, Burst-compiled systems and integration with Entities Graphics, Unity Physics, and the modern Input System.

Important technical snapshots
- Recommended Unity Editor: 6000.2.6f2 (see ProjectSettings/ProjectVersion.txt)
- Notable packages (Packages/manifest.json):
  - com.unity.entities 1.3.14
  - com.unity.entities.graphics 1.4.12
  - com.unity.physics 1.3.14
  - com.unity.burst 1.8.24
  - com.unity.mathematics 1.3.2
  - com.unity.render-pipelines.universal 17.2.0
  - com.unity.inputsystem 1.14.2
- License: MIT

Quick start — open and play
1. Clone the repo:
   git clone https://github.com/cemtas81/AlienShooterDOTS.git
2. Open Unity Hub and add the project. Select the "Alien Shooter" folder (project root contains a space in the folder name).
3. Open the project in Unity Editor version matching ProjectVersion.txt (6000.2.6f2) or a compatible 6.x release.
4. Let Unity Package Manager resolve and install packages (internet required). Git-based packages will download from their remote URLs.
5. To start quickly:
   - Use the editor menu command (if present): AlienShooterDOTS > Create Working Demo Scene
   - Or manually: create a new Scene, add an empty GameObject, add the bootstrap/authoring components (e.g. SimpleGameObjectBootstrap / PlayerAuthoring), assign Player/Enemy prefabs from Assets/AlienShooterDOTS/Prefabs, add the PlayerInputSystem component, then press Play.

Controls
- Move: WASD
- Shoot: Mouse (0)

What’s in the project (high level)
- Assets/AlienShooterDOTS/
  - Core/ — ECS component definitions and core systems (Player, Enemy, Weapon, etc.)
  - Gameplay/ — GameManager, wave & spawn logic, scoring
  - Integration/ — GPU animation, navigation, physics & input integration layers
  - Authoring/ — GameObject → Entity authoring components and prefabs
  - Examples/ — example scenes and usage samples
  - Editor/ — custom editor tools and configuration windows
- Packages/ — package manifest and any package subfolders
- ProjectSettings/ — Unity project configuration (ProjectVersion.txt included)

DOTS & performance notes
- Systems use IComponentData and job-friendly patterns, with Burst compiled hot paths where applicable.
- Intended to scale to many entities using native collections, EntityCommandBuffer for structural changes, and efficient queries.
- Entities Graphics and GPU animation integrations are present for rendering optimized characters.

Dependencies and gotchas
- Several packages are referenced by Git URLs (e.g. damage-numbers-DOTS, animationcooker). These require network access and valid remote repos.
- Mismatched Unity or package versions can cause compile or package resolution errors — prefer the ProjectVersion.txt editor or Unity 6.2.x.
- If a package fails to resolve, check Packages/manifest.json and the Unity Package Manager logs.

Troubleshooting checklist
- Missing packages: open Window > Package Manager and reinstall/refresh packages.
- Input not responding: ensure PlayerInputSystem (or LegacyPlayerInputSystem) component exists in the scene and actions are configured.
- Entities not spawning: verify authoring components are correctly assigned on prefabs and GameManager/Bootstrap is present.
- Performance issues: enable Burst, profile with the Unity Profiler and Entities Debugger (Window > Analysis > Entities Debugger).

Development & contribution guide
- Coding style: follow ECS best practices — components as structs (IComponentData), keep managed allocations out of hot loops, prefer IJobEntity and Burst for performance-critical systems.
- To contribute:
  1. Fork the repository.
  2. Create a topic branch for your change.
  3. Make changes and include a short description and tests where applicable.
  4. Open a pull request describing the change and rationale.
- For larger design changes, open an issue first to discuss approach (especially if changing core systems or package versions).

Extending the project (common tasks)
- Add a new enemy type:
  1. Extend EnemyType enum and related components.
  2. Create an authoring prefab with EnemyAuthoring.
  3. Add spawn logic in GameManager or spawn system.
  4. Add any AI behaviors in EnemyAISystem.
- Add a weapon:
  1. Add weapon type to enums and components.
  2. Create weapon prefab and authoring component.
  3. Configure projectile behavior and add to WeaponSystem.

License
- MIT — see LICENSE file for details.

Where to go from here
- Inspect Examples/ for ready scenes and authoring usage.
