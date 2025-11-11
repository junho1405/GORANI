using System.Collections;
using UnityEngine;

namespace VN
{
    /// <summary>
    /// 기능 쇼케이스: BG, BGM/SFX, 포트레이트 좌/중앙/우 표시·숨김, 선택지, 대사창 on/off
    /// 필요 리소스 예:
    /// BG  : Resources/BG/BG1, Resources/BG/bg_park_noon
    /// CH  : Resources/Chara/ye_wang_neutral, Resources/Chara/kai_smile
    /// BGM : Resources/Audio/BGM/1.harusora, Resources/Audio/BGM/park_theme
    /// SFX : Resources/Audio/SFX/click, Resources/Audio/SFX/confirm
    ///
    /// 즉시 전환 : @bg BG/천장
    /// 페이드    : @bg fade BG/천장 0.6
    /// 슬라이드  : @bg slide BG/천장 0.6 left/right/up/down
    /// 표시      : @ch show left Chara/ye_wang_neutral [scale] [fade] [flip] [dx] [dy]
    /// 숨김      : @ch hide right [fade]
    /// 크기조절  : @ch size center <scale> [sec]
    /// 흔들기    : @ch shake left <duration> <amplitude> [frequency]
    /// </summary>
    public class ExampleShowcaseScript : VNScript
    {
        public override IEnumerator Define(VNEngine vn)
        {
            // 시작 캡션 → 대화 패널 ON
            yield return vn.Center("— 데모 시작 —");
            yield return vn.Line("@dialogue on");

            // 배경/BGM
            yield return vn.Line("@bg BG/BG1");                            // 즉시 전환(시간 인자 없음)
            yield return vn.Line("@bgm play BGM/1.harusora 0.7 0.6 true"); // 경로에 Audio/ 접두어 불필요

            // 포트레이트 표시 (좌/우)
            yield return vn.Line("@ch show left Chara/ye_wang_neutral");
            yield return vn.Line("@ch show right Chara/kai_smile");

            // 대사
            yield return vn.Say("린", "안녕?\n나는 린이야.");
            yield return vn.Say("카이", "오늘 일정 정하자. 어디로 갈까?");

            // 선택지
            yield return vn.Choice(
                ("카페에 남는다", "cafe"),
                ("공원으로 간다", "park")
            );
            var c = vn.GetChoice();

            if (c == "cafe")
            {
                // 효과음
                yield return vn.Line("@sfx SFX/confirm 1.0 1.0");

                // 중앙만 강조(좌/우 숨김 후 중앙 표시)
                yield return vn.Line("@ch hide all 0.2");
                yield return vn.Line("@ch show center Chara/ye_wang_neutral 1.1 0.2");

                yield return vn.Say("린", "그럼 잠깐 더 쉬었다 가자.");
                yield return vn.Say("카이", "좋아. 음악을 조금 더 키울까?");
                yield return vn.Line("@bgm play BGM/1.harusora 0.9 0.5 true");

                // 대사창 OFF로 자막만
                yield return vn.Line("@dialogue off");
                yield return vn.Center("— 잠시 후 —");
                yield return vn.Line("@dialogue on");

                yield return vn.Say("린", "충분히 쉬었어. 이제 출발해볼까?");
            }
            else // park
            {
                // 배경/음악 전환
                yield return vn.Line("@bg BG/bg_park_noon");
                yield return vn.Line("@bgm play BGM/park_theme 0.7 0.6 true");

                // 포트레이트 교체
                yield return vn.Line("@ch hide all 0.2");
                yield return vn.Line("@ch show left Chara/kai_smile 1.0 0.2");

                yield return vn.Say("카이", "공기는 상쾌하고, 바람도 좋네.");
                yield return vn.Say("린", "그러게. 기분이 좋아졌어!");
                yield return vn.Line("@sfx SFX/click 1.0 1.1");

                // 자막용 중앙 텍스트만
                yield return vn.Line("@dialogue off");
                yield return vn.Center("— 공원 산책을 즐겼다 —");
                yield return vn.Line("@dialogue on");
            }

            // 마무리: 포트레이트/대사창/브금 제어
            yield return vn.Line("@ch hide all 0.2");
            yield return vn.Line("@bgm stop 0.8");
            yield return vn.Center("— 데모 종료 —");
            yield return vn.Line("@dialogue off");
        }
    }
}
