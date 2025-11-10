using System.Collections;
using UnityEngine;

namespace VN
{
    public class PrologueScript : VNScript
    {
        public override IEnumerator Define(VNEngine vn)
        {
            // 시작 BGM
            yield return vn.Line("@bgm play BGM/Theme1 0.7 1.0 loop");

            yield return vn.Say("린", "여긴… 어디지?\n나는 린이야.");
            yield return vn.Say("카이", "괜찮아? 일단 숨을 고르자.");

            yield return vn.Line("@sfx SFX/click 1.0 1.0");

            yield return vn.Center("— 잠시 후 —");

            yield return vn.Say("린", "이제 좀 진정됐어.");
            yield return vn.Say("카이", "그럼 출발하자.");

            yield return vn.Line("@bgm stop 0.8");
        }
    }
}
