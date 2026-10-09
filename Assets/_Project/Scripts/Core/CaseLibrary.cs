using System.Collections.Generic;

namespace CSIVR.Core
{
    public struct EvidenceInfo
    {
        public string Title;
        public string Description;
        public string Observation;
        public string Hint;
    }

    public class AnswerOption
    {
        public string Id;
        public string Label;
        public bool Correct;
        public string Summary;
        public string Explanation;
        public string StillUnknown;
    }

    public class SubmitResult
    {
        public int CaseIndex;
        public bool Correct;
        public bool Accepted = true;
        public string AnswerLabel;
        public string Summary;
        public string Explanation;
        public string StillUnknown;
    }

    /// <summary>Everything that differs between the rooms: story, evidence wording and the answer options.</summary>
    public class CaseDefinition
    {
        public string Name;           // "CASE 01"
        public string Title;          // "The Missing Prototype"
        public string BriefingText;
        public string SubmitPrompt;
        public string SubmitFootnote;
        public string[] EvidenceIds;
        public Dictionary<string, EvidenceInfo> Evidence;
        public AnswerOption[] Answers;

        public int RequiredRecords => EvidenceIds.Length;
        public bool Contains(string id) => System.Array.IndexOf(EvidenceIds, id) >= 0;
        public int IndexOf(string id) => System.Array.IndexOf(EvidenceIds, id);
        public EvidenceInfo Info(string id) => Evidence[id];

        public AnswerOption Answer(string id)
        {
            foreach (var a in Answers)
                if (a.Id == id) return a;
            return Answers[Answers.Length - 1];
        }
    }

    /// <summary>Fictional case content, kept in one place so wording is easy to edit.</summary>
    public static class CaseLibrary
    {
        public static readonly CaseDefinition[] Cases =
        {
            new CaseDefinition
            {
                Name = "CASE 01",
                Title = "The Murder Scene",
                BriefingText =
                    "A researcher was found dead in this small research office late last night.\n\n" +
                    "You are a trainee investigator. Inspect the room, record FOUR clues at the evidence station, " +
                    "then choose the entry route the evidence best supports.\n\n" +
                    "This case is fictional. Press Start for a short practice.",
                SubmitPrompt = "All four records are logged.\n\nWhich entry route is best supported by the evidence you recorded?",
                SubmitFootnote = "A card ID shows which card was used, not who used it.",
                EvidenceIds = new[] { "E01", "E02", "E03", "E04" },
                Evidence = new Dictionary<string, EvidenceInfo>
                {
                    ["E01"] = new EvidenceInfo
                    {
                        Title = "Access card",
                        Description = "Blue plastic access card with a white strip. The printed ID reads B-17.",
                        Observation = "Card identifier B-17. Finding a card does not identify who used it.",
                        Hint = "Look for a small card near the desk.",
                    },
                    ["E02"] = new EvidenceInfo
                    {
                        Title = "Access log",
                        Description = "Printed access log. One line reads: B-17, status OK, 19:42.",
                        Observation = "The log records successful B-17 access at 19:42.",
                        Hint = "Check the printed log on the desktop.",
                    },
                    ["E03"] = new EvidenceInfo
                    {
                        Title = "Torn packing label",
                        Description = "Torn packing label. The printed words read PROTOTYPE CONTAINER.",
                        Observation = "The label matches the prototype container. It supports what was moved.",
                        Hint = "Inspect the shelf for packaging.",
                    },
                    ["E04"] = new EvidenceInfo
                    {
                        Title = "UV transfer mark",
                        Description = "A faint violet smear on the cabinet handle under UV light.",
                        Observation = "A transfer mark on the cabinet handle shows it was touched.",
                        Hint = "Use the UV scanner on the cabinet handle.",
                    },
                },
                Answers = new[]
                {
                    new AnswerOption
                    {
                        Id = "door", Label = "Card-controlled door", Correct = true,
                        Summary = "Well supported finding.",
                        Explanation =
                            "The access log records successful use of card B-17 at 19:42, the torn label matches the " +
                            "prototype container, and the UV mark shows the cabinet handle was touched. " +
                            "Together these support entry through the card-controlled door.",
                        StillUnknown = "Who was holding card B-17 is not established. A card ID is not proof of a person's guilt.",
                    },
                    new AnswerOption
                    {
                        Id = "window", Label = "Forced window entry", Correct = false,
                        Summary = "The evidence does not support this route.",
                        Explanation =
                            "Nothing you recorded shows a forced window. The access log and card both point to the door. " +
                            "Review what each record actually shows, then try again.",
                        StillUnknown = "The window was never examined as an entry route in this case.",
                    },
                    new AnswerOption
                    {
                        Id = "insufficient", Label = "Insufficient evidence", Correct = false,
                        Summary = "There was enough evidence to compare the routes.",
                        Explanation =
                            "With four records you can compare the two routes. The log and card link activity to the door, " +
                            "and nothing points to the window. Review the records and try again.",
                        StillUnknown = "The identity of the person who used the card remains unknown.",
                    },
                },
            },
            new CaseDefinition
            {
                Name = "CASE 02",
                Title = "The Camera Blackout",
                BriefingText =
                    "Security camera 3 went dark at 19:40, just before the prototype vanished.\n\n" +
                    "Inspect this security office, record FOUR clues at the evidence station, " +
                    "then decide why the camera lost its feed.\n\n" +
                    "The UV scanner is on the tool table. Press Start when you are ready.",
                SubmitPrompt = "All four records are logged.\n\nWhy did camera 3 lose its feed at 19:40?",
                SubmitFootnote = "A fingerprint match is a lead for the police, not a verdict.",
                EvidenceIds = new[] { "E05", "E06", "E07", "E08" },
                Evidence = new Dictionary<string, EvidenceInfo>
                {
                    ["E05"] = new EvidenceInfo
                    {
                        Title = "Maintenance ticket",
                        Description = "Maintenance ticket form number 4471. The scheduled work box is empty.",
                        Observation = "No camera maintenance was scheduled for this evening.",
                        Hint = "Look for paperwork on the console desk.",
                    },
                    ["E06"] = new EvidenceInfo
                    {
                        Title = "Camera status report",
                        Description = "Camera 3 status report. Power lost at 19:40. No network or software fault is listed.",
                        Observation = "Camera 3 lost power at 19:40. Network and software logs show no faults.",
                        Hint = "Check the report on the side table.",
                    },
                    ["E07"] = new EvidenceInfo
                    {
                        Title = "Cable tag",
                        Description = "Orange cable tag. It reads CAM-3 POWER.",
                        Observation = "The tag reads CAM-3 POWER. It identifies the switch that feeds the camera.",
                        Hint = "Inspect the shelf by the east wall.",
                    },
                    ["E08"] = new EvidenceInfo
                    {
                        Title = "UV fingerprint",
                        Description = "A faint violet fingerprint on the CAM-3 power switch under UV light.",
                        Observation = "A fingerprint on the CAM-3 power switch shows it was touched. It can be searched in the police database.",
                        Hint = "Use the UV scanner on the CAM-3 power switch.",
                    },
                },
                Answers = new[]
                {
                    new AnswerOption
                    {
                        Id = "manual", Label = "Switch turned off by hand", Correct = true,
                        Summary = "Well supported finding.",
                        Explanation =
                            "No maintenance was scheduled, the status report shows a sudden power loss with no software fault, " +
                            "the tag identifies the CAM-3 power switch, and the UV mark shows that switch was touched. " +
                            "Together these support the camera being switched off by hand.",
                        StillUnknown = "A fingerprint match identifies a suspect, but only a court can decide guilt.",
                    },
                    new AnswerOption
                    {
                        Id = "maintenance", Label = "Scheduled maintenance outage", Correct = false,
                        Summary = "The evidence does not support this explanation.",
                        Explanation =
                            "The maintenance ticket says no work was scheduled that evening, and the status report shows a " +
                            "sudden power loss. Review those records and try again.",
                        StillUnknown = "Whether any unrecorded maintenance happened is not shown by the evidence.",
                    },
                    new AnswerOption
                    {
                        Id = "insufficient", Label = "Insufficient evidence", Correct = false,
                        Summary = "There was enough evidence to decide.",
                        Explanation =
                            "With four records you can rule out maintenance and a software fault. The tag and the UV mark " +
                            "point at the power switch. Review the records and try again.",
                        StillUnknown = "The identity of the person who touched the switch remains unknown.",
                    },
                },
            },
        };

