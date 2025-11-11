using System.Collections;
using UnityEngine;

namespace VN
{
    /// <summary>
    /// 기능 쇼케이스: BG, BGM/SFX, 포트레이트 좌/중앙/우 표시·숨김, 선택지, 대사창 on/off, 이름입력/토큰치환
    /// 필요 리소스:
    /// BG  : Resources/BG/BG1, Resources/BG/검은화면, Resources/BG/밤의골목길
    /// CH  : Resources/Chara/달려오는고라니 (PNG, Sprite)
    /// BGM : Resources/Audio/BGM/1.harusora
    /// SFX : Resources/Audio/SFX/click, Resources/Audio/SFX/confirm
    /// </summary>
    /// /// <summary>
    /// ─────────────────────────────────────────────────────────────────────────────
    /// ✅ VNEngine 출력·연출 명령 매뉴얼 (현 버전 지원 범위)
    /// ─────────────────────────────────────────────────────────────────────────────
    /// ■ 리소스 위치(중요)
    ///   - 모든 경로는 Resources 기준, 확장자 없이 작성
    ///   - 예) Assets/main/VN/Resources/Audio/BGM/1.harusora.wav  →  "Audio/BGM/1.harusora"
    ///   - 스프라이트는 Texture Type = Sprite(2D and UI) 여야 함
    ///
    /// ■ 토큰 치환
    ///   - 대사 및 중앙 자막 내에서 {Key} → AskName 등으로 저장된 값으로 치환
    ///   - 예) "{Player}, 준비됐어?" → "예왕, 준비됐어?"
    ///
    /// ■ 대사/자막 API (C# 호출)
    ///   - yield return vn.Say("이름", "본문 텍스트\n줄바꿈 가능");
    ///   - yield return vn.Line("본문만 출력");
    ///   - yield return vn.Center("화면 중앙 자막(대화창 OFF/자동 복귀)");
    ///
    /// ■ 대화창 토글
    ///   - @dialogue on    : 대화창 보이기(중앙 자막 끄기)
    ///   - @dialogue off   : 대화창 숨기기(중앙 자막 켜기)
    ///
    /// ■ 배경(BG)
    ///   - @bg BG/경로                     : 즉시 교체
    ///   - @bg fade BG/경로 시간초         : 페이드(검은막 → 교체 → 페이드인)
    ///     예) @bg BG/검은화면
    ///         @bg fade BG/밤의골목길 1.2
    ///
    /// ■ 배경 페이드(화면 전체 블랙 오버레이)
    ///   - @fade out 시간초   : 화면을 점점 어둡게(블랙 100%)
    ///   - @fade in  시간초   : 화면을 점점 밝게(블랙 0%)
    ///
    /// ■ BGM
    ///   - @bgm play Audio/BGM/경로 볼륨 페이드초 루프(bool)
    ///     예) @bgm play Audio/BGM/1.harusora 0.7 0.6 true
    ///   - @bgm stop 페이드초
    ///     예) @bgm stop 0.8
    ///
    /// ■ SFX
    ///   - @sfx Audio/SFX/경로 볼륨 피치
    ///     예) @sfx Audio/SFX/click 1.0 1.0
    ///
    /// ■ 캐릭터(포트레이트) 표시/연출  [left | center | right]
    ///   - @ch show 위치 Chara/경로 [scale] [fadeSec] [preserve] [ox] [oy]
    ///     · scale: 배율(기본 1), fadeSec: 등장 페이드 시간(기본 0)
    ///     · preserve: 이미지 비율 보존(Image.preserveAspect, 기본 false)
    ///     · ox, oy: 위치 오프셋(기본 0, 0)
    ///     예) @ch show center Chara/달려오는고라니 2.0 0.2 true 10 -50
    ///
    ///   - @ch hide 위치|all [fadeSec]
    ///     예) @ch hide right 0.25
    ///         @ch hide all 0.2
    ///
    ///   - @ch size 위치 scale [duration]
    ///     예) @ch size center 1.2 0.2     // 0.2초 동안 1.2배로
    ///
    ///   - @ch offset 위치 x y
    ///     예) @ch offset left -40 10      // 좌측으로 40, 위로 10
    ///
    ///   - @ch shake 위치 duration amplitude
    ///     예) @ch shake center 0.35 20    // 0.35초 동안 진폭 20 흔들기
    ///
    /// ■ 이름 입력(AskName)
    ///   - @askname Key 안내문구
    ///     · 입력 패널이 표시되고, 확인 시 vars[Key]에 값 저장
    ///     · 입력 대기 동안 클릭 스킵 차단(enter/버튼으로만 확정)
    ///     예) @askname Player 당신의 이름을 입력하세요
    ///         …
    ///         yield return vn.Say("{Player}", "여기서 플레이어 이름이 치환돼요");
    ///
    /// ■ 선택지(Choices)
    ///   - C#에서 호출:
    ///       yield return vn.Choice(("문장1","id1"), ("문장2","id2"));
    ///       var pick = vn.GetChoice();  // "id1" 또는 "id2"
    ///
    /// ■ 타자효과(타이핑)
    ///   - Inspector의 typewriterSpeed(초/글자)로 조절(0이면 즉시 출력)
    ///
    /// ─────────────────────────────────────────────────────────────────────────────
    /// 사용 예시 (스크립트 내):
    ///   yield return vn.Line("@bg BG/검은화면");
    ///   yield return vn.Line("@bgm play Audio/BGM/1.harusora 0.7 0.6 true");
    ///   yield return vn.Center("당신의 이름은?");
    ///   yield return vn.Line("@askname Player 이름을 입력하세요");
    ///   yield return vn.Line("@dialogue on");
    ///   yield return vn.Line("@bg fade BG/밤의골목길 1.2");
    ///   yield return vn.Say("{Player}", "출발하자!");
    ///   yield return vn.Line("@ch show center Chara/달려오는고라니 2.0 0.2 true 10 -50");
    ///   yield return vn.Line("@ch shake center 0.2 80");
    ///   yield return vn.Line("@sfx Audio/SFX/confirm 1.0 1.0");
    ///   yield return vn.Line("@fade out 0.5");
    ///   yield return vn.Line("@bg BG/BG1");
    ///   yield return vn.Line("@fade in 0.5");
    /// ─────────────────────────────────────────────────────────────────────────────
    /// </summary>
    public class PrologueScript : VNScript
    {
        public override IEnumerator Define(VNEngine vn)
        {
            // BG 즉시 전환
            yield return vn.Line("@bg BG/검은화면");

            // BGM 시작
            yield return vn.Line("@bgm play BGM/1.harusora 0.7 0.6 true");

            // 이름 입력
            yield return vn.Center("당신의 이름은?");
            yield return vn.Line("@askname Player 당신의 이름을 입력하세요");

            yield return vn.Center("있지… {Player}, 그거 알아?");
            yield return vn.Center("노루는 고라니와 달리 엉덩이털이 하얗대…");
            yield return vn.Center("이건…");
            yield return vn.Center("그 이야기이다.");
            yield return vn.Line("@dialogue on");

            // 장면 전환: BG 페이드
            yield return vn.Line("@bg fade BG/밤의골목길 1.0");

            // 대사
            yield return vn.Say("", "나는 가끔 생각한다.");
            yield return vn.Say("", "밥, 똥, 야근만 하는 인생을 살 바엔\n라이트노벨마냥 전생 트럭에 치이면 어떨까?");
            yield return vn.Say("{Player}", "하… 정말이지 이세계 전생이란 걸 할 수 있다면 좋겠다.");
            yield return vn.Say("", "물론, 이 세상에 그딴 게 존재할 리 없다.");
            yield return vn.Say("", "하지만…");
            yield return vn.Say("{Player}", "나도 이세계로 가서 미소녀와 해피 라이프를 살아보고 싶다…");
            yield return vn.Say("", "많잖아?\n게임 섭종할 때 로그인 중이면 간다거나,");
            yield return vn.Say("", "길 가다 칼빵 맞았다고 이세계로 가거나,\n심지어 편의점에서 나오는 도중에 가는 경우도 있었다.");
            yield return vn.Say("", "…");
            yield return vn.Say("{Player}", "여긴 현실이다. 정신 차려, {Player}…");
            yield return vn.Say("", "나는 고개를 저으며 현실을 인지한다.");
            yield return vn.Say("{Player}", "그치만…");
            yield return vn.Say("{Player}", "역시, 가고 싶다");
            yield return vn.Say("{Player}", "이세계");

            // 고라니 표시: show(스케일, 페이드시간만)
            yield return vn.Line("@ch show center Chara/달려오는고라니 2.0 0.0");
            // 위치 보정은 별도 명령
            yield return vn.Line("@ch offset center 10 -50");
            // 흔들기
            yield return vn.Line("@ch shake center 0.2 80");

            yield return vn.Say("미친 고라니", "끼에에엑!!!!!!!");
            yield return vn.Line("@ch hide center");
            yield return vn.Line("@dialogue off");
            yield return vn.Line("@bg BG/검은화면");
            yield return vn.Center("가을이었다.");
            yield return vn.Say("","");
            yield return vn.Say("","...");
            yield return vn.Say("","어...네...가?");
            yield return vn.Say("" , "...");
            yield return vn.Choice(
                ("눈을 뜬다.", "OpenTheEyes"),
                ("10분만 더 눈을 감는다.", "GameOver01")
                );
            var Event01 = vn.GetChoice();  // "id1" 또는 "id2"

            if (Event01 == "GameOver01")
            {
                yield return vn.Say("???", "소난다...");
                yield return vn.Say("???", "밟을게...");
                yield return vn.Say("{Player}", "??!!");
                //콰직효과음
                yield return vn.Say("콰직!", "");
                yield return vn.Say("우드득", "");
                yield return vn.Center("가을이었다.");


                yield return vn.Say("???", "밟을게...");
                Application.Quit();
            }
            else // OpenTheEyes
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
        }
    }
}
