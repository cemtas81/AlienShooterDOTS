using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Auto Aim")]
    public bool autoAim = true;
    [SerializeField] private float targetHeight = 1.2f;
    [SerializeField] private float maxTerrainRayDistance = 100f; // Terrain raycast max mesafesi

    [Header("DOTS Integration")]
    public GameObject bulletPrefabGO; // DOTS'a bake edilmiş BulletAuthoring prefabı
    public Transform firePoint;

    private EntityManager entityManager;
    private Entity bulletPrefabEntity;
    private Entity playerEntity;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Transform target;
    [SerializeField] private LayerMask floorMask = -1; // Terrain layer mask

    private const float CAM_RAY_LENGTH = 100f;
    private const float ENEMY_HIT_RADIUS = 2f;

    // Job system için gerekli değişkenler
    private JobHandle enemyHitJobHandle;
    private NativeReference<EnemyHitResult> hitResult;
    private bool hitResultAllocated = false;
    public TMP_Text AimFormat;
    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        rb = GetComponent<Rigidbody>();

        // NativeReference'ı allocate et
        hitResult = new NativeReference<EnemyHitResult>(Allocator.Persistent);
        hitResultAllocated = true;

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

    void OnDestroy()
    {
        // Job'ın tamamlanmasını bekle ve NativeReference'ı dispose et
        if (hitResultAllocated)
        {
            enemyHitJobHandle.Complete();
            hitResult.Dispose();
            hitResultAllocated = false;
        }
    }
    public void ToggleAimFormat()
    {       
        autoAim =!autoAim;
        AimFormat.text = "AutoAim" + autoAim.ToString();
    }
    private void Update()
    {
        UpdatePlayerEntityPosition();
        UpdateFirePointPosition();
        HandleInputBridge();

        // Runtime'da auto-aim toggle için
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            autoAim = !autoAim;
            Debug.Log($"Auto Aim: {(autoAim ? "ON" : "OFF")}");
        }
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
        rb.MovePosition(rb.position + moveSpeed * Time.fixedDeltaTime * moveInput);
        AnimateThePlayer(moveInput);
    }

    void UpdatePlayerEntityPosition()
    {
        if (entityManager.Exists(playerEntity))
        {
            var current = entityManager.GetComponentData<LocalTransform>(playerEntity);
            current.Position = (float3)transform.position;
            current.Rotation = (quaternion)transform.rotation;
            entityManager.SetComponentData(playerEntity, current);
        }
    }

    void HandleRotation()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        float3 targetPos;

        if (autoAim)
        {
            targetPos = GetAutoAimTarget();
        }
        else
        {
            targetPos = GetTerrainAwareTarget();
        }

        // Target pozisyonunu güncelle (artık terrain yüksekliği + targetHeight)
        target.position = new Vector3(targetPos.x, targetPos.y + targetHeight, targetPos.z);

        // Player rotasyonu
        Vector3 playerToTarget = new(targetPos.x - transform.position.x,
                                    0f,
                                    targetPos.z - transform.position.z);

        if (playerToTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(playerToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.2f);
        }
    }

    float3 GetAutoAimTarget()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Önceki job'ın tamamlanmasını bekle
        enemyHitJobHandle.Complete();

        // Enemy entity'leri sorgula
        var enemyQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<EnemyTag>(),
            ComponentType.ReadOnly<LocalTransform>());

        // Eğer enemy var ise job çalıştır
        if (enemyQuery.CalculateEntityCount() > 0)
        {
            using var enemyTransforms = enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            // Job'ı hazırla ve çalıştır
            var enemyHitJob = new EnemyHitDetectionJob
            {
                EnemyTransforms = enemyTransforms,
                RayOrigin = ray.origin,
                RayDirection = ray.direction,
                HitRadius = ENEMY_HIT_RADIUS,
                Result = hitResult
            };

            enemyHitJobHandle = enemyHitJob.Schedule();
            enemyHitJobHandle.Complete(); // Bu frame'de sonuca ihtiyacımız var

            var result = hitResult.Value;
            if (result.HitEnemy)
            {
                return result.TargetPosition;
            }
        }

        // Enemy'ye hit etmediysek terrain-aware target yap
        return GetTerrainAwareTarget();
    }

    float3 GetTerrainAwareTarget()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Terrain/ground ile raycast yap
        if (Physics.Raycast(ray, out RaycastHit hit, maxTerrainRayDistance, floorMask))
        {
            return hit.point; // Gerçek terrain yüksekliği
        }

        // Fallback: player'ın önündeki pozisyon (terrain yüksekliğinde)
        Vector3 fallbackPos = transform.position + transform.forward * 10f;

        // Fallback pozisyonu için de terrain yüksekliği al
        if (Physics.Raycast(new Vector3(fallbackPos.x, fallbackPos.y + 10f, fallbackPos.z),
                          Vector3.down, out RaycastHit groundHit, 20f, floorMask))
        {
            return groundHit.point;
        }

        // Son fallback: transform yüksekliği
        return (float3)transform.position;
    }

    void UpdateFirePointPosition()
    {
        if (entityManager.Exists(playerEntity) && firePoint != null)
        {
            entityManager.SetComponentData(playerEntity, new PlayerFirePoint
            {
                Position = (float3)firePoint.position,
                Rotation = (quaternion)firePoint.rotation
            });
        }
    }

    void AnimateThePlayer(Vector3 desiredDirection)
    {
        if (!playerAnimator)
            return;

        Vector3 movement = new(desiredDirection.x, 0f, desiredDirection.z);
        float forw = Vector3.Dot(movement, transform.forward);
        float stra = Vector3.Dot(movement, transform.right);

        playerAnimator.SetFloat("Forward", forw);
        playerAnimator.SetFloat("Strafe", stra);
    }

    void HandleInputBridge()
    {
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