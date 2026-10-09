using UnityEngine;

namespace CSIVR.Core
{
    /// <summary>A fictional entry in the simulated police criminal database.</summary>
    public class CriminalRecord
    {
        public string Name;
        public string InmateNumber;
        public string FingerprintId;
        public string PriorRecord;
        /// <summary>Path under a Resources folder, without extension. Null for records that have no photo.</summary>
        public string PhotoResource;
    }

    /// <summary>
    /// Fictional police criminal database for case 2. The fingerprint found with the UV scanner is compared against
    /// these records. This is a simplified training device, not real forensics, and a match is a lead, not a verdict.
    /// </summary>
    public static class CriminalDatabase
    {
        public const string CulpritFingerprintId = "FP-8841";

        public static readonly CriminalRecord[] Records =
        {
            new CriminalRecord
            {
                Name = "Sameera Jayalath", InmateNumber = "319054", FingerprintId = "FP-2210",
                PriorRecord = "fraud (2019)",
            },
            new CriminalRecord
            {
                Name = "Ravindu Kalansooriya", InmateNumber = "482736", FingerprintId = CulpritFingerprintId,
                PriorRecord = "burglary (2021), assault (2023)",
                PhotoResource = "Criminals/criminal_482736",
            },
            new CriminalRecord
            {
                Name = "Dinuka Abeywickrama", InmateNumber = "275913", FingerprintId = "FP-7753",
                PriorRecord = "vehicle theft (2020)",
            },
        };

        /// <summary>The fingerprint ID found on an evidence item, or null if the item has no fingerprint.</summary>
        public static string FingerprintFor(string evidenceId) => evidenceId == "E08" ? CulpritFingerprintId : null;

        public static CriminalRecord FindByFingerprint(string fingerprintId)
        {
            foreach (var r in Records)
                if (r.FingerprintId == fingerprintId) return r;
            return null;
        }

        static Sprite s_Photo;

        /// <summary>
        /// Loads the mug shot. A Sprite is created from the texture at runtime, so the image does not need to be
        /// imported with a Sprite texture type.
        /// </summary>
        public static Sprite LoadPhoto(CriminalRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.PhotoResource)) return null;
            if (s_Photo != null) return s_Photo;

            var tex = Resources.Load<Texture2D>(record.PhotoResource);
            if (tex == null)
            {
                Debug.LogWarning($"[CriminalDatabase] Photo '{record.PhotoResource}' not found under a Resources folder.");
                return null;
            }

            s_Photo = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return s_Photo;
        }

        /// <summary>The closing reveal. It ties the fingerprint to the stolen card from case 1.</summary>
        public static string RevealText(CriminalRecord c, StaffRecord cardHolder)
        {
            var holder = cardHolder ?? SuspectRegistry.Staff[0];
            return
                $"Fingerprint match: <b><color=#14958b>{c.Name}</color></b>  (inmate {c.InmateNumber})\n" +
                $"Prior record: {c.PriorRecord}.\n" +
                $"{c.Name} switched off camera 3 at 19:40, then used {holder.Name}'s access card ({holder.CardId}) " +
                "to enter the research centre at 19:42 and killed the researcher.";
        }
    }
}
