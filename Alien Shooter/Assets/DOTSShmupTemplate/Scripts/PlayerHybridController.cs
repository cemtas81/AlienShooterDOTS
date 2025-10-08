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
    private Rigidbody rb;
    [Header("Rotation")]
    public Camera mainCamera;

    [Header("DOTS Integration")]
    public GameObject bulletPrefabGO; // DOTS'a bake edilmiş BulletAuthoring prefabı
    public Transform firePoint;
    
    private EntityManager entityManager;
    private Entity bulletPrefabEntity;
    private Entity playerEntity;
    [SerializeField]private Animator playerAnimator;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        rb = GetComponent<Rigidbody>();
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
                typeof(PlayerInput),
                typeof(PlayerFirePoint)
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
    private void Update()
    {
        
        UpdatePlayerEntityPosition();
        UpdateFirePointPosition(); // Yeni eklenen çağrı
        HandleInputBridge();
    }

    private void FixedUpdate()
    {
        HandleRotation();
        HandleMovement();
    }
    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 moveInput = new(h, 0, v);
        if (moveInput.magnitude > 1f) moveInput.Normalize();
        rb.MovePosition(rb.position+moveSpeed * Time.fixedDeltaTime * moveInput); // Rigidbody ile çarpışma için
        AnimateThePlayer(moveInput);
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
        Plane ground = new(Vector3.up, transform.position);
        if (ground.Raycast(ray, out float distance))
        {
            Vector3 hit = ray.GetPoint(distance);
            Vector3 dir = hit - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
            {
                // Transform rotasyonunu direkt değiştir
                Quaternion targetRotation = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
                // Rigidbody rotasyonunu transform'a göre güncelle
                rb.rotation = transform.rotation;
            }
        }
    }
    // FixedUpdate içinde UpdatePlayerEntityPosition() çağrısından sonra ekleyin:
    void UpdateFirePointPosition()
    {
        if (entityManager.Exists(playerEntity) && firePoint != null)
        {
            entityManager.SetComponentData(playerEntity, new PlayerFirePoint
            {
                Position = (float3)firePoint.position
            });
        }
    }
    void AnimateThePlayer(Vector3 desiredDirection)
    {
        if (!playerAnimator)
            return;

        Vector3 movement = new (desiredDirection.x, 0f, desiredDirection.z);
        float forw = Vector3.Dot(movement, transform.forward);
        float stra = Vector3.Dot(movement, transform.right);

        playerAnimator.SetFloat("Forward", forw);
        playerAnimator.SetFloat("Strafe", stra);
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