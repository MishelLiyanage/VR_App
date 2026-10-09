namespace CSIVR.Core
{
    /// <summary>A fictional member of staff in the card registry.</summary>
    public class StaffRecord
    {
        public string Name;
        public string Role;
        public string CardId;
        /// <summary>Where this person was last recorded, as a phrase that follows "who ..." in a sentence.</summary>
        public string LastSeen;
    }

    /// <summary>
    /// Fictional staff card registry for case 1. Looking up the access card gives a person of interest,
    /// never proof of guilt: the registry shows whose card it was, not who was holding it.
    /// </summary>
    public static class SuspectRegistry
    {
        public static readonly StaffRecord[] Staff =
        {
            new StaffRecord
            {
                Name = "Arjun Mendis", Role = "Lab Technician", CardId = "B-17",
                LastSeen = "badged out of the main entrance at 19:15",
            },
            new StaffRecord
            {
                Name = "Nimali Fernando", Role = "Security Officer", CardId = "A-04",
                LastSeen = "was on duty at the front desk until 20:30",
            },
            new StaffRecord
            {
                Name = "Kasun Perera", Role = "Facilities Supervisor", CardId = "C-31",
                LastSeen = "was at the loading dock from 19:30 to 20:00",
            },
        };

        /// <summary>The card number printed on an evidence item, or null if the item is not a card.</summary>
        public static string CardIdFor(string evidenceId) => evidenceId == "E01" ? "B-17" : null;

        public static StaffRecord FindByCard(string cardId)
        {
            foreach (var s in Staff)
                if (s.CardId == cardId) return s;
            return null;
        }

        /// <summary>Text for the evidence station panel once the lookup finishes.</summary>
        public static string ResultText(StaffRecord r) =>
            $"<b>REGISTRY MATCH</b>\n" +
            $"Card {r.CardId}: <b>{r.Name}</b>, {r.Role}\n" +
            $"Last seen: {r.LastSeen}\n" +
            "<size=70%>A registered holder is not proof of who used the card.</size>";

        /// <summary>Line added to the case 1 debrief when the card was looked up.</summary>
        public static string DebriefNote(StaffRecord r) =>
            $"Registry: card {r.CardId} is registered to {r.Name} ({r.Role}), who {r.LastSeen}. " +
            "The log shows the card used at 19:42, so someone else may have been holding it. " +
            "A registered holder is a lead, not proof of guilt.";
    }
}
