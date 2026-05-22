using UnityEngine;

public class BowTracker : MonoBehaviour
{
	void Update()
	{
		// Afișează în Android Logcat în fiecare secundă poziția exactă și dacă mai e activ
		if (Time.frameCount % 60 == 0)
		{
			Vector3 pos = transform.position;
			Vector3 scale = transform.localScale;
			Debug.Log($"[LOG_ARC] Pozitie: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2}) | Scala: ({scale.x:F2}, {scale.y:F2}, {scale.z:F2}) | Parinte: {(transform.parent != null ? transform.parent.name : "Niciunul")}");
		}
	}
}