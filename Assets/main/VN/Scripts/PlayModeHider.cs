// Assets/main/VN/Scripts/PlayModeHider.cs
using UnityEngine;

[DisallowMultipleComponent]
public class PlayModeHider : MonoBehaviour
{
    [Tooltip("비활성화 대신 완전히 파괴합니다. (플레이 중만)")]
    public bool destroyInstead = false;

    void Awake()
    {
        if (!Application.isPlaying) return;

        if (destroyInstead)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