        public static int Count => Cases.Length;
        public static CaseDefinition Get(int index) => Cases[System.Math.Max(0, System.Math.Min(index, Cases.Length - 1))];

        public static int CaseOf(string id)
        {
            for (int i = 0; i < Cases.Length; i++)
                if (Cases[i].Contains(id)) return i;
            return -1;
        }

        public static bool IsValidId(string id) => id != null && CaseOf(id) >= 0;

        public static EvidenceInfo Info(string id)
        {
            int c = CaseOf(id);
            return c >= 0 ? Cases[c].Info(id) : new EvidenceInfo { Title = id, Observation = "", Hint = "" };
        }

        public static SubmitResult Evaluate(int caseIndex, string answerId)
        {
            var a = Get(caseIndex).Answer(answerId);
            return new SubmitResult
            {
                CaseIndex = caseIndex,
                Correct = a.Correct,
                AnswerLabel = a.Label,
                Summary = a.Summary,
                Explanation = a.Explanation,
                StillUnknown = a.StillUnknown,
            };
        }

        public const string CreditsText =
            "UNITY AND PACKAGES\n" +
            "Unity 6 and Universal Render Pipeline - Unity Technologies\n" +
            "XR Interaction Toolkit 3.x and Starter Assets sample - Unity Technologies\n" +
            "OpenXR Plugin, XR Plug-in Management, Input System, TextMesh Pro - Unity Technologies\n\n" +
            "CONTENT\n" +
            "All rooms and props are original Unity primitives. Textures, UI shapes and sounds are implemented in code.\n" +
            "SOUND EFFECTS\n" +
            "Door opening sound - Sound Effect by Jurij from Pixabay\n" +
            "Background music - Music by Jacob Potison from Pixabay \n" +
            "CCTV Camera footages - Youtube\n" +
            "The UV scanner is a simplified fictional training device, not a forensic instrument.\n\n";
    }
}
