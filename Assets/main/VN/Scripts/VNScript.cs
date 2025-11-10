using System.Collections;
using UnityEngine;

namespace VN
{
    /// <summary>
    /// 모든 시나리오 스크립트는 이걸 상속합니다.
    /// </summary>
    public abstract class VNScript : MonoBehaviour
    {
        public abstract IEnumerator Define(VNEngine vn);
    }
}
