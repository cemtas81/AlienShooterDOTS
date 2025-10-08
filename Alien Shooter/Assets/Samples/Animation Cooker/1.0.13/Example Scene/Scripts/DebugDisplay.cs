// This class will dump output to the screen
// To use it, add an empty gameobject to the scene and attach this script to it as a component.
// you can use the parameters in the Inspector to specify the location and width/height.
//--------------------------------------------------------------------------------------------------//

using UnityEngine;
using System.Collections;

namespace AnimationCookerExample
{
 
 public class DebugDisplay : MonoBehaviour
 {
     private string m_log = "";
     private Queue m_logQueue = new Queue();

    [Tooltip("The X coordiate of the top left (default 0)")]
    public int m_x = 0;

    [Tooltip("The Y coordiate of the top left (default 0)")]
    public int m_y = 0;

    [Tooltip("If this is < 0, then the screen width will be used (default -1)")]
    public int m_width = -1;

    [Tooltip("If this is < 0, then the screen width will be used (default -1)")]
    public int m_height = -1;

    [Tooltip("Set to false if you want normal non-error/non-warning logs to be displayed as well (default true)")]
    public bool m_ignoreInformative = true;

    [Tooltip("Text color")]
    public Color m_textColor = Color.yellow;

    [Tooltip("Font size")]
    public int m_fontSize = 12;

    GUIStyle m_style = null;

    void OnEnable()
	{
         Application.logMessageReceived += HandleLog;
    }
     
     void OnDisable()
	 {
         Application.logMessageReceived -= HandleLog;
     }
 
     void HandleLog(string logString, string stackTrace, LogType type)
	 {
        if (m_ignoreInformative && (type == LogType.Log)) { return; }
         m_log = logString;
         string newString = "\n [" + type + "] : " + m_log;
         m_logQueue.Enqueue(newString);
         if (type == LogType.Exception) {
             newString = "\n" + stackTrace;
             m_logQueue.Enqueue(newString);
         }
         m_log = string.Empty;
         foreach(string log in m_logQueue) { m_log += log; }
     }
 
     void OnGUI ()
	 {
        int w = (m_width < 0) ? Screen.width : m_width;
        int h = (m_height < 0) ? Screen.height : m_height;
        GUILayout.BeginArea(new Rect(m_x, m_y, w, h));
        if (m_style == null) {
            m_style = new GUIStyle(GUI.skin.label);
            m_style.normal.textColor = m_textColor;
            m_style.fontSize = m_fontSize;
        }
        GUILayout.Label(m_log, m_style);
        GUILayout.EndArea();
     }
 }

} //  namespace