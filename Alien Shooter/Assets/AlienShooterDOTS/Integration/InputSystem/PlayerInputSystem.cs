using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using AlienShooterDOTS.Core.Components;

namespace AlienShooterDOTS.Integration.InputSystem
{
    /// <summary>
    /// Sample Input System integration for player input
    /// This demonstrates how to bridge Unity Input System with DOTS
    /// </summary>
    public class PlayerInputSystem : MonoBehaviour
    {
        [Header("Input Settings")]
        public float MovementDeadzone = 0.1f;
        public bool InvertYAxis = false;

        // Input Action references
        private InputAction _moveAction;
        private InputAction _fireAction;
        private InputAction _dashAction;
        private InputAction _reloadAction;

        // Cached input values
        private Vector2 _movementInput;
        private bool _firePressed;
        private bool _dashPressed;
        private bool _reloadPressed;

        // Entity references
        private Entity _playerEntity;
        private EntityManager _entityManager;

        void Start()
        {
            // Get entity manager
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // Find player entity
            FindPlayerEntity();

            // Set up input actions
            SetupInputActions();
        }

        void Update()
        {
            // Update input values
            UpdateInputValues();

            // Apply input to player entity
            ApplyInputToPlayer();
        }

        void OnDestroy()
        {
            // Clean up input actions
            CleanupInputActions();
        }

        private void FindPlayerEntity()
        {
            // Find the player entity in the world
            var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(PlayerInput));
            
            if (playerQuery.CalculateEntityCount() > 0)
            {
                var entities = playerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                _playerEntity = entities[0]; // Get first player entity
                entities.Dispose();
            }

            playerQuery.Dispose();
        }

        private void SetupInputActions()
        {
            // Create input actions programmatically
            // In a real project, these would typically come from an Input Action Asset

            _moveAction = new InputAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            _fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            _fireAction.AddBinding("<Gamepad>/rightTrigger");
            _fireAction.AddBinding("<Keyboard>/space");

            _dashAction = new InputAction("Dash", InputActionType.Button, "<Keyboard>/leftShift");
            _dashAction.AddBinding("<Gamepad>/buttonSouth"); // A button on Xbox controller

            _reloadAction = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
            _reloadAction.AddBinding("<Gamepad>/buttonWest"); // X button on Xbox controller

            // Enable all actions
            _moveAction.Enable();
            _fireAction.Enable();
            _dashAction.Enable();
            _reloadAction.Enable();
        }

        private void UpdateInputValues()
        {
            // Read input values
            Vector2 rawMovement = _moveAction.ReadValue<Vector2>();
            
            // Apply deadzone
            if (rawMovement.magnitude < MovementDeadzone)
            {
                _movementInput = Vector2.zero;
            }
            else
            {
                _movementInput = rawMovement;
                
                // Invert Y axis if needed
                if (InvertYAxis)
                {
                    _movementInput.y = -_movementInput.y;
                }
            }

            _firePressed = _fireAction.IsPressed();
            _dashPressed = _dashAction.WasPressedThisFrame();
            _reloadPressed = _reloadAction.WasPressedThisFrame();
        }

        private void ApplyInputToPlayer()
        {
            // Make sure we have a valid player entity
            if (_playerEntity == Entity.Null || !_entityManager.Exists(_playerEntity))
            {
                FindPlayerEntity();
                return;
            }

            // Update player input component
            if (_entityManager.HasComponent<PlayerInput>(_playerEntity))
            {
                var playerInput = _entityManager.GetComponentData<PlayerInput>(_playerEntity);
                
                playerInput.MovementInput = new float2(_movementInput.x, _movementInput.y);
                playerInput.FirePressed = _firePressed;
                playerInput.DashPressed = _dashPressed;
                playerInput.ReloadPressed = _reloadPressed;

                _entityManager.SetComponentData(_playerEntity, playerInput);
            }
        }

        private void CleanupInputActions()
        {
            // Dispose of input actions
            _moveAction?.Dispose();
            _fireAction?.Dispose();
            _dashAction?.Dispose();
            _reloadAction?.Dispose();
        }

        // Public methods for external control (useful for AI or testing)
        public void SetMovementInput(Vector2 movement)
        {
            _movementInput = movement;
        }

        public void SetFireInput(bool pressed)
        {
            _firePressed = pressed;
        }

        public void SetDashInput(bool pressed)
        {
            _dashPressed = pressed;
        }

        public void SetReloadInput(bool pressed)
        {
            _reloadPressed = pressed;
        }

