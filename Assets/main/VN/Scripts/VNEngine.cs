using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    [DisallowMultipleComponent]
    public partial class VNEngine : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image bgImage;
        [SerializeField] private RectTransform charaLayer;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private RectTransform choicesPanel;
        [SerializeField] private Button choiceButtonPrefab;
        [SerializeField, Range(0f, 0.2f)] private float typewriterSpeed = 0.02f;

        [Header("Dialogue UI Toggle")]
        [SerializeField] private RectTransform dialoguePanel; // (배경+이름+본문)
        [SerializeField] private TMP_Text centerText;         // 중앙 텍스트

        [Header("Script")]
        [SerializeField] private VNScript scriptBehaviour;
        [SerializeField] private bool autoStart = true;

        // 내부 상태
        private bool clickRequested;
        private string lastChoiceId;

        void Awake()
        {
            if (!scriptBehaviour)
                Debug.LogError("scriptBehaviour가 비어있습니다.");

            if (centerText) centerText.gameObject.SetActive(false);
            if (choicesPanel) choicesPanel.gameObject.SetActive(false);
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);
        }

        void Start()
        {
            if (autoStart && scriptBehaviour)
                StartCoroutine(Run());
        }

        public IEnumerator Run()
        {
            yield return scriptBehaviour.Define(this);
        }

        // ===== 공용 API =====

        public IEnumerator Say(string speaker, string message)
        {
            if (!string.IsNullOrEmpty(message) && message[0] == '@')
            {
                if (TryProcessCommandLine(message, out var routine))
                {
                    if (routine != null) yield return routine;
                    yield break;
                }
            }

            if (centerText) centerText.gameObject.SetActive(false);
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);

            if (nameText) nameText.text = speaker ?? string.Empty;
            if (bodyText)
            {
                yield return Typewriter(bodyText, message ?? string.Empty);
                yield return WaitForClick();
            }
        }

        public IEnumerator Line(string message)
        {
            if (!string.IsNullOrEmpty(message) && message[0] == '@')
            {
                if (TryProcessCommandLine(message, out var routine))
                {
                    if (routine != null) yield return routine;
                    yield break;
                }
            }

            if (centerText) centerText.gameObject.SetActive(false);
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);

            if (nameText) nameText.text = string.Empty;
            if (bodyText)
            {
                yield return Typewriter(bodyText, message ?? string.Empty);
                yield return WaitForClick();
            }
        }

        public IEnumerator Center(string message)
        {
            if (!string.IsNullOrEmpty(message) && message[0] == '@')
            {
                if (TryProcessCommandLine(message, out var routine))
                {
                    if (routine != null) yield return routine;
                    yield break;
                }
            }

            if (dialoguePanel) dialoguePanel.gameObject.SetActive(false);
            if (centerText)
            {
                centerText.gameObject.SetActive(true);
                centerText.text = message ?? string.Empty;
            }
            yield return WaitForClick();
            if (centerText) centerText.gameObject.SetActive(false);
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);
        }

        public IEnumerator Choice(params (string text, string id)[] options)
        {
            lastChoiceId = null;

            if (choicesPanel)
            {
                choicesPanel.gameObject.SetActive(true);
                for (int i = choicesPanel.childCount - 1; i >= 0; i--)
                    Destroy(choicesPanel.GetChild(i).gameObject);

                foreach (var op in options)
                {
                    var btn = Instantiate(choiceButtonPrefab, choicesPanel);
                    var label = btn.GetComponentInChildren<TMP_Text>();
                    if (label) label.text = op.text ?? "…";

                    string captured = op.id;
                    btn.onClick.AddListener(() =>
                    {
                        lastChoiceId = captured;
                        choicesPanel.gameObject.SetActive(false);
                    });
                }
            }

            while (lastChoiceId == null) yield return null;
        }

        public string GetChoice() => lastChoiceId;

        // ===== 내부 유틸 =====

        IEnumerator Typewriter(TMP_Text target, string text)
        {
            target.text = string.Empty;
            if (typewriterSpeed <= 0f)
            {
                target.text = text;
                yield break;
            }

            for (int i = 0; i <= text.Length; i++)
            {
                target.text = text.Substring(0, i);
                yield return new WaitForSecondsRealtime(typewriterSpeed);
            }
        }

        public IEnumerator WaitForClick()
        {
            clickRequested = false;
            while (!clickRequested) yield return null;
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                clickRequested = true;
        }

        // ===== 커맨드 파서 =====
        // 반환: 처리했으면 true (routine이 있을 수도/없을 수도), 모르면 false
        // 지원: 
        // @bgm play <path> [volume] [fadeSec] [loop]
        // @bgm stop [fadeSec]
        // @sfx <path> [volume] [pitch]
        // @wait <seconds>
        // @center <text...>
        // @dialogue
        // @bg <spritePath>
        bool TryProcessCommandLine(string line, out IEnumerator routine)
        {
            routine = null;

            if (string.IsNullOrWhiteSpace(line) || line[0] != '@')
                return false;

            var parts = line.Substring(1).Trim().Split(' ');
            if (parts.Length == 0) return false;

            string cmd = parts[0].ToLowerInvariant();

            switch (cmd)
            {
                case "bgm":
                    {
                        if (parts.Length >= 2)
                        {
                            string sub = parts[1].ToLowerInvariant();
                            if (sub == "play")
                            {
                                // @bgm play <path> [volume] [fadeSec] [loop]
                                string path = (parts.Length >= 3) ? JoinRest(parts, 2) : null;
                                float volume = -1f, fade = 0.5f;
                                bool loop = true;
                                ParseTail(parts, ref loop, ref fade, ref volume);

                                if (!string.IsNullOrEmpty(path) && VNAudio.Instance)
                                    VNAudio.Instance.PlayBgm(path, volume, fade, loop);

                                return true;
                            }
                            else if (sub == "stop")
                            {
                                float fade = 0.5f;
                                if (parts.Length >= 3 && TryFloat(parts[2], out var f)) fade = f;
                                if (VNAudio.Instance) VNAudio.Instance.StopBgm(fade);
                                return true;
                            }
                        }
                        break;
                    }

                case "sfx":
                    {
                        // @sfx <path> [volume] [pitch]
                        if (parts.Length >= 2)
                        {
                            string path = JoinRest(parts, 1);
                            float volume = -1f;
                            float pitch = 1f;
                            ParseTail(parts, ref pitch, ref volume);
                            if (VNAudio.Instance && !string.IsNullOrEmpty(path))
                                VNAudio.Instance.PlaySfx(path, volume, pitch);
                            return true;
                        }
                        break;
                    }

                case "wait":
                    {
                        // @wait <seconds>
                        if (parts.Length >= 2 && TryFloat(parts[1], out var sec))
                            routine = WaitSeconds(sec);
                        else
                            routine = null;
                        return true;
                    }

                case "center":
                    {
                        // @center <text...>
                        string text = line.Substring("@center".Length + 1).Trim();
                        routine = CenterRoutine(text);
                        return true;
                    }

                case "dialogue":
                    {
                        // @dialogue (즉시 전환)
                        if (centerText) centerText.gameObject.SetActive(false);
                        if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);
                        return true;
                    }

                case "bg":
                    {
                        // @bg <spritePath>
                        if (parts.Length >= 2)
                        {
                            string path = JoinRest(parts, 1);
                            var sprite = Resources.Load<Sprite>(path);
                            if (!sprite)
                                Debug.LogWarning($"[VN] BG sprite not found: {path}");
                            else if (bgImage) bgImage.sprite = sprite;
                        }
                        return true;
                    }
            }

            Debug.LogWarning($"[VN] Unknown command: {line}");
            return true; // 모르는 커맨드라도 ‘처리됨’으로 보고 본문 출력 막음
        }

        // ----- 파서 유틸 -----
        static bool TryFloat(string s, out float v)
            => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

        static string JoinRest(string[] parts, int startIndex)
        {
            if (startIndex >= parts.Length) return string.Empty;
            return string.Join(" ", parts, startIndex, parts.Length - startIndex).Trim();
        }

        // Overload 1: loop(bool), fade(float), volume(float)
        static void ParseTail(string[] parts, ref bool loop, ref float fade, ref float volume)
        {
            for (int i = parts.Length - 1; i >= 0; --i)
            {
                var t = parts[i].ToLowerInvariant();
                if (t == "true" || t == "false")
                {
                    if (bool.TryParse(t, out var b)) { loop = b; return; }
                }
                else if (TryFloat(t, out var f))
                {
                    if (fade < 0f || Mathf.Approximately(fade, 0.5f)) { fade = f; }
                    else { volume = f; }
                }
            }
        }

        // Overload 2: pitch(float), volume(float)
        static void ParseTail(string[] parts, ref float pitch, ref float volume)
        {
            int found = 0;
            for (int i = parts.Length - 1; i >= 0; --i)
            {
                if (TryFloat(parts[i], out var f))
                {
                    if (found == 0) { pitch = f; found++; }
                    else if (found == 1) { volume = f; break; }
                }
            }
        }

        // ----- 보조 코루틴 -----
        static IEnumerator WaitSeconds(float sec)
        {
            yield return new WaitForSecondsRealtime(sec);
        }

        IEnumerator CenterRoutine(string text)
        {
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(false);
            if (centerText)
            {
                centerText.gameObject.SetActive(true);
                centerText.text = text ?? string.Empty;
            }
            yield return WaitForClick();
            if (centerText) centerText.gameObject.SetActive(false);
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);
        }
    }
}
