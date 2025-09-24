
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Hybrid Player controller: GameObject hareket ve rotasyonu, DOTS ile köprü.
/// DOTS düşmanları ve sistemleriyle etkileşim için PlayerTag, LocalTransform ve PlayerInput componentlerini eksiksiz ekler.
/// Ateş etme, DOTS bullet prefabını doğru şekilde instantiate eder.
/// </summary>
public class PlayerHybridController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Rotation")]
    public Camera mainCamera;

    [Header("DOTS Integration")]
    public GameObject bulletPrefabGO; // DOTS'a bake edilmiş BulletAuthoring prefabı
    public Transform firePoint;

    private EntityManager entityManager;
    private Entity bulletPrefabEntity;
    private Entity playerEntity;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // DOTS bullet prefab entity'sini bul
        var query = entityManager.CreateEntityQuery(typeof(BulletPrefabReference));
        if (query.CalculateEntityCount() > 0)
            bulletPrefabEntity = entityManager.GetComponentData<BulletPrefabReference>(
                query.GetSingletonEntity()).Prefab;
        else
            Debug.LogError("BulletPrefabReference bulunamadı, DOTS bullet prefabı eksik olabilir.");

        // PlayerTag entity'sini bul veya eksiksiz olarak oluştur
        var playerQuery = entityManager.CreateEntityQuery(typeof(PlayerTag));
        if (playerQuery.CalculateEntityCount() == 0)
        {
            playerEntity = entityManager.CreateEntity(
                typeof(PlayerTag),
                typeof(LocalTransform),
                typeof(PlayerInput)
            );
            entityManager.SetComponentData(playerEntity, LocalTransform.FromPosition((float3)transform.position));
            entityManager.SetComponentData(playerEntity, new PlayerInput { Move = float2.zero, Fire = false });
        }
        else
        {
            playerEntity = playerQuery.GetSingletonEntity();
            // LocalTransform ve PlayerInput eksikse ekle
            if (!entityManager.HasComponent<LocalTransform>(playerEntity))
                entityManager.AddComponentData(playerEntity, LocalTransform.FromPosition((float3)transform.position));
            if (!entityManager.HasComponent<PlayerInput>(playerEntity))
                entityManager.AddComponentData(playerEntity, new PlayerInput { Move = float2.zero, Fire = false });
        }
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
        UpdatePlayerEntityPosition();
        HandleInputBridge();
  
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 moveInput = new Vector3(h, 0, v);
        if (moveInput.magnitude > 1f) moveInput.Normalize();

        transform.position += moveInput * moveSpeed * Time.deltaTime;
    }
    void UpdatePlayerEntityPosition()
    {
        if (entityManager.Exists(playerEntity))
        {
            var current = entityManager.GetComponentData<LocalTransform>(playerEntity);
            current.Position = (float3)transform.position;
            current.Rotation = (quaternion)transform.rotation; // <-- Burası sokomelli!
            entityManager.SetComponentData(playerEntity, current);
        }
    }
    void HandleRotation()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, transform.position);
        if (ground.Raycast(ray, out float distance))
        {
            Vector3 hit = ray.GetPoint(distance);
            Vector3 dir = hit - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }

    void HandleInputBridge()
    {
        // DOTS sisteminin input ile uyumlu çalışması için PlayerInput componentini güncelliyoruz
        if (entityManager.Exists(playerEntity))
        {
            var input = new PlayerInput
            {
                Move = new float2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                Fire = Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1")
            };
            entityManager.SetComponentData(playerEntity, input);
        }
    }
}