        // Debug information
        void OnGUI()
        {
            if (Application.isPlaying && _playerEntity != Entity.Null)
            {
                GUILayout.BeginArea(new Rect(10, 10, 300, 150));
                GUILayout.Label("Player Input Debug");
                GUILayout.Label($"Movement: {_movementInput}");
                GUILayout.Label($"Fire: {_firePressed}");
                GUILayout.Label($"Dash: {_dashPressed}");
                GUILayout.Label($"Reload: {_reloadPressed}");
                GUILayout.Label($"Player Entity: {_playerEntity}");
                GUILayout.EndArea();
            }
        }
    }

    /// <summary>
    /// Alternative input system using Unity's legacy input system
    /// This can be used as a fallback or for simpler input needs
    /// </summary>
    public class LegacyPlayerInputSystem : MonoBehaviour
    {
        [Header("Input Keys")]
        public KeyCode FireKey = KeyCode.Space;
        public KeyCode DashKey = KeyCode.LeftShift;
        public KeyCode ReloadKey = KeyCode.R;

        [Header("Input Settings")]
        public string HorizontalAxis = "Horizontal";
        public string VerticalAxis = "Vertical";
        public float MovementDeadzone = 0.1f;

        private Entity _playerEntity;
        private EntityManager _entityManager;

        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            FindPlayerEntity();
        }

        void Update()
        {
            if (_playerEntity == Entity.Null || !_entityManager.Exists(_playerEntity))
            {
                FindPlayerEntity();
                return;
            }

            UpdatePlayerInput();
        }

        private void FindPlayerEntity()
        {
            var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(PlayerInput));
            
            if (playerQuery.CalculateEntityCount() > 0)
            {
                var entities = playerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                _playerEntity = entities[0];
                entities.Dispose();
            }

            playerQuery.Dispose();
        }

        private void UpdatePlayerInput()
        {
            if (!_entityManager.HasComponent<PlayerInput>(_playerEntity))
                return;

            // Read legacy input
            float horizontal = Input.GetAxis(HorizontalAxis);
            float vertical = Input.GetAxis(VerticalAxis);

            // Apply deadzone
            Vector2 movement = new Vector2(horizontal, vertical);
            if (movement.magnitude < MovementDeadzone)
            {
                movement = Vector2.zero;
            }

            bool firePressed = Input.GetKey(FireKey);
            bool dashPressed = Input.GetKeyDown(DashKey);
            bool reloadPressed = Input.GetKeyDown(ReloadKey);

            // Update player input component
            var playerInput = _entityManager.GetComponentData<PlayerInput>(_playerEntity);
            
            playerInput.MovementInput = new float2(movement.x, movement.y);
            playerInput.FirePressed = firePressed;
            playerInput.DashPressed = dashPressed;
            playerInput.ReloadPressed = reloadPressed;

            _entityManager.SetComponentData(_playerEntity, playerInput);
        }
    }

    /// <summary>
    /// Input system for AI-controlled entities
    /// This can be used to provide input to entities controlled by AI
    /// </summary>
    public class AIInputProvider : MonoBehaviour
    {
        [Header("AI Input Settings")]
        public Entity TargetEntity;
        public float UpdateInterval = 0.1f;

        private float _lastUpdateTime;
        private EntityManager _entityManager;

        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        void Update()
        {
            if (Time.time - _lastUpdateTime >= UpdateInterval)
            {
                UpdateAIInput();
                _lastUpdateTime = Time.time;
            }
        }

        private void UpdateAIInput()
        {
            if (TargetEntity == Entity.Null || !_entityManager.Exists(TargetEntity))
                return;

            if (!_entityManager.HasComponent<PlayerInput>(TargetEntity))
                return;

            // Generate AI input based on some logic
            var aiInput = GenerateAIInput();

            // Apply to target entity
            _entityManager.SetComponentData(TargetEntity, aiInput);
        }

        private PlayerInput GenerateAIInput()
        {
            // Simple AI input generation
            // In a real implementation, this would be much more sophisticated
            
            return new PlayerInput
            {
                MovementInput = new float2(
                    Mathf.Sin(Time.time) * 0.5f,
                    Mathf.Cos(Time.time) * 0.5f
                ),
                FirePressed = UnityEngine.Random.value > 0.8f,
                DashPressed = UnityEngine.Random.value > 0.95f,
                ReloadPressed = UnityEngine.Random.value > 0.98f
            };
        }

        public void SetTargetEntity(Entity entity)
        {
            TargetEntity = entity;
        }
    }
}