/*
 # VN Tool Manual (.cs 기반)

## 0) 폴더 구조 (권장)
Assets/
  main/
    VN/
      Scripts/
        VNEngine.cs         # 코어(씬에 1개)
        VNCommands.cs       # 명령 모델
        VNScript.cs         # 시나리오 베이스 클래스
        PrologueScript.cs   # 예시 시나리오(자유롭게 추가)
      Resources/
        BG/                 # 배경 스프라이트 (예: room_day.png)
        Chara/
          Rin/              # 캐릭터명
            smile.png       # 포즈/표정명
          Kai/
            neutral.png
      Prefabs/
        ChoiceButton.prefab # 선택지 버튼(프리팹, 파란 큐브)

※ `Resources/` 아래의 경로/파일명은 코드에서 확장자 없이 불러옵니다.
## 1) 씬 구성 체크리스트
- Canvas
  - BG (Image)                → Source Image: None, Color: 검정
  - CharaLayer (RectTransform)→ Anchor: Stretch Full, Image 없음(컴포넌트 제거)
  - DialoguePanel (RectTransform)
    - NameText (TextMeshProUGUI)
    - BodyText (TextMeshProUGUI)
  - ChoicesPanel (RectTransform) → 기본 Inactive
- VNEngine (GameObject) → VNEngine.cs 부착
- Script_Prologue (GameObject) → PrologueScript.cs 부착 (또는 원하는 시나리오)
- EventSystem (자동)

### Canvas 컴포넌트 연결 (VNEngine)
- Bg Image            → Canvas/BG(Image)
- Chara Layer         → Canvas/CharaLayer(Transform)
- Name Text           → Canvas/DialoguePanel/NameText (TMP_Text)
- Body Text           → Canvas/DialoguePanel/BodyText (TMP_Text)
- Choices Panel       → Canvas/ChoicesPanel (Transform)
- Choice Button Prefab→ Project의 `Prefabs/ChoiceButton.prefab` (파란 큐브)
- Typewriter Speed    → 0.02 ~ 0.05
- Script Behaviour    → 씬의 Script_Prologue(또는 원하는 시나리오)
- Auto Start          → 체크

## 2) 폰트(한글) 설정
- TextMeshPro Font Asset(SDF) 생성 후 NameText/BodyText/ChoiceButton의 TMP_Text에 지정
  (Dynamic 권장, Project Settings > TextMeshPro > Default Font로 등록하면 앞으로 자동)

// 3) 시나리오 작성 가이드 (새 챕터 템플릿)
// 파일명 예: Assets/main/VN/Scripts/Chapter1Script.cs
using VN;

public class Chapter1Script : VNScript
{
    protected override void Define()
    {
        // 시작 노드 지정 (없으면 기본 "start")
        StartNode = "start";

        Node("start")
            .Bg("BG/room_day")                     // Resources/BG/room_day.png
            .Say("A", "좋은 아침.")
            .Show("Kai", "neutral", "center")      // Resources/Chara/Kai/neutral.png
            .Say("Kai", "오늘 일정은?")
            .Choice(
                ("커피부터.", "coffee"),
                ("바로 회의실로.", "meeting")
            );

        Node("coffee")
            .Say("Rin", "나도 한 잔.",         speaker:"Rin")
            .Show("Rin", "smile", "right")     // Resources/Chara/Rin/smile.png
            .Goto("end");

        Node("meeting")
            .Hide("right")                     // 슬롯/캐릭터 위치: left/center/right
            .Say("Kai", "오케이, 회의실로.")
            .Goto("end");

        Node("end").End();
    }
}

// 4) 명령 레퍼런스 (VNCommands.cs 요약, 사용법 기준)
// 시나리오에서 체이닝으로 사용됨

// Node(string id)                : 노드 선언
//   .Next(string id)             : 다음 노드 기본 지정 (없으면 마지막에 자동 호출)
//   .Goto(string id)             : 즉시 다른 노드로 분기
//   .End()                       : 스토리 종료

// .Say(string speaker, string text, string speakerOverride=null)
//   - 말풍선 출력. speaker가 null이면 이름칸 비움.
//   - 클릭(좌클릭/Space)으로 다음으로 진행

// .Bg(string path)
//   - 배경 교체. Resources.Load<Sprite>(path)

// .Show(string charName, string pose, string pos)
//   - 캐릭터 표시(좌/중/우 포지션): pos = "left" | "center" | "right"
//   - 스프라이트 경로: Resources/Chara/{charName}/{pose}.png

// .Hide(string posOrChar)
//   - 해당 슬롯/식별자 비활성화

// .Var(name, op, value)
//   - 변수 연산: op = "=", "+", "-"
//   - 분기 조건은 시나리오에서 변수 읽어 커스텀 제어(필요 시 If/When 패턴 추가 가능)

// .Choice((string text, string gotoId), ...)
//   - 선택지 생성→ 클릭 시 해당 gotoId로 이동

## 5) 입력/조작
- 다음 대사로 진행: 마우스 왼쪽 클릭 또는 Space
- 선택지: 동적으로 버튼 생성(오른쪽 패널)
- 텍스트 출력: 타자기 효과(typewriterSpeed로 속도 조절)

(선택) 스킵/로그/오토 등 확장은 VNEngine에 핸들러를 추가해서 구현

## 6) 리소스 규칙
- 배경: `Resources/BG/<name>.png` → `.Bg("BG/<name>")`
- 캐릭터: `Resources/Chara/<Char>/<Pose>.png` → `.Show("<Char>","<Pose>","left|center|right")`
- 파일명/대소문자 정확히 일치 (특히 빌드 타겟이 대소문자 구분 OS일 경우)

## 7) 선택지 버튼 프리팹 규격
- RectTransform: 600×64(권장)
- 자식 TMP_Text 1개(라벨)
- Button + Image(배경) 구성
- Layout Element: PreferredWidth=600, PreferredHeight=64
- 프리팹은 Project의 `Prefabs/ChoiceButton.prefab`에 보관하고,
  VNEngine의 Choice Button Prefab 슬롯에 반드시 "파란 큐브" 프리팹을 연결

## 8) 자주 나오는 경고/오류 해결
- ArgumentException: Instantiate is null
  → VNEngine의 Choice Button Prefab 슬롯에 "씬 오브젝트"가 아니라
     Project의 프리팹(파란 큐브)을 연결했는지 확인. 씬의 ChoiceButton은 삭제.

- [VN] Sprite not found: Chara/Xxx/pose
  → Resources 폴더 아래 경로/이름이 정확한지 확인(확장자 제외, 대소문자 일치).
  → 예: Assets/main/VN/Resources/Chara/Rin/smile.png

- 흰 네모가 중앙에 보임
  → CharaLayer에 Image 컴포넌트가 붙어있지 않은지 확인(제거).
  → 슬롯은 코드에서 생성되며 sprite가 없으면 enabled=false/비활성 처리됨.

- 폰트가 □로 보임
  → TMP SDF 폰트 생성 후 Name/Body/ChoiceButton 라벨에 적용(Dynamic 권장).

// 9) 새 시나리오 추가 절차 (3줄 요약)
// (1) Scripts 폴더에 새 .cs 추가 — VNScript 상속해 Node들 정의
// (2) 씬에 빈 GameObject 생성 → 새 스크립트 부착
// (3) VNEngine의 Script Behaviour 슬롯에 그 오브젝트를 드래그

## 10) 확장 아이디어
- 사운드: Resources/BGM, Resources/SFX 경로를 추가하고 Bg와 동일 패턴으로 Load
- 페이드/트윈: DoBg/DoShow에서 CanvasGroup/Graphic.CrossFadeAlpha 응용
- 로그/스킵: VNEngine에 큐·스택 저장 후 UI로 노출
- 세이브/로드: 노드ID, 변수 Dictionary 저장(JSON/PlayerPrefs/파일)
 
 
 
 */