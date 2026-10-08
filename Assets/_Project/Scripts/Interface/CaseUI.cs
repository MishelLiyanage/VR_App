using System.Text;
using CSIVR.Core;
using CSIVR.Evidence;
using CSIVR.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CSIVR.Interface
{
    /// <summary>
    /// One room's briefing/progress/submit/debrief board plus a small evidence-log panel at its station.
    /// It observes CaseManager and only shows live content while its own case is the current one.
    /// Buttons call named CaseManager operations.
    /// </summary>
    public class CaseUI : MonoBehaviour
    {
        enum View { Main, Help, Credits }

        [SerializeField] int m_CaseIndex;
        [SerializeField] Transform m_BoardAnchor;
        [SerializeField] Transform m_StationAnchor;

        CaseManager m_Case;
        View m_View;
        string m_Warning;
        string m_LastMessage = "Place a clue on the tray, then let go of it.";

        TextMeshProUGUI m_ChipText, m_Title, m_Body, m_Side;
        RectTransform m_ButtonRow, m_Rows;
        GameObject m_SideCard, m_IconRoot;
        Image m_IconCircle, m_IconGlyph;
        Image[] m_Dots;

        TextMeshProUGUI m_StationBody, m_StationCount;

        CaseDefinition Def => CaseLibrary.Get(m_CaseIndex);

        void Awake()
        {
            BuildBoard();
            BuildStationPanel();
        }

        void OnEnable()
        {
            m_Case = CaseManager.Instance;
            if (m_Case == null)
            {
                Debug.LogWarning("[CaseUI] No CaseManager in the scene.");
                return;
            }
            m_Case.StateChanged += OnStateChanged;
            m_Case.TutorialProgressed += Refresh;
            m_Case.EvidenceRecorded += OnEvidenceRecorded;
            m_Case.Message += OnMessage;
            Refresh();
        }

        void OnDisable()
        {
            if (m_Case == null) return;
            m_Case.StateChanged -= OnStateChanged;
            m_Case.TutorialProgressed -= Refresh;
            m_Case.EvidenceRecorded -= OnEvidenceRecorded;
            m_Case.Message -= OnMessage;
        }

        void OnStateChanged()
        {
            m_View = View.Main;
            m_Warning = null;
            if (m_Case.CaseIndex == m_CaseIndex && m_Case.State == CaseState.Briefing)
                m_LastMessage = "Place a clue on the tray, then let go of it.";
            Refresh();
        }

        void OnEvidenceRecorded(string id) => Refresh();

        void OnMessage(string message)
        {
            if (m_Case.CaseIndex != m_CaseIndex) return;
            m_LastMessage = message;
            RefreshStation();
            if (m_Case.State == CaseState.Investigating) Refresh();
        }

        // ---------- building ----------

        void BuildBoard()
        {
            var canvas = SpatialUI.CreateCanvas("Case Board", m_BoardAnchor, new Vector2(1280f, 720f), 2.4f);
            var root = canvas.transform;

            var frame = SpatialUI.CreateImage(root, "Frame", UISprites.Rounded, SpatialUI.Frame, true, true);
            SpatialUI.Stretch(frame.rectTransform, 0, 0, 0, 0);
            var card = SpatialUI.CreateImage(root, "Card", UISprites.Rounded, SpatialUI.Paper, true, true);
            SpatialUI.Stretch(card.rectTransform, 14, 14, 14, 14);

            // Header: case chip on the left, progress dots on the right.
            var chip = SpatialUI.CreateImage(root, "Chip", UISprites.Rounded, SpatialUI.Teal);
            SpatialUI.TopLeft(chip.rectTransform, 56, 44, 230, 58);
            m_ChipText = SpatialUI.CreateText(chip.transform, "Text", 30f, TextAlignmentOptions.Center, Color.white);
            m_ChipText.fontStyle = FontStyles.Bold;
            m_ChipText.characterSpacing = 6f;
            SpatialUI.Stretch(m_ChipText.rectTransform, 0, 0, 0, 0);

            var dotsRoot = new GameObject("Progress Dots", typeof(RectTransform)).GetComponent<RectTransform>();
            dotsRoot.SetParent(root, false);
            SpatialUI.TopRight(dotsRoot, 60, 58, 4 * 34f + 3 * 14f, 34f);
            m_Dots = new Image[4];
            for (int i = 0; i < m_Dots.Length; i++)
            {
                m_Dots[i] = SpatialUI.CreateImage(dotsRoot, "Dot " + i, UISprites.Circle, SpatialUI.PaperDim, false);
                SpatialUI.TopLeft(m_Dots[i].rectTransform, i * 48f, 0, 34f, 34f);
            }

            m_Title = SpatialUI.CreateText(root, "Title", 56f, TextAlignmentOptions.TopLeft, SpatialUI.Ink);
            m_Title.fontStyle = FontStyles.Bold;
            m_Title.textWrappingMode = TextWrappingModes.NoWrap;
            m_Title.overflowMode = TextOverflowModes.Ellipsis;
            SpatialUI.TopStretch(m_Title.rectTransform, 56, 56, 118, 80);

            var divider = SpatialUI.CreateImage(root, "Divider", null, SpatialUI.Teal, false);
            SpatialUI.TopStretch(divider.rectTransform, 56, 56, 206, 5);

            // Content area (text, evidence rows and a side card share it).
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(root, false);
            SpatialUI.Stretch(content, 56, 56, 226, 150);

            m_Body = SpatialUI.CreateText(content, "Body", 36f, TextAlignmentOptions.TopLeft, SpatialUI.Ink);
            SpatialUI.Stretch(m_Body.rectTransform, 0, 0, 0, 0);

            var rows = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rows.transform.SetParent(content, false);
            m_Rows = (RectTransform)rows.transform;
            m_Rows.anchorMin = new Vector2(0f, 0f);
            m_Rows.anchorMax = new Vector2(0.58f, 1f);
            m_Rows.offsetMin = m_Rows.offsetMax = Vector2.zero;
            var vlg = rows.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var side = SpatialUI.CreateImage(content, "Side Card", UISprites.Rounded, SpatialUI.PaperDim);
            m_SideCard = side.gameObject;
            side.rectTransform.anchorMin = new Vector2(0.62f, 0f);
            side.rectTransform.anchorMax = new Vector2(1f, 1f);
            side.rectTransform.offsetMin = side.rectTransform.offsetMax = Vector2.zero;
            m_Side = SpatialUI.CreateText(side.transform, "Text", 28f, TextAlignmentOptions.TopLeft, SpatialUI.Ink);
            SpatialUI.Stretch(m_Side.rectTransform, 24, 24, 20, 16);

            // Result icon for the debrief.
            m_IconCircle = SpatialUI.CreateImage(content, "Result Icon", UISprites.Circle, SpatialUI.Teal, false);
            SpatialUI.TopLeft(m_IconCircle.rectTransform, 0, 4, 120, 120);
            m_IconRoot = m_IconCircle.gameObject;
            m_IconGlyph = SpatialUI.CreateImage(m_IconCircle.transform, "Glyph", UISprites.Check, Color.white, false);
            SpatialUI.Stretch(m_IconGlyph.rectTransform, 10, 10, 10, 10);

            // Buttons.
            var row = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(root, false);
            m_ButtonRow = (RectTransform)row.transform;
            SpatialUI.BottomStretch(m_ButtonRow, 56, 56, 40, 88);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        void BuildStationPanel()
        {
            if (m_StationAnchor == null) return;

            var canvas = SpatialUI.CreateCanvas("Evidence Log Panel", m_StationAnchor, new Vector2(760f, 340f), 1.0f, interactive: false);
            var root = canvas.transform;

            var card = SpatialUI.CreateImage(root, "Card", UISprites.Rounded, SpatialUI.Navy);
            SpatialUI.Stretch(card.rectTransform, 0, 0, 0, 0);
            var bar = SpatialUI.CreateImage(root, "Accent", UISprites.Rounded, SpatialUI.Teal);
            SpatialUI.TopLeft(bar.rectTransform, 30, 26, 10, 66);

            var title = SpatialUI.CreateText(root, "Title", 46f, TextAlignmentOptions.Left, SpatialUI.Light);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 4f;
            title.text = "EVIDENCE LOG";
            SpatialUI.TopLeft(title.rectTransform, 58, 22, 440, 74);

            var pill = SpatialUI.CreateImage(root, "Count Pill", UISprites.Rounded, SpatialUI.Teal);
            SpatialUI.TopRight(pill.rectTransform, 30, 28, 150, 62);
            m_StationCount = SpatialUI.CreateText(pill.transform, "Text", 40f, TextAlignmentOptions.Center, Color.white);
            m_StationCount.fontStyle = FontStyles.Bold;
            SpatialUI.Stretch(m_StationCount.rectTransform, 0, 0, 0, 0);

            m_StationBody = SpatialUI.CreateText(root, "Body", 32f, TextAlignmentOptions.TopLeft, SpatialUI.TealLight);
            SpatialUI.Stretch(m_StationBody.rectTransform, 34, 34, 118, 20);
        }

        // ---------- content ----------

        void Refresh()
        {
            if (m_Case == null || m_Title == null) return;

            ClearButtons();
            ClearRows();
            SetLayout(body: true, rows: false, side: false, icon: false);
            m_ChipText.text = Def.Name;
            UpdateDots();

            if (m_Case.CaseIndex < m_CaseIndex) ShowLocked();
            else if (m_Case.CaseIndex > m_CaseIndex) ShowClosed();
            else if (m_View == View.Help) ShowHelp();
            else if (m_View == View.Credits) ShowCredits();
            else ShowState();

            RefreshStation();
        }

        void SetLayout(bool body, bool rows, bool side, bool icon)
        {
            m_Body.gameObject.SetActive(body);
            m_Rows.gameObject.SetActive(rows);
            m_SideCard.SetActive(side);
            m_IconRoot.SetActive(icon);
            // Text leaves room for the icon in the debrief.
            SpatialUI.Stretch(m_Body.rectTransform, icon ? 150f : 0f, 0f, 0f, 0f);
        }

        void UpdateDots()
        {
            int current = 0;
            bool live = m_Case.CaseIndex == m_CaseIndex;
            if (m_Case.CaseIndex > m_CaseIndex) current = 4;
            else if (live)
            {
                switch (m_Case.State)
                {
                    case CaseState.Investigating: current = 1; break;
                    case CaseState.ReadyToSubmit: current = 2; break;
                    case CaseState.Debrief: current = 3; break;
                    default: current = 0; break;
                }
            }
            for (int i = 0; i < m_Dots.Length; i++)
            {
                m_Dots[i].color = i < current ? SpatialUI.Teal : i == current ? SpatialUI.Amber : new Color(0.78f, 0.82f, 0.87f, 1f);
                m_Dots[i].rectTransform.localScale = Vector3.one * (i == current ? 1.2f : 1f);
            }
        }

        void ShowState()
        {
            switch (m_Case.State)
            {
                case CaseState.Briefing: ShowBriefing(); break;
                case CaseState.Tutorial: ShowTutorial(); break;
                case CaseState.Investigating: ShowInvestigating(); break;
                case CaseState.ReadyToSubmit: ShowSubmit(); break;
                default: ShowDebrief(); break;
            }
        }

        void ShowBriefing()
        {
            m_Title.text = Def.Title;
            m_Body.fontSize = 36f;
            m_Body.text = Def.BriefingText;
            AddButton("Start", () => m_Case.StartCase(), 260f);
            AddButton("Help", () => Show(View.Help), 200f, ButtonStyle.Secondary);
            AddButton("Credits", () => Show(View.Credits), 230f, ButtonStyle.Secondary);
        }

        void ShowTutorial()
        {
            m_Title.text = $"Practice  {m_Case.TutorialDoneCount}/3";
            m_Body.fontSize = 36f;
            var hints = ControlHints.Current;
            var sb = new StringBuilder();
            sb.AppendLine(Step(TutorialStep.Move, "Move to another spot", hints.Move));
            sb.AppendLine(Step(TutorialStep.Grab, "Pick up and let go of the practice cube", hints.Grab));
            sb.AppendLine(Step(TutorialStep.Activate, "Pick up the UV scanner and switch it on", hints.Activate));
            sb.AppendLine();
            sb.Append("<size=75%><color=#617388>Practice objects do not count as evidence.</color></size>");
            m_Body.text = sb.ToString();
            AddButton("Skip practice", () => m_Case.SkipTutorial(), 300f, ButtonStyle.Secondary);
            AddButton("Help", () => Show(View.Help), 200f, ButtonStyle.Secondary);
        }

        string Step(TutorialStep step, string label, string hint)
        {
            bool done = m_Case.IsTutorialDone(step);
            string mark = done ? "<color=#21bdb0><b>DONE</b></color>" : "<color=#98a5b5>TO DO</color>";
            return $"<size=60%>{mark}</size>  {label}\n<size=62%><color=#617388>{hint}</color></size>";
        }

        void ShowInvestigating()
        {
            var def = Def;
            m_Title.text = $"Investigating   {m_Case.RecordedCount}/{def.RequiredRecords}";
            SetLayout(body: false, rows: true, side: true, icon: false);

            string nextHint = null;
            foreach (var id in def.EvidenceIds)
            {
                bool recorded = m_Case.IsRecorded(id);
                AddEvidenceRow(id, recorded ? def.Info(id).Title : "Not yet recorded", recorded);
                if (!recorded && nextHint == null) nextHint = def.Info(id).Hint;
            }

            var sb = new StringBuilder();
            sb.AppendLine("<size=70%><color=#617388><b>NEXT STEP</b></color></size>");
            sb.AppendLine(nextHint ?? "Everything is recorded.");
            if (!string.IsNullOrEmpty(m_LastMessage))
                sb.AppendLine($"\n<size=68%><color=#617388>{m_LastMessage}</color></size>");
            if (!string.IsNullOrEmpty(m_Warning))
                sb.Append($"\n<size=72%><color=#d9831f><b>{m_Warning}</b></color></size>");
            m_Side.text = sb.ToString();

            AddButton("Submit finding", OnSubmitEarly, 330f);
            AddButton("Restore loose items", OnRestore, 400f, ButtonStyle.Secondary, 30f);
            AddButton("Help", () => Show(View.Help), 180f, ButtonStyle.Secondary);
        }

        void AddEvidenceRow(string id, string title, bool recorded)
        {
            var row = SpatialUI.CreateImage(m_Rows, "Row " + id, UISprites.Rounded,
                recorded ? new Color(0.80f, 0.95f, 0.93f, 1f) : SpatialUI.PaperDim);
            var layoutElement = row.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 66f;
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(22, 18, 6, 6);
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var idText = SpatialUI.CreateText(row.transform, "Id", 28f, TextAlignmentOptions.Left,
                recorded ? SpatialUI.TealDark : SpatialUI.Muted);
            idText.fontStyle = FontStyles.Bold;
            idText.text = id;
            idText.gameObject.AddComponent<LayoutElement>().preferredWidth = 84f;

            var titleText = SpatialUI.CreateText(row.transform, "Title", 30f, TextAlignmentOptions.Left,
                recorded ? SpatialUI.Ink : SpatialUI.Muted);
            titleText.fontStyle = recorded ? FontStyles.Bold : FontStyles.Normal;
            titleText.text = title;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var status = SpatialUI.CreateImage(row.transform, "Status", UISprites.Circle,
                recorded ? SpatialUI.Teal : new Color(0.78f, 0.82f, 0.87f, 1f), false);
            var statusLayout = status.gameObject.AddComponent<LayoutElement>();
            statusLayout.preferredWidth = 42f;
            statusLayout.preferredHeight = 42f;
            if (recorded)
            {
                var tick = SpatialUI.CreateImage(status.transform, "Tick", UISprites.Check, Color.white, false);
                SpatialUI.Stretch(tick.rectTransform, 2, 2, 2, 2);
            }
        }

        void ShowSubmit()
        {
            m_Title.text = "Submit your finding";
            m_Body.fontSize = 36f;
            m_Body.text = Def.SubmitPrompt + $"\n\n<size=72%><color=#617388>{Def.SubmitFootnote}</color></size>";
            foreach (var answer in Def.Answers)
            {
                var a = answer;
                AddButton(a.Label, () => Submit(a.Id), 370f, ButtonStyle.Primary, 30f);
            }
        }

        void ShowDebrief()
        {
            var r = m_Case.LastResult;
            bool correct = r != null && r.Correct;
            m_Title.text = correct ? $"{Def.Name} complete" : "Finding not supported";
            SetLayout(body: true, rows: false, side: false, icon: true);
            m_IconCircle.color = correct ? SpatialUI.Teal : SpatialUI.Amber;
            m_IconGlyph.sprite = correct ? UISprites.Check : UISprites.Cross;
            m_Body.fontSize = 29f;

            if (r == null)
            {
                m_Body.text = "";
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"<size=80%><color=#617388>{m_Case.RecordedCount} / {Def.RequiredRecords} clues recorded</color></size>");
                sb.AppendLine($"Finding: <b><color=#14958b>{r.AnswerLabel}</color></b>");
                sb.AppendLine(r.Explanation);
                sb.AppendLine($"<size=85%><color=#617388>Still unknown: {r.StillUnknown}</color></size>");
                if (correct)
                    sb.Append(m_Case.IsFinalCase
                        ? "\n<b>All cases solved. Well done, investigator.</b>"
                        : "\n<b><color=#14958b>The door to the next room is now unlocked.</color></b>");
                m_Body.text = sb.ToString();
            }

            if (r != null && !r.Correct)
                AddButton("Try again", () => m_Case.RetryFinding(), 260f);
            AddButton("Restart", () => m_Case.Restart(), 230f, ButtonStyle.Secondary);
            AddButton("Credits", () => Show(View.Credits), 230f, ButtonStyle.Secondary);
        }

        void ShowClosed()
        {
            var solved = m_Case.SolvedResult(m_CaseIndex);
            m_Title.text = $"{Def.Name} closed";
            m_Body.fontSize = 34f;
            m_Body.text = solved == null ? "" :
                $"Finding: <b><color=#14958b>{solved.AnswerLabel}</color></b>\n\n{solved.Explanation}";
            AddButton("Restart", () => m_Case.Restart(), 230f, ButtonStyle.Secondary);
        }

        void ShowLocked()
        {
            m_Title.text = "Locked";
            m_Body.fontSize = 36f;
            m_Body.text = "Solve the previous case to unlock this briefing.";
        }

        void ShowHelp()
        {
            m_Title.text = "Help and controls";
            m_Body.fontSize = 32f;
            m_Body.text = ControlHints.Current.Help;
            AddButton("Back", () => Show(View.Main), 220f, ButtonStyle.Secondary);
        }

        void ShowCredits()
        {
            m_Title.text = "Credits";
            m_Body.fontSize = 26f;
            m_Body.text = CaseLibrary.CreditsText;
            AddButton("Back", () => Show(View.Main), 220f, ButtonStyle.Secondary);
        }

        void RefreshStation()
        {
            if (m_StationBody == null || m_Case == null) return;
            m_StationCount.text = $"{m_Case.RecordedIn(m_CaseIndex)} / {Def.RequiredRecords}";
            m_StationBody.text = m_Case.CaseIndex == m_CaseIndex ? m_LastMessage
                : m_Case.CaseIndex > m_CaseIndex ? "Case closed." : "Locked until the earlier case is solved.";
        }

        // ---------- actions ----------

        void OnSubmitEarly()
        {
            var result = m_Case.SubmitFinding(Def.Answers[0].Id);
            if (!result.Accepted)
            {
                m_Warning = result.Explanation;
                AudioFx.PlayAt(AudioFx.Error, m_BoardAnchor.position);
                Refresh();
            }
        }

        void Submit(string answerId) => m_Case.SubmitFinding(answerId);

        void OnRestore()
        {
            int moved = ObjectRecovery.RestoreAll();
            m_LastMessage = moved > 0 ? $"Restored {moved} loose item(s)." : "Nothing needed restoring.";
            Refresh();
        }

        void Show(View view)
        {
            m_View = view;
            Refresh();
        }

        void AddButton(string label, UnityEngine.Events.UnityAction action, float width = 300f,
            ButtonStyle style = ButtonStyle.Primary, float fontSize = 34f)
        {
            SpatialUI.CreateButton(m_ButtonRow, label, () =>
            {
                AudioFx.PlayAt(AudioFx.Click, m_BoardAnchor.position, 0.7f);
                action();
            }, width, 80f, fontSize, style);
        }

        void ClearButtons()
        {
            for (int i = m_ButtonRow.childCount - 1; i >= 0; i--)
            {
                var child = m_ButtonRow.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        void ClearRows()
        {
            for (int i = m_Rows.childCount - 1; i >= 0; i--)
            {
                var child = m_Rows.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }
    }

    /// <summary>Control wording for the active mode (desktop keyboard/mouse, or controllers).</summary>
    public struct ControlHints
    {
        public string Move, Grab, Activate, Help;

        public static ControlHints Current =>
            ModeBootstrap.ActiveMode == RigMode.Desktop ? Desktop : Controllers;

        static readonly ControlHints Desktop = new ControlHints
        {
            Move = "Hold T, aim at the floor, release. Or walk with W A S D.",
            Grab = "Aim at it and hold the right mouse button.",
            Activate = "Hold the right mouse button to pick it up, then press the left mouse button.",
            Help =
                "W A S D  move        Q / E  turn        Mouse  look\n" +
                "Hold T, release  teleport to the spot you aim at\n" +
                "Hold right mouse  grab and hold      Wheel  push / pull\n" +
                "R + mouse  rotate the held object\n" +
                "Left mouse  use the held tool, or press a button\n" +
                "Esc  free the cursor",
        };

        static readonly ControlHints Controllers = new ControlHints
        {
            Move = "Aim the teleport ray forward on the thumbstick and release. Turn with the thumbstick.",
            Grab = "Point at it and squeeze the grip.",
            Activate = "Grab it with the grip, then press the trigger.",
            Help =
                "Thumbstick forward and release  teleport\n" +
                "Thumbstick left / right  snap turn\n" +
                "Grip  grab and hold, release to drop\n" +
                "Rotate your wrist  rotate the held object\n" +
                "Trigger  use the held tool, or press a button",
        };
    }
}
