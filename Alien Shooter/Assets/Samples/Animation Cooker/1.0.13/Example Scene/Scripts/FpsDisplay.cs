// i yanked this class from the internet... stackoverflow or something.
// i added the ability to change color and font size
// i added the ability to set target frame rate and vsync count.
// i added the ability to set where on the screen the text is anchored.
// i added the display for frame averages
//--------------------------------------------------------------------------------------------------//

using UnityEngine;

namespace AnimationCookerExample
{

public class FpsDisplay : MonoBehaviour
{
	[Tooltip("An integer number for font size that scales with screen size, 1..n (default 2)")]
	public int m_fontSize = 4;

	[Tooltip("Color of the text to use (default null)")]
	public Color m_textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);

	[Tooltip("The target frame-rate for the app. If set it to zero, the rate won't be set. (default 0)")]
	public int m_appTargetFrameRate = 0;

	[Tooltip("The vsync count for the app. If set to zero, the count won't be set. (default 0)")]
	public int m_vSyncCount = 0;

	[Tooltip("Determines how the text is aligned on the screen. (default UpperLeft)")]
	public TextAnchor m_alignment = TextAnchor.UpperLeft;

	[Tooltip("The number of frames to average. (default 60)")]
	public int m_frameCountForAvg = 60;

	float m_deltaTime = 0.0f;
	float m_y;
	float m_height;
	float m_totalDelta = 0f;
	int m_frameCount = 0;
	float m_avgFps = 0f;
	float m_avgMsec = 0f;

	private void Awake()
    {
		if (m_appTargetFrameRate >= 0) { Application.targetFrameRate = m_appTargetFrameRate; }
		if (m_vSyncCount >= 0) { QualitySettings.vSyncCount = m_vSyncCount; }

		m_height = (Screen.height * m_fontSize) / 100;
		if ((m_alignment == TextAnchor.LowerCenter) || (m_alignment == TextAnchor.LowerLeft) || (m_alignment == TextAnchor.LowerRight)) {
			m_y = Screen.height - m_height;
		} else if ((m_alignment == TextAnchor.MiddleCenter) || (m_alignment == TextAnchor.MiddleLeft) || (m_alignment == TextAnchor.MiddleRight)) {
			m_y = (Screen.height / 2f) - m_height;
		}
	}

    private void OnEnable()
    {
        m_totalDelta = 0f;
		m_frameCount = 0;
		m_avgMsec = 0f;
		m_avgFps = 0f;
		m_deltaTime = 0.0f;
    }

    void Update()
	{
		m_deltaTime += (Time.unscaledDeltaTime - m_deltaTime) * 0.1f;
		m_totalDelta += m_deltaTime;
		//UnityEngine.Debug.Log($"m_frameCount: {m_frameCount}, m_deltaTime: {m_deltaTime}, m_totalDelta: {m_totalDelta}");
		m_frameCount++;
		if (m_frameCount >= m_frameCountForAvg) {
			float avgDelta = m_totalDelta / m_frameCountForAvg;
			m_avgMsec = avgDelta * 1000.0f;
			m_avgFps = avgDelta > 0 ? 1f / avgDelta : 0f;
			// reset accumulators
			m_totalDelta = 0f;
			m_frameCount = 0;
		}
	}

	void OnGUI()
	{
		int h = Screen.height;
		GUIStyle style = new GUIStyle();
		Rect rect = new Rect(0, m_y, Screen.width, m_height);
		style.alignment = m_alignment;
		style.fontSize = (int)m_height;
		style.normal.textColor = m_textColor;
		float msec = m_deltaTime * 1000.0f;
		float fps = (m_deltaTime > 0) ? (1.0f / m_deltaTime) : 0f;
		string text = string.Format("NOW: {0:0.0} ms, {1:0.} fps", msec, fps);
		GUI.Label(rect, text, style);
		text = string.Format("AVG: {0:0.0} ms, {1:0.} fps", m_avgMsec, m_avgFps);
		rect.y += rect.height;
		GUI.Label(rect, text, style);
	}
}

} // namespace