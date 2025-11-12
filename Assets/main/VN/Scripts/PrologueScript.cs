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
            yield return vn.Line("@bg fade BG/경고 1.0");
            yield return vn.Say("", "");
            yield return vn.Line("@bg BG/검은화면");

            // BGM 시작

            // 이름 입력

            

            yield return vn.Center("당신의 이름은?");
            yield return vn.Line("@askname Player 당신의 이름을 입력하세요");

            yield return vn.Center("있지… {Player}, 그거 알아?");
            yield return vn.Center("노루는 고라니와 달리 엉덩이털이 하얗대…");
            yield return vn.Center("이건…");
            yield return vn.Center("그 이야기이야.");
            yield return vn.Line("@dialogue on");

            // 장면 전환: BG 페이드
            yield return vn.Line("@bg fade BG/밤의골목길 1.0");
            yield return vn.Line("@bgm play BGM/귀뚜라미 0.7 0.6 true");

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
            yield return vn.Line("@bgm stop BGM/귀뚜라미 0.7 0.6 true");
            yield return vn.Line("@sfx SFX/고라니울음소리01 100.0 1.0");
            yield return vn.Line("@ch show center Chara/달려오는고라니 1.3 0.0");
            // 위치 보정은 별도 명령
            yield return vn.Line("@ch offset center 10 -50");
            // 흔들기
            yield return vn.Line("@ch shake center 0.5 80");

            yield return vn.Say("미친 고라니", "끼에에엑!!!!!!!");
            yield return vn.Line("@sfx SFX/퍽 100.0 1.0");
            yield return vn.Line("@ch hide center");
            yield return vn.Line("@dialogue off");
            yield return vn.Line("@bg BG/검은화면");
            yield return vn.Center("가을이었다.");
            yield return vn.Say("", "");
            yield return vn.Say("", "...");
            yield return vn.Say("???", "어...네...가?");
            yield return vn.Say("", "...");

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
                yield return vn.Line("@sfx SFX/퍽2 100.0 1.0");
                yield return vn.Center("다시 한 번 말하지만, 가을이었다.");


                yield return vn.Say("{Player}", "...");
                yield return vn.Say("{Player}", "난 죽은건가?");
                yield return vn.Say("{Player}", "여긴 어디지?\n난 분명...");
                yield return vn.Say("{Player}", "...");
                yield return vn.Say("{Player}", "모르겠다.");
                yield return vn.Say("", "천천히 눈을 떠본다..");

                yield return vn.Line("@bgm play BGM/브금01 0.7 0.6 true");
                yield return vn.Line("@bg BG/신창섭");
                yield return vn.Say("신창섭", "용사님, 드디어 눈을 떴습니까?");
                yield return vn.Say("{Player}", "신창섭?");
                yield return vn.Say("{Player}", "당신이 어째서...??");
                yield return vn.Say("신창섭", "뭐긴 뭐야 게임오버지");
                yield return vn.Say("신창섭", "잘가라");
                Application.Quit();
            }
            else // OpenTheEyes
            {

            }
            // 배경/음악 전환
            yield return vn.Line("@bg fade BG/들판 1.0");
            yield return vn.Line("@bgm play BGM/성삼 0.7 0.6 true");
            yield return vn.Say("", "낯선 들판이다.");
            yield return vn.Say("???", "드디어 정신을 차렸구만?.");


            yield return vn.Line("@ch show left Chara/고니 1.0 1.0 false -800 -200");

            yield return vn.Say("???", "자네, 드디어 정신을 차렸구만?");
            yield return vn.Say("???", "그래서, 자네는 원숭이인가? 아니면 고릴라인가?");

            yield return vn.Choice(
                ("코주부 원숭이입니다.", "Monkey"),
                ("미친 고라니다!", "CrazyWaterDeer")
            );
            var Event02 = vn.GetChoice();

            if (Event02 == "Monkey")
            {
                yield return vn.Say("???", "어쩐지...아주 찌그러진 것같이 생겼더군");
                yield return vn.Say("", "뭐지, 고라니가 말을 한다고?");
                yield return vn.Say("", "나의 상식선에서는 있을 수 없는 일이다.");

            }
            else
            {
                yield return vn.Say("???", "초면에 아주 실례많군");
            }

            yield return vn.Say("???", "뭐, 고라니가 말을 하는 게 신기하다는 듯한 표정이군");
            yield return vn.Say("???", "아직 진화가 덜된 원숭이라면 그럴 수 있어");
            yield return vn.Say("고니", "아, 소개가 늦었군\n내이름은 고니, 보다 싶이 고라니일세");
            yield return vn.Say("고니", "그나저나 자네 같은 동물은 처음 보는군\n외래종인가? 아니면 기행종?");
            yield return vn.Say("고니", "뭐, 그건 차근차근 알아가도록 하고 자네는 대체 누구인가?");
            yield return vn.Say("", "...\n이딴게 이세계인가?");
            yield return vn.Say("", "일단은 사실확인이 먼저일테고\n상대도 나에게 적의는 없는듯 하다.");
            yield return vn.Say("", "최대한 대화를 통해 상황을 파악하자");
            yield return vn.Say("{Player}", "{Player}라고 합니다.");
            yield return vn.Say("고니", "별 특이한 이름이 다 있군");
            yield return vn.Say("고니", "흠...");
            yield return vn.Say("고니", "혹시 말일세");
            yield return vn.Say("고니", "오래전부터 당신같은 남자를 기다려왔다면 믿겠나?");
            yield return vn.Say("", "모태솔로 인생 22년차\n생애 처음으로 고백이란걸 받아봤다.");
            yield return vn.Say("", "문제가 있다면 그 대상이 수컷 고라니다.");
            yield return vn.Say("", "왜, 수컷이라고 단언짓냐 생각할 수 있겠지만\n고라니는 수컷만이 송곳니를 가지고 있다.");
            yield return vn.Say("{Player}", "죄송합니다. 저는 여자가 좋습니다.");
            yield return vn.Say("고니", "마침 잘됐군, 이 일이 끝나면 딸을 자네에게 주겠네");
            yield return vn.Say(". . .", "");
            yield return vn.Say("고니", " . . .");
            yield return vn.Say("고니", "싫은가?");
            yield return vn.Say("", "내가 인간의 자식이며 털박이가 아닌 이상 무조건 싫다.");
            yield return vn.Say("", "애초에 저건 털박이가 아니라 리얼 짐승이지 않나?");
            yield return vn.Say("", "아마, 털박이를 불러봐도 ");
            yield return vn.Say("", "'저건좀...'이라는 말이 나오지 않을까 싶다.\n만약 가능이라고 하는 사람이 있다면");
            yield return vn.Say("", "진심으로 정신병원에 갈것을 권유하고 싶다.");
            yield return vn.Say("고니", "왜그런가?");
            yield return vn.Say("", "여기서 말 실수라도 했다간 딸을 모욕했다며 앞발로 짓눌릴지도 모른다.");
            yield return vn.Say("", "보통의 사람이라면 뒷발차기를 할거라 생각하지만\n수컷고라니는 영역다툼을 할 때 앞발로 상대를 공격한다.");
            yield return vn.Say("", "나는 저 우람한 발굽에 찍히고 싶지 않다.");
            yield return vn.Say("", "그렇기에 최대한 예의바르고 정당한 이유를 생각해야한다.");
            yield return vn.Say(". . .", "");
            yield return vn.Say("", "떠올랐다.\n이상황을 완벽하게 회피할 수 있는 대답!");
            yield return vn.Say("{Player}", "저는 게이입니다.");
            yield return vn.Say(". . .", "");
            yield return vn.Say("고니", "그럼 어쩔수 없지 기회가되면 좋은 수컷 하나 소개해주겠네");
            yield return vn.Say("", "과정이 이상하지만 결과는 좋은듯하다.");
            yield return vn.Say("고니", "자네, 만약 갈곳이 없다면 따라오게 먹여주고 재워주겠네");
            yield return vn.Say("고니", "물론, 공짜는 아니지만, 조건이 마음에 안들면 떠나도 좋네");
            yield return vn.Say("", "이 사람...아니, 이 고라니는 다른 목적이 있는 듯하다.");
            yield return vn.Say("", "그래도 아무 이유없이 돕겠다는 사람보단 이런 사람, 아니 고라니가 나을지 모른다.");
            yield return vn.Say("고니", "표정을 보아하니 긍정하는걸로 알겠네\n따라오게");

            yield return vn.Line("@bg fade BG/성삼 1.0");
            yield return vn.Say("{Player}", "여긴...");
            yield return vn.Say("", "역시 이세계인가, 이렇게 성스러운 느낌이 드는 숲은 처음이다.");
            yield return vn.Say("", "정말...\n아름답다.");
            yield return vn.Say("고니", "정말 아름다운 곳이지 않나?");
            yield return vn.Say("고니", "새들은 지저귀고 꽃들은 피어나고...");
            yield return vn.Say("고니", "이런 날에 자네를 만난 게 운명이 아닌가 싶네");
            yield return vn.Say("{Player}", "죄송한데, 어디로 가는건가요?");
            yield return vn.Say("", "이 사...아니 고라니는 나를 왜 이런 곳에 대려온걸까?");
            yield return vn.Say("고니", "우리 고라니 일족에겐 예로부터 전해져오는 보물이 있네");
            yield return vn.Say("고니", "자네가 한 번 봐줬음 하네");
            yield return vn.Say("", "내가... 봐야한다고?");
            yield return vn.Say("", "고니는 나의 표정을 살핀다.");
            yield return vn.Say("고니", "그래, 자네같은 영장류의 생물이 필요한 일이야");
            yield return vn.Say("고니", "알지모르겠지만 옛날부터 자네같은 영장류는 멸종직전일세");
            yield return vn.Say("", "영장류의 멸종?");
            yield return vn.Say("고니", "뭐, 육식동물들에게 자주 사냥당한 모양이야.");
            yield return vn.Say("고니", "지금은 아무도 모를 곳에 숨어있을지도 모르지");
            yield return vn.Say("고니", "하지만, 걱정말게 내가 책임지고 지켜주겠네");
            yield return vn.Say("", "대체 어떤 곳인지 감도 안잡힌다.");
            yield return vn.Line("@sfx SFX/고라니울음소리01 100.0 1.0");
            yield return vn.Say("!??!!?", "");
            yield return vn.Line("@sfx SFX/차충돌 100.0 1.0");
            yield return vn.Say("고니", "또, 시작인가?");
            yield return vn.Say("{Player}", "대체 무슨일이 벌어지고 있는건가요?");
            yield return vn.Say("", "엄청나가 큰 비명과 큰 충돌음이 들려왔다.");
            yield return vn.Say("고니", "가보면 알걸세");
            yield return vn.Say("", "고니는 발걸음을 서둘렀다.");
            yield return vn.Say("", "나를 배려하는 것인지 사족보행이 아닌 이족보행이었지만\n덕분에 따라갈만 했다.");
            yield return vn.Line("@sfx SFX/고라니울음소리01 100.0 1.0");
            yield return vn.Say("", "다시 한 번 비명소리가 들려왔다.");
            yield return vn.Line("@sfx SFX/차충돌 100.0 1.0");
            yield return vn.Say("", "그리고 이어지는 충돌음...");
            yield return vn.Say("", "확실히 소리는 좀 더 가까워졌다.");
            yield return vn.Say("", "대체 무슨일이 벌어지고 있는거지?");

            
            yield return vn.Say("", "하지만, 얼마안가 숲 안쪽에서 작은 인영...아니 고라니의 그림자가 보였다.");
            yield return vn.Say("고니", "라니야, 또 거기에 갔다온거냐?");


            yield return vn.Line("@ch show right Chara/라니 1.0 1.0 false 800 -200");
            yield return vn.Line("@ch offset right 800 -200");
            yield return vn.Say("라니", "아빠?");
            yield return vn.Say("", "아빠...?");
            yield return vn.Say("라니", "....네, 거기 갔다오는 길이에요.");
            yield return vn.Say("라니", ". . . ?");
            yield return vn.Say("", "라니라는 고라니는 나를 빤히 쳐다본다.");
            yield return vn.Say("라니", "원숭이?\n아빠, 진짜에요?");
            yield return vn.Say("", "믿기지 않는다는 표정으로 나를 주시한다.");
            yield return vn.Say("고니", "그래, 이 분이 우리들의 희망이다.");
            yield return vn.Say("", "라니의 맑은 눈망울이 떨려온다.");
            yield return vn.Say("", "감격? 충격? 사람의 눈동자가 아니라 이해할 수 없지만");
            yield return vn.Say("", "부디 사랑은 아니길 빈다.");
            yield return vn.Say("라니", "어서 가요! 원숭이씨!");
            yield return vn.Say("{Player}", "우왓!?");
            yield return vn.Say("", "라니는 나의 옷깃을 물고 달려가기 시작했다.");

            yield return vn.Line("@bg fade BG/검은화면 1.0");
            yield return vn.Say("", "나는 힘겹게 균형을 잡고 사족보행으로 달리는 \n라니의 속도를 따라잡기위해 전력으로 달렸다.");
            yield return vn.Say("", "나를 물고 달려가는 라니와 고니의 눈빛은 똘망했다.");
            yield return vn.Say("", "마치...무너진 세상속에서 한 줄기의 희망을 찾은 구도자와 같았다.");
            yield return vn.Say("", "도착한 곳은 숲속의 중심부이자 가장 신성한 곳인마냥 넓은 공터가 보였다.");
            yield return vn.Say("", "그리고...");
            yield return vn.Say("고니", "어떤가? 이게 우리 고라니들의 보물일세");
            yield return vn.Line("@ch hide left 0.0");
            yield return vn.Line("@ch hide right 0.0");
            yield return vn.Line("@bg fade BG/트럭 1.0");
            yield return vn.Say("", "전생트럭이 아니라 전생한 트럭이 눈앞에 있었다.");
            yield return vn.Say("고니", "어떤가? 보기만해도 들이 박고 싶어지지 않나?");
            yield return vn.Say("라니", "마치 성유물 같아...");
            yield return vn.Say("고니", "문헌에 따르면 이것은 스스로 움직일 수 있다고 하더군");
            yield return vn.Say("고니", "자네가, 이걸 고쳐줬으면 하네!");
            yield return vn.Line("@bg fade BG/검은화면 1.0");
            yield return vn.Line("@dialogue off");

            yield return vn.Center("데모버전은 여기까지입니다.");
            yield return vn.Center("정식버전이 나올지는 미지수입니다.");
            yield return vn.Center("많은 무관심 부탁드립니다.");
            Application.Quit();

        }
    }
}
