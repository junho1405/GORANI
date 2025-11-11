using System.Collections;
using UnityEngine;

namespace VN
{
    /// <summary>
    /// 기능 쇼케이스: BG, BGM/SFX, 포트레이트 좌/중앙/우 표시·숨김, 선택지, 대사창 on/off
    /// 필요 리소스 예:
    /// BG  : Resources/BG/BG1
    /// CH  : Resources/Chara/ye_wang_neutral, Resources/Chara/kai_smile
    /// BGM : Resources/Audio/BGM/1.harusora, Resources/Audio/BGM/park_theme
    /// SFX : Resources/Audio/SFX/click, Resources/Audio/SFX/confirm
    /// </summary>
    public class PrologueScript : VNScript
    {
        public override IEnumerator Define(VNEngine vn)
        {
            //이름 입력
            // 배경/BGM
            yield return vn.Line("@bg BG/검은화면");
            yield return vn.Line("@bgm play BGM/1.harusora 0.7 0.6 true");
            yield return vn.Line("@center 당신의 이름은?");
            yield return vn.Line("@askname Player 당신의 이름을 입력하세요");
            //yield return vn.Line("@bg BG/BG1");
            // 시작 캡션 → 대화 패널 복귀
            yield return vn.Line("@center 있지, {Player}, 그거 알아?");
            yield return vn.Line("@center 노루는 고라니와 달리 엉덩이털이 하얗데...");
            yield return vn.Line("@dialogue on");


            // 대사
            yield return vn.Line("@bg BG/밤의골목길");
            yield return vn.Say("{Player}", "시X, 꿈....");
            yield return vn.Say("{Player}", "언제부터였을까?\n나는");

            yield return vn.Choice(
                ("카페에 남는다", "cafe"),
                ("공원으로 간다", "park")
            );
            var c = vn.GetChoice();

            if (c == "cafe")
            {
                // 효과음
                yield return vn.Line("@sfx SFX/confirm 1.0 1.0");

                // 중앙 포트레이트만 강조(좌/우 숨김 후 중앙 표시)
                yield return vn.Line("@ch hide all");
                yield return vn.Line("@ch show center Chara/ye_wang_neutral");

                yield return vn.Say("린", "그럼 잠깐 더 쉬었다 가자.");
                yield return vn.Say("카이", "좋아. 음악을 조금 더 키울까?");
                yield return vn.Line("@bgm play Audio/BGM/1.harusora 0.9 0.5 true");

                // 대사창 OFF로 자막만 보여주기
                yield return vn.Line("@dialogue off");
                yield return vn.Line("@center — 잠시 후 —");
                yield return vn.Line("@dialogue on");

                yield return vn.Say("린", "충분히 쉬었어. 이제 출발해볼까?");
            }
            else
            {
                // 배경/음악 전환
                yield return vn.Line("@bg BG/bg_park_noon");
                yield return vn.Line("@bgm play Audio/BGM/park_theme 0.7 0.6 true");

                // 포트레이트 교체
                yield return vn.Line("@ch hide all");
                yield return vn.Line("@ch show left Chara/kai_smile");

                yield return vn.Say("카이", "공기는 상쾌하고, 바람도 좋네.");
                yield return vn.Say("린", "그러게. 기분이 좋아졌어!");
                yield return vn.Line("@sfx Audio/SFX/click 1.0 1.1");

                // 자막용 중앙 텍스트만
                yield return vn.Line("@dialogue off");
                yield return vn.Line("@center — 공원 산책을 즐겼다 —");
                yield return vn.Line("@dialogue on");
            }

            // 마무리: 포트레이트/대사창/브금 제어
            yield return vn.Line("@ch hide all");
            yield return vn.Line("@bgm stop 0.8");
            yield return vn.Line("@center — 데모 종료 —");
            yield return vn.Line("@dialogue off"); // 필요시 UI 정리


        }
    }
}
