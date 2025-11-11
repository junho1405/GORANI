// Assets/main/VN/Scripts/CameraCullingDevHelper.cs
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public class CameraCullingDevHelper : MonoBehaviour
{
    [Tooltip("작업용 오브젝트가 있는 레이어 이름 (예: DevHelper)")]
    public string devLayerName = "DevHelper";

    [Tooltip("플레이 중에만 CullingMask에서 devLayer를 제외합니다.")]
    public bool runtimeOnly = true;

    void Awake()
    {
        if (runtimeOnly && !Application.isPlaying) return;

        var cam = GetComponent<Camera>();
        if (!cam)
        {
            cam = Camera.main;
            if (!cam)
            {
                Debug.LogWarning("[CameraCullingDevHelper] Camera를 찾지 못했습니다.");
                return;
            }
        }

        int devLayer = LayerMask.NameToLayer(devLayerName);
        if (devLayer < 0)
        {
            Debug.LogWarning($"[CameraCullingDevHelper] 레이어 '{devLayerName}' 가 존재하지 않습니다. (Project Settings > Tags and Layers 에서 추가)");
            return;
        }

        // 해당 레이어 비트만 꺼주기
        cam.cullingMask &= ~(1 << devLayer);
    }
}
