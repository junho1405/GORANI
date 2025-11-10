using System.Collections;
using UnityEngine;

namespace VN
{
    // 예: Assets/main/VN/Scripts/Chapter1Script.cs
    public class Chapter1Script : VNScript
    {
        public override IEnumerator Define(VNEngine vn)
        {
            // 간단 예시: wait/center/dialogue 커맨드 사용
            yield return vn.Line("@wait 0.2");
            yield return vn.Line("@center 중앙 자막 테스트");
            yield return vn.Line("@dialogue");

            yield return vn.Say("카이", "오늘은 어디서 시작할까?");
            yield return vn.Say("플레이어", "좋아, 가보자.");
        }
    }
}
