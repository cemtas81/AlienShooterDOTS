using Unity.Entities;

/// <summary>
/// Enemy avoidance + player'dan uzak durma parametreleri
/// </summary>
public struct EnemyAvoidance : IComponentData
{
    /// <summary>Diðer düþmanlarý algýlama yarýçapý</summary>
    public float DetectionRadius;
    /// <summary>Opsiyonel açý (þu an kullanýlmýyor, geleceðe býrakýldý)</summary>
    public float MaxAngle;
    /// <summary>Düþman separation kuvveti çarpaný (0-1)</summary>
    public float AvoidanceStrength;
    /// <summary>Player'a yaklaþýnca geri itilecek minimum mesafe yarýçapý</summary>
    public float PlayerSeparationRadius;
}