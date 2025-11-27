using Unity.Entities;

/// <summary>
/// Basit agent collider komponenti - çarpýþma engellemesi için
/// </summary>
public struct EnemyCollider : IComponentData, IEnableableComponent
{
    /// <summary>Collider aktif mi?</summary>
    public bool IsEnabled;
}