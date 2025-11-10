using System.Collections;
using UnityEngine;

namespace VN
{
    /// <summary>
    /// 엔진 기능 쇼케이스 스크립트
    /// 사용 리소스 예시(프로젝트에 존재해야 함):
    /// - BG:    Resources/BG/bg_cafe_morning, Resources/BG/bg_park_noon
    /// - BGM:   Resources/Audio/BGM/cafe_day, Resources/Audio/BGM/park_theme
    /// - SFX:   Resources/Audio/SFX/click, Resources/Audio/SFX/confirm
    /// </summary>
    public class ExampleShowcaseScript : VNScript
    {
        public override IEnumerator Define(VNEngine vn)
        {
            // 시작: 중앙 캡션 → 대화 패널 복귀
            yield return vn.Line("@center — 데모 시작 —");
            yield return vn.Line("@dialogue");

            // 배경 이미지 & BGM 시작
            yield return vn.Line("@bg BG/bg_cafe_morning");
            yield return vn.Line("@bgm play Audio/BGM/cafe_day 0.7 0.6 true");

            // SFX (클릭)
            yield return vn.Line("@sfx Audio/SFX/click 1.0 1.0");

            // 이름/대사 + 줄바꿈 예시
            yield return vn.Say("린", "안녕?\n나는 린이야.");
            yield return vn.Say("카이", "오늘 일정 정하자. 어디로 갈까?");

            // 선택지 표시
            yield return vn.Choice(
                ("카페에 남는다", "cafe"),
                ("공원으로 간다", "park")
            );

            var choice = vn.GetChoice();

            if (choice == "cafe")
            {
                // 카페 루트
                yield return vn.Line("@sfx Audio/SFX/confirm 1.0 1.0");
                yield return vn.Say("린", "그럼 잠깐 더 쉬었다 가자.");
                yield return vn.Say("카이", "좋아. 음악을 조금 더 키울까?");

                // BGM 교체(같은 곡 재생도 가능)
                yield return vn.Line("@bgm play Audio/BGM/cafe_day 0.9 0.5 true");

                // 잠시 대기 → 중앙 자막
                yield return vn.Line("@wait 0.6");
                yield return vn.Line("@center — 잠시 후 —");
                yield return vn.Line("@dialogue");

                yield return vn.Say("린", "충분히 쉬었어. 이제 출발해볼까?");
            }
            else // "park"
            {
                // 배경/음악 전환
                yield return vn.Line("@bg BG/bg_park_noon");
                yield return vn.Line("@bgm play Audio/BGM/park_theme 0.7 0.6 true");

                yield return vn.Say("카이", "공기는 상쾌하고, 바람도 좋네.");
                yield return vn.Say("린", "그러게. 기분이 좋아졌어!");
                yield return vn.Line("@sfx Audio/SFX/click 1.0 1.1");

                yield return vn.Line("@wait 0.4");
                yield return vn.Line("@center — 공원 산책을 즐겼다 —");
                yield return vn.Line("@dialogue");
            }

            // 마무리: BGM 페이드아웃 & 엔딩 캡션
            yield return vn.Line("@bgm stop 0.8");
            yield return vn.Line("@center — 데모 종료 —");
        }
    }
}
