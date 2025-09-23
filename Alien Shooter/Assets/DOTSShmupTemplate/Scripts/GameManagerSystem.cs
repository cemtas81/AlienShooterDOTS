using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[BurstCompile]
public partial struct GameManagerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Skoru güncelle (örnek: öldürülen düşman sayısı kadar artır)
        var gameScoreEntity = SystemAPI.GetSingletonEntity<GameScore>();
        var gameScore = SystemAPI.GetComponentRW<GameScore>(gameScoreEntity);

        // Öldürülen düşmanları say (bu örnekte, sadece skor artırma için örnek olarak kullanılıyor)
        // Daha gelişmiş bir sistem için EnemyDamageSystem'da skor güncellenebilir!
        // Burada sadece örnek olarak space tuşuna basınca skor artırılıyor.
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            gameScore.ValueRW.Value += 1;
            Debug.Log("Score: " + gameScore.ValueRW.Value);
        }

        // Oyunu sıfırlama (örnek: R tuşuna basınca sahneyi baştan yükle)
        if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }
}