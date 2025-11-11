// Assets/main/VN/Scripts/VNEngine.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    [DisallowMultipleComponent]
    public class VNEngine : MonoBehaviour
    {
        [Header("UI Roots")]
        [SerializeField] private Image bgImage;
        [SerializeField] private RectTransform charaLayer;

        [Header("Portrait Slots (Images in CharaLayer)")]
        [SerializeField] private Image charLeft;
        [SerializeField] private Image charCenter;
        [SerializeField] private Image charRight;

        [Header("Dialogue UI")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private RectTransform dialoguePanel;
        [SerializeField] private TMP_Text centerText;

        [Header("Choices")]
        [SerializeField] private RectTransform choicesPanel;
        [SerializeField] private Button choiceButtonPrefab;

        [Header("Typing")]
        [SerializeField, Range(0f, 0.2f)] private float typewriterSpeed = 0.02f;

        [Header("Name Input")]
        [SerializeField] private RectTransform nameInputPanel;
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button nameConfirmButton;

        [Header("Fade Overlay (black full-screen Image)")]
        [SerializeField] private Image fadeOverlay;

        [Header("Script")]
        [SerializeField] private VNScript scriptBehaviour;
        [SerializeField] private bool autoStart = true;

        // runtime
        private bool clickRequested;
        private bool inputBlocked;
        private string lastChoiceId;
        private readonly Dictionary<string, string> vars = new();
        private readonly Dictionary<Image, Vector2> baseOffset = new();

        void Awake()
        {
            if (centerText) centerText.gameObject.SetActive(false);
            if (choicesPanel) choicesPanel.gameObject.SetActive(false);
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);
            if (nameInputPanel) nameInputPanel.gameObject.SetActive(false);

            if (!charLeft) charLeft = CreateCharSlot("CharLeft");
            if (!charCenter) charCenter = CreateCharSlot("CharCenter");
            if (!charRight) charRight = CreateCharSlot("CharRight");
            RememberBase(charLeft); RememberBase(charCenter); RememberBase(charRight);

            if (!fadeOverlay) fadeOverlay = CreateFadeOverlay();
            PushFadeOverlayToTop();
            SetOverlayAlpha(0f);
        }

        void Start()
        {
            if (autoStart && scriptBehaviour) StartCoroutine(Run());
        }

        public IEnumerator Run() => scriptBehaviour ? scriptBehaviour.Define(this) : null;

        // ===================== Public Script API =====================
        public IEnumerator Say(string speaker, string message)
        {
            if (IsCmd(message, out var co)) { yield return co; yield break; }

            if (centerText) centerText.gameObject.SetActive(false);
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);

            if (nameText) nameText.text = ReplaceVars(speaker ?? string.Empty);
            if (bodyText)
            {
                yield return Typewriter(bodyText, ReplaceVars(message ?? string.Empty));
                yield return WaitForClick();
            }
        }

        public IEnumerator Line(string message)
        {
            if (IsCmd(message, out var co)) { yield return co; yield break; }

            if (centerText) centerText.gameObject.SetActive(false);
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);

            if (nameText) nameText.text = string.Empty;
            if (bodyText)
            {
                yield return Typewriter(bodyText, ReplaceVars(message ?? string.Empty));
                yield return WaitForClick();
            }
        }

        public IEnumerator Center(string message)
        {
            if (IsCmd(message, out var co)) { yield return co; yield break; }

            if (dialoguePanel) dialoguePanel.gameObject.SetActive(false);
            if (centerText)
            {
                centerText.gameObject.SetActive(true);
                centerText.text = ReplaceVars(message ?? string.Empty);
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
                    if (label) label.text = ReplaceVars(op.text ?? "…");
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

        // ===================== Input & Typewriter =====================
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
            if (!inputBlocked && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
                clickRequested = true;
        }

        // ===================== Command Parser =====================
        bool IsCmd(string msg, out IEnumerator co)
        {
            co = null;
            if (string.IsNullOrEmpty(msg) || msg[0] != '@') return false;
            co = ProcessCmd(msg);
            return true;
        }

        IEnumerator ProcessCmd(string line)
        {
            var tok = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tok.Length == 0) yield break;
            string c = tok[0].ToLowerInvariant();

            switch (c)
            {
                case "@dialogue":
                    {
                        bool on = tok.Length < 2 || tok[1].ToLowerInvariant() == "on";
                        if (dialoguePanel) dialoguePanel.gameObject.SetActive(on);
                        if (centerText) centerText.gameObject.SetActive(!on);
                        yield break;
                    }

                case "@bg":
                    {
                        if (tok.Length >= 2 && tok[1].ToLowerInvariant() == "fade")
                        {
                            // @bg fade BG/path duration
                            if (tok.Length < 4) yield break;
                            string path = tok[2];
                            float dur = F(tok[3], 0.6f);
                            yield return FadeBGSequence(path, dur);
                        }
                        else
                        {
                            // @bg BG/path
                            if (tok.Length < 2) yield break;
                            string path = tok[1];
                            var sp = LoadSpriteSmart(path);
                            if (!sp) { Debug.LogWarning($"[VN] BG sprite not found: {path}"); yield break; }
                            if (bgImage)
                            {
                                bgImage.sprite = sp;
                                FitFull(bgImage.rectTransform);
                            }
                        }
                        yield break;
                    }

                case "@bgm":
                    {
                        if (tok.Length >= 2 && tok[1].ToLowerInvariant() == "play")
                        {
                            string path = tok.Length >= 3 ? tok[2] : null;
                            float vol = tok.Length >= 4 ? F(tok[3], 0.8f) : 0.8f;
                            float fad = tok.Length >= 5 ? F(tok[4], 0.5f) : 0.5f;
                            bool loop = tok.Length >= 6 ? B(tok[5], true) : true;
                            VNAudio.Instance?.PlayBgm(path, vol, fad, loop);
                        }
                        else if (tok.Length >= 2 && tok[1].ToLowerInvariant() == "stop")
                        {
                            float fad = tok.Length >= 3 ? F(tok[2], 0.5f) : 0.5f;
                            VNAudio.Instance?.StopBgm(fad);
                        }
                        yield break;
                    }

                case "@sfx":
                    {
                        string path = tok.Length >= 2 ? tok[1] : null;
                        float vol = tok.Length >= 3 ? F(tok[2], 1f) : 1f;
                        float pitch = tok.Length >= 4 ? F(tok[3], 1f) : 1f;
                        VNAudio.Instance?.PlaySfx(path, vol, pitch);
                        yield break;
                    }

                case "@askname":
                    {
                        if (tok.Length < 2) yield break;
                        string key = tok[1];
                        string prompt = line.Substring(line.IndexOf(tok[1]) + tok[1].Length).Trim();
                        yield return AskNameCo(key, prompt);
                        yield break;
                    }

                case "@fade":
                    {
                        if (tok.Length < 3) yield break;
                        bool toBlack = tok[1].ToLowerInvariant() == "out";
                        float dur = F(tok[2], 0.5f);
                        yield return FadeTo(toBlack ? 1f : 0f, dur);
                        yield break;
                    }

                case "@ch":
                    {
                        if (tok.Length < 2) yield break;
                        string sub = tok[1].ToLowerInvariant();

                        if (sub == "show")
                        {
                            // @ch show <left|center|right> <path> [scale] [fadeSec] [preserve(bool)] [ox] [oy]
                            if (tok.Length < 4) yield break;
                            var img = GetSlot(tok[2]); if (!img) yield break;

                            string path = tok[3];
                            float scale = tok.Length >= 5 ? F(tok[4], 1f) : 1f;
                            float fade = tok.Length >= 6 ? F(tok[5], 0f) : 0f;
                            bool preserve = tok.Length >= 7 ? B(tok[6], false) : false;
                            float ox = tok.Length >= 8 ? F(tok[7], 0f) : 0f;
                            float oy = tok.Length >= 9 ? F(tok[8], 0f) : 0f;

                            var sp = LoadSpriteSmart(path);
                            if (!sp) { Debug.LogWarning($"[VN] Chara sprite not found: {path}"); yield break; }

                            img.preserveAspect = preserve;
                            img.sprite = sp;
                            img.SetNativeSize();
                            FitCenter(img.rectTransform);
                            img.rectTransform.localScale = Vector3.one * scale;
                            img.rectTransform.anchoredPosition = Base(img) + new Vector2(ox, oy);
                            img.gameObject.SetActive(true);

                            if (fade > 0f) yield return FadeImage(img, 0f, 1f, fade);
                        }
                        else if (sub == "hide")
                        {
                            // @ch hide <left|center|right|all> [fadeSec]
                            string pos = tok.Length >= 3 ? tok[2].ToLowerInvariant() : "all";
                            float fade = tok.Length >= 4 ? F(tok[3], 0.2f) : 0.2f;

                            if (pos == "all")
                            {
                                yield return HideOne(charLeft, fade);
                                yield return HideOne(charCenter, fade);
                                yield return HideOne(charRight, fade);
                            }
                            else yield return HideOne(GetSlot(pos), fade);
                        }
                        else if (sub == "size")
                        {
                            // @ch size <left|center|right> <scale> [duration]
                            if (tok.Length < 4) yield break;
                            var img = GetSlot(tok[2]); if (!img) yield break;
                            float sc = F(tok[3], 1f);
                            float dur = tok.Length >= 5 ? F(tok[4], 0f) : 0f;
                            if (dur <= 0f) img.rectTransform.localScale = Vector3.one * sc;
                            else yield return ScaleTo(img.rectTransform, sc, dur);
                        }
                        else if (sub == "offset")
                        {
                            // @ch offset <left|center|right> <x> <y>
                            if (tok.Length < 5) yield break;
                            var img = GetSlot(tok[2]); if (!img) yield break;
                            float ox = F(tok[3], 0f);
                            float oy = F(tok[4], 0f);
                            img.rectTransform.anchoredPosition = Base(img) + new Vector2(ox, oy);
                        }
                        else if (sub == "shake")
                        {
                            // @ch shake <left|center|right> <duration> <amplitude>
                            if (tok.Length < 5) yield break;
                            var img = GetSlot(tok[2]); if (!img) yield break;
                            float dur = F(tok[3], 0.2f);
                            float amp = F(tok[4], 20f);
                            yield return Shake(img.rectTransform, dur, amp);
                            img.rectTransform.anchoredPosition = Base(img);
                        }
                        yield break;
                    }

                default:
                    Debug.LogWarning($"[VN] Unknown command: {line}");
                    yield break;
            }
        }

        // ===================== BG Fade (out→swap→in) =====================
        IEnumerator FadeBGSequence(string path, float duration)
        {
            PushFadeOverlayToTop();
            yield return FadeTo(1f, duration); // 덮기
            var sp = LoadSpriteSmart(path);
            if (!sp) Debug.LogWarning($"[VN] BG sprite not found: {path}");
            if (bgImage)
            {
                bgImage.sprite = sp;
                FitFull(bgImage.rectTransform);
            }
            yield return FadeTo(0f, duration); // 드러내기
        }

        IEnumerator FadeTo(float target, float sec)
        {
            if (!fadeOverlay) yield break;
            PushFadeOverlayToTop();
            float a0 = fadeOverlay.color.a;
            float t = 0f;
            while (t < sec)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / sec);
                SetOverlayAlpha(Mathf.Lerp(a0, target, k));
                yield return null;
            }
            SetOverlayAlpha(target);
        }

        void SetOverlayAlpha(float a)
        {
            var c = fadeOverlay.color; c.a = a; fadeOverlay.color = c;
        }

        void PushFadeOverlayToTop()
        {
            if (!fadeOverlay) return;
            var rt = fadeOverlay.rectTransform;
            rt.SetParent(dialoguePanel ? dialoguePanel.parent : charaLayer, true);
            rt.SetAsLastSibling();
            FitFull(rt);
            fadeOverlay.raycastTarget = false;
        }

        // ===================== Name Input =====================
        IEnumerator AskNameCo(string key, string prompt)
        {
            inputBlocked = true;

            if (dialoguePanel) dialoguePanel.gameObject.SetActive(false);
            if (centerText)
            {
                centerText.gameObject.SetActive(true);
                centerText.text = string.IsNullOrWhiteSpace(prompt) ? "이름을 입력하세요" : ReplaceVars(prompt);
            }

            if (!nameInputPanel || !nameInputField || !nameConfirmButton)
            {
                Debug.LogWarning("[VN] Name input UI not assigned. Using default 'Player'.");
                vars[key] = "Player";
                inputBlocked = false;
                yield break;
            }

            nameInputPanel.gameObject.SetActive(true);
            nameInputField.text = string.Empty;
            nameInputField.Select();
            nameInputField.ActivateInputField();

            bool decided = false;
            void Decide()
            {
                var v = nameInputField.text;
                if (string.IsNullOrWhiteSpace(v)) v = "Player";
                vars[key] = v.Trim();
                decided = true;
            }
            nameConfirmButton.onClick.RemoveAllListeners();
            nameConfirmButton.onClick.AddListener(Decide);

            while (!decided)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Decide();
                yield return null;
            }

            nameInputPanel.gameObject.SetActive(false);
            if (centerText) centerText.gameObject.SetActive(false);
            if (dialoguePanel) dialoguePanel.gameObject.SetActive(true);

            inputBlocked = false;
        }

        // ===================== Helpers =====================
        Image CreateCharSlot(string name)
        {
            if (!charaLayer) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(charaLayer, false);
            FitCenter(rt);
            var img = go.GetComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = false;
            go.SetActive(false);
            return img;
        }

        Image CreateFadeOverlay()
        {
            var parent = dialoguePanel ? dialoguePanel.parent : (Transform)charaLayer;
            var go = new GameObject("FadeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            FitFull(rt);
            var img = go.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = false;
            return img;
        }

        Image GetSlot(string pos)
        {
            switch (pos.ToLowerInvariant())
            {
                case "left": return charLeft;
                case "center": return charCenter;
                case "right": return charRight;
                default: return null;
            }
        }

        void RememberBase(Image img)
        {
            if (!img) return;
            if (!baseOffset.ContainsKey(img))
                baseOffset[img] = img.rectTransform.anchoredPosition;
        }
        Vector2 Base(Image img) => (img && baseOffset.TryGetValue(img, out var v)) ? v : Vector2.zero;

        Sprite LoadSpriteSmart(string path)
        {
            var sp = Resources.Load<Sprite>(path);
            if (sp) return sp;
            var arr = Resources.LoadAll<Sprite>(path);
            if (arr != null && arr.Length > 0) return arr[0];
            return null;
        }

        static float F(string s, float def) =>
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;

        static bool B(string s, bool def)
        {
            if (bool.TryParse(s, out var b)) return b;
            if (s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (s == "0" || s.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            return def;
        }

        static void FitFull(RectTransform rt)
        {
            if (!rt) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;
        }

        static void FitCenter(RectTransform rt)
        {
            if (!rt) return;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        IEnumerator FadeImage(Image img, float aFrom, float aTo, float sec)
        {
            if (!img) yield break;
            var c = img.color; c.a = aFrom; img.color = c;
            img.gameObject.SetActive(true);
            float t = 0f;
            while (t < sec)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / sec);
                c.a = Mathf.Lerp(aFrom, aTo, k);
                img.color = c;
                yield return null;
            }
            c.a = aTo; img.color = c;
        }

        IEnumerator HideOne(Image img, float fade)
        {
            if (!img || !img.gameObject.activeSelf) yield break;
            if (fade > 0f) yield return FadeImage(img, img.color.a, 0f, fade);
            img.gameObject.SetActive(false);
        }

        IEnumerator ScaleTo(RectTransform rt, float targetScale, float sec)
        {
            if (!rt) yield break;
            Vector3 s0 = rt.localScale, s1 = Vector3.one * targetScale;
            float t = 0f;
            while (t < sec)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / sec);
                rt.localScale = Vector3.Lerp(s0, s1, k);
                yield return null;
            }
            rt.localScale = s1;
        }

        IEnumerator Shake(RectTransform rt, float sec, float amp)
        {
            if (!rt) yield break;
            Vector2 basePos = rt.anchoredPosition;
            float t = 0f;
            while (t < sec)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(t / sec);
                float ax = UnityEngine.Random.Range(-amp, amp) * k;
                float ay = UnityEngine.Random.Range(-amp, amp) * k;
                rt.anchoredPosition = basePos + new Vector2(ax, ay);
                yield return null;
            }
            rt.anchoredPosition = basePos;
        }

        // ===== Variable replace ({Key} -> value) =====
        string ReplaceVars(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            foreach (var kv in vars)
                s = s.Replace("{" + kv.Key + "}", kv.Value);
            return s;
        }
    }
}
