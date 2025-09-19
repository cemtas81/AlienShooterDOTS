using System.Collections;
using TMPro;
using Unity.Entities;
using UnityEngine;

using SnivelerCode.Samples.Components;

namespace SnivelerCode.Samples.Gui
{
    public class WalkingStats : MonoBehaviour
    {
        [SerializeField] 
        TMP_Text fpsLabel;
        
        [SerializeField]
        TMP_Text entriesLabel;
        Coroutine m_Coroutine;
        
        void Start() => StartCoroutine(StatsUpdate());
        
        IEnumerator StatsUpdate()
        {
            yield return null;
            var manager = World.All[0].EntityManager;
            var query = manager.CreateEntityQuery(typeof(WalkingMinionSpeed));
            while (true)
            {
                yield return new WaitForSeconds(0.2f);
                fpsLabel.text = $"fps: {(int)(1.0f / Time.smoothDeltaTime)}";
                entriesLabel.text = $"entries: {query.CalculateEntityCount()}";    
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }   
}


