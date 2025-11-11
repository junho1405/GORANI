using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VN
{
    [DisallowMultipleComponent]
    public class VNEngine : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image bgImage;
        [SerializeField] private RectTransform charaLayer;
        [SerializeField] private Image charLeft;
        [SerializeField] private Image charCenter;
        [SerializeField] private Image charRight;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private RectTransform choicesPanel;
        [SerializeField] private Button choiceButtonPrefab;
        [SerializeField, Range(0f, 0.2f)] private float typewriterSpeed = 0.02f;

        [Header("Dialogue UI Toggle")]
        [SerializeField] private RectTransform dialoguePanel;
        [SerializeField] private TMP_Text centerText;

        [Header("Name Input (optional)")]
        [SerializeField] private RectTransform nameInputPanel;
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button nameConfirmButton;

        [Header("Script")]
        [SerializeField] private VNScript scriptBehaviour;
        [SerializeField] private bool autoStart = true;

        // ===== 변수 치환 =====
        const string PP_PLAYER = "VN.Player";
        readonly Dictionary<string, string> vars = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Player", "플레이어" }
        };
        static readonly Regex VarToken = new(@"\{([A-Za-z0-9_]+)\}", RegexOptions.Compiled);

        // 내부 상태
        private bool clickRequested;
        private string lastChoiceId;

        void Awake()
        {
            if (PlayerPrefs.HasKey(PP_PLAYER))
                vars["Player"] = PlayerPrefs.GetString(PP_PLAYER);

            if (!scriptBehaviour)
                Debug.LogError("scriptBehaviour가 비어있습니다.");

            if (centerText)
            {
                centerText.gameObject.SetActive(false);
                // 중앙 텍스트가 입력을 가로막지 않도록
                centerText.raycastTarget = false;
            }
            if (choicesPanel) choicesPanel.gameObject.SetActive(false);
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);

            if (nameInputPanel) nameInputPanel.gameObject.SetActive(false);

            EnsurePortrait(ref charLeft, "CharLeft");
            EnsurePortrait(ref charCenter, "CharCenter");
            EnsurePortrait(ref charRight, "CharRight");
            ShowPortrait(charLeft, false);
            ShowPortrait(charCenter, false);
            ShowPortrait(charRight, false);
        }

        void Start()
        {
            if (autoStart && scriptBehaviour)
                StartCoroutine(Run());
        }

        public IEnumerator Run() { yield return scriptBehaviour.Define(this); }

        // ===== 변수 API =====
        public void SetPlayerName(string name)
        {
            SetVar("Player", name);
            PlayerPrefs.SetString(PP_PLAYER, name ?? "");
            PlayerPrefs.Save();
        }
        public void SetVar(string key, string value) => vars[key] = value ?? "";
        public string GetVar(string key) => vars.TryGetValue(key, out var v) ? v : "";

        // ===== 대사 API =====
        public IEnumerator Say(string speaker, string message)
        {
            if (!string.IsNullOrEmpty(message) && message[0] == '@')
            {
                if (TryProcessCommandLine(message, out var routine)) { if (routine != null) yield return routine; yield break; }
            }

            if (centerText) centerText.gameObject.SetActive(false);
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);

            if (nameText) nameText.text = ExpandVars(speaker ?? string.Empty);
            if (bodyText)
            {
                yield return Typewriter(bodyText, ExpandVars(message ?? string.Empty));
                yield return WaitForClick();
            }
        }

        public IEnumerator Line(string message)
        {
            if (!string.IsNullOrEmpty(message) && message[0] == '@')
            {
                if (TryProcessCommandLine(message, out var routine)) { if (routine != null) yield return routine; yield break; }
            }

            if (centerText) centerText.gameObject.SetActive(false);
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);

            if (nameText) nameText.text = string.Empty;
            if (bodyText)
            {
                yield return Typewriter(bodyText, ExpandVars(message ?? string.Empty));
                yield return WaitForClick();
            }
        }

        public IEnumerator Center(string message)
        {
            if (!string.IsNullOrEmpty(message) && message[0] == '@')
            {
                if (TryProcessCommandLine(message, out var routine)) { if (routine != null) yield return routine; yield break; }
            }

            if (dialoguePanel) dialoguePanel.gameObject.SetActive(false);
            if (centerText)
            {
                centerText.gameObject.SetActive(true);
                centerText.text = ExpandVars(message ?? string.Empty);
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
                    if (label) label.text = ExpandVars(op.text ?? "…");

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

        // ===== 이름 입력 패널 =====
        public IEnumerator AskName(string key = "Player", string prompt = "이름을 입력하세요")
        {
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(false);

            if (!nameInputPanel || !nameInputField || !nameConfirmButton)
            {
                Debug.LogWarning("[VN] Name Input references are not assigned.");
                SetVar(key, GetVar(key));
                yield break;
            }

            // 패널 띄우기
            nameInputPanel.gameObject.SetActive(true);

            // 프롬프트를 중앙 텍스트로 안내
            if (centerText)
            {
                centerText.gameObject.SetActive(true);
                centerText.text = prompt;
            }

            // 기본값 채우고 자동 포커스
            nameInputField.text = GetVar(key);
            yield return null; // 한 프레임 쉬고
            var es = EventSystem.current;
            if (es != null)
            {
                es.SetSelectedGameObject(null);
                es.SetSelectedGameObject(nameInputField.gameObject);
            }
            nameInputField.caretPosition = nameInputField.text.Length;
            nameInputField.selectionAnchorPosition = 0;
            nameInputField.selectionFocusPosition = nameInputField.text.Length;
            nameInputField.ActivateInputField();   // ← 키보드 입력 즉시 가능

            bool submitted = false;
            void OnSubmit()
            {
                var val = nameInputField.text;
                if (string.IsNullOrWhiteSpace(val)) val = "플레이어";
                SetVar(key, val);
                if (string.Equals(key, "Player", StringComparison.OrdinalIgnoreCase))
                {
                    PlayerPrefs.SetString(PP_PLAYER, val);
                    PlayerPrefs.Save();
                }
                submitted = true;
            }

            nameConfirmButton.onClick.AddListener(OnSubmit);
            nameInputField.onSubmit.AddListener(_ => OnSubmit());   // Enter 처리
            nameInputField.onEndEdit.AddListener(_ =>
            {
                // 모바일/일부 환경에서 onSubmit이 안 올 때 대비
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    OnSubmit();
            });

            while (!submitted) yield return null;

            nameConfirmButton.onClick.RemoveListener(OnSubmit);
            nameInputField.onSubmit.RemoveListener(_ => OnSubmit());
            nameInputField.onEndEdit.RemoveAllListeners();

            nameInputPanel.gameObject.SetActive(false);
            if (centerText) centerText.gameObject.SetActive(false);
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);
        }

        // ===== 내부 유틸 =====
        IEnumerator Typewriter(TMP_Text target, string text)
        {
            target.text = string.Empty;
            if (typewriterSpeed <= 0f) { target.text = text; yield break; }

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

        string ExpandVars(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return VarToken.Replace(input, m =>
            {
                var key = m.Groups[1].Value;
                return GetVar(key);
            });
        }

        // ===== 커맨드 파서 =====
        // @bgm play <path> [volume] [fadeSec] [loop]
        // @bgm stop [fadeSec]
        // @sfx <path> [volume] [pitch]
        // @wait <seconds>
        // @center <text...>
        // @dialogue [on|off]
        // @bg <spritePath>
        // @ch show <left|center|right> <spritePath>
        // @ch hide <left|center|right|all>
        // @set <Key> <Value...>
        // @askname [Key] [Prompt...]
        bool TryProcessCommandLine(string line, out IEnumerator routine)
        {
            routine = null;
            if (string.IsNullOrWhiteSpace(line) || line[0] != '@') return false;

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
                                if (parts.Length < 3) { Debug.LogWarning("[VN] @bgm play: path missing"); return true; }
                                string path = parts[2];
                                float volume = -1f, fade = 0.5f; bool loop = true;
                                if (parts.Length >= 4 && TryFloat(parts[3], out var v)) volume = v;
                                if (parts.Length >= 5 && TryFloat(parts[4], out var f)) fade = f;
                                if (parts.Length >= 6 && bool.TryParse(parts[5], out var b)) loop = b;
                                if (VNAudio.Instance) VNAudio.Instance.PlayBgm(path, volume, fade, loop);
                                else Debug.LogWarning("[VN] VNAudio not found in scene.");
                                return true;
                            }
                            else if (sub == "stop")
                            {
                                float fade = 0.5f;
                                if (parts.Length >= 3 && TryFloat(parts[2], out var f)) fade = f;
                                if (VNAudio.Instance) VNAudio.Instance.StopBgm(fade);
                                else Debug.LogWarning("[VN] VNAudio not found in scene.");
                                return true;
                            }
                        }
                        break;
                    }
                case "sfx":
                    {
                        if (parts.Length < 2) { Debug.LogWarning("[VN] @sfx: path missing"); return true; }
                        string path = parts[1];
                        float volume = -1f, pitch = 1f;
                        if (parts.Length >= 3 && TryFloat(parts[2], out var v)) volume = v;
                        if (parts.Length >= 4 && TryFloat(parts[3], out var p)) pitch = p;
                        if (VNAudio.Instance) VNAudio.Instance.PlaySfx(path, volume, pitch);
                        else Debug.LogWarning("[VN] VNAudio not found in scene.");
                        return true;
                    }
                case "wait":
                    {
                        if (parts.Length >= 2 && TryFloat(parts[1], out var sec)) routine = WaitSeconds(sec);
                        return true;
                    }
                case "center":
                    {
                        string text = line.Substring("@center".Length + 1).Trim();
                        routine = CenterRoutine(ExpandVars(text));
                        return true;
                    }
                case "dialogue":
                    {
                        bool on = true;
                        if (parts.Length >= 2 && parts[1].ToLowerInvariant() == "off") on = false;
                        if (centerText && on) centerText.gameObject.SetActive(false);
                        if (dialoguePanel) dialoguePanel.gameObject.SetActive(on);
                        return true;
                    }
                case "bg":
                    {
                        if (parts.Length >= 2)
                        {
                            string path = parts[1];
                            var sprite = Resources.Load<Sprite>(path);
                            if (!sprite) Debug.LogWarning($"[VN] BG sprite not found: {path}");
                            else if (bgImage) bgImage.sprite = sprite;
                        }
                        else Debug.LogWarning("[VN] @bg: path missing");
                        return true;
                    }
                case "ch":
                    {
                        if (parts.Length < 3) { Debug.LogWarning("[VN] @ch usage: show|hide <left|center|right|all> [spritePath]"); return true; }
                        var action = parts[1].ToLowerInvariant();
                        var slot = parts[2].ToLowerInvariant();

                        if (action == "show")
                        {
                            if (parts.Length < 4) { Debug.LogWarning("[VN] @ch show: sprite path missing"); return true; }
                            string path = parts[3];
                            var sprite = Resources.Load<Sprite>(path);
                            if (!sprite) { Debug.LogWarning($"[VN] Portrait sprite not found: {path}"); return true; }
                            var img = GetSlot(slot);
                            if (!img) { Debug.LogWarning($"[VN] Unknown portrait slot: {slot}"); return true; }
                            img.sprite = sprite; ShowPortrait(img, true);
                            return true;
                        }
                        else if (action == "hide")
                        {
                            if (slot == "all") { ShowPortrait(charLeft, false); ShowPortrait(charCenter, false); ShowPortrait(charRight, false); return true; }
                            var img = GetSlot(slot);
                            if (!img) { Debug.LogWarning($"[VN] Unknown portrait slot: {slot}"); return true; }
                            ShowPortrait(img, false);
                            return true;
                        }
                        Debug.LogWarning("[VN] @ch: unknown action"); return true;
                    }
                case "set":
                    {
                        if (parts.Length < 3) { Debug.LogWarning("[VN] @set usage: @set <Key> <Value>"); return true; }
                        string key = parts[1];
                        string value = line.Substring(line.IndexOf(key, StringComparison.Ordinal) + key.Length).Trim();
                        SetVar(key, value);
                        if (string.Equals(key, "Player", StringComparison.OrdinalIgnoreCase))
                        {
                            PlayerPrefs.SetString(PP_PLAYER, value);
                            PlayerPrefs.Save();
                        }
                        return true;
                    }
                case "askname":
                    {
                        string key = "Player";
                        string prompt = "이름을 입력하세요";
                        if (parts.Length >= 2) key = parts[1];
                        if (parts.Length >= 3) prompt = line.Substring(line.IndexOf(parts[1], StringComparison.Ordinal) + parts[1].Length).Trim();
                        routine = AskName(key, prompt);
                        return true;
                    }
                case "bh":
                    {
                        Debug.LogWarning("[VN] '@bh' is a typo. Use '@bg'. Auto-converting.");
                        if (parts.Length >= 2)
                        {
                            string path = parts[1];
                            var sprite = Resources.Load<Sprite>(path);
                            if (!sprite) Debug.LogWarning($"[VN] BG sprite not found: {path}");
                            else if (bgImage) bgImage.sprite = sprite;
                        }
                        return true;
                    }
            }

            Debug.LogWarning($"[VN] Unknown command: {line}");
            return true;
        }

        // ----- 포트레이트 유틸 -----
        void EnsurePortrait(ref Image img, string name)
        {
            if (img) return;
            if (!charaLayer) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(charaLayer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        Image GetSlot(string slot)
        {
            switch (slot)
            {
                case "left": return charLeft;
                case "center": return charCenter;
                case "right": return charRight;
            }
            return null;
        }

        void ShowPortrait(Image img, bool on)
        {
            if (!img) return;
            img.gameObject.SetActive(on);
            var rt = img.rectTransform;
            if (img == charLeft) rt.anchoredPosition = new Vector2(-420f, 0f);
            if (img == charCenter) rt.anchoredPosition = new Vector2(0f, 0f);
            if (img == charRight) rt.anchoredPosition = new Vector2(420f, 0f);
            if (on && img.sprite) rt.sizeDelta = new Vector2(720f, 900f);
        }

        // ----- 파서/코루틴 유틸 -----
        static bool TryFloat(string s, out float v)
            => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

        static IEnumerator WaitSeconds(float sec) => new WaitForSecondsRealtime(sec);

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
