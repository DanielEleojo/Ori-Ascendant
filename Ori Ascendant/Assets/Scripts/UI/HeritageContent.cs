namespace OriAscendant.UI
{
    /// <summary>
    /// The heritage statement and glossary (GAMEPLAY §3.7, §7 cultural red
    /// lines). Authored content — shipping a glossary + an explicit homage
    /// statement, and labelling each tradition distinctly, is the solo-dev form
    /// of the appropriation literature's consent/context/centering test (§7.7).
    /// Full diacritics are kept here (the glossary is the canonical place for
    /// correct orthography, §7.9).
    /// </summary>
    public static class HeritageContent
    {
        public const string Heritage =
            "Ori Ascendant is a work of fantasy — a respectful homage by a descendant, " +
            "drawing on the Igala, Yoruba, and Igbo traditions of West Africa. It is not a " +
            "depiction of any living religion or its practices. The cultivation framing " +
            "(\"cultivator\", \"tribulation\") is borrowed from the xianxia genre to keep that " +
            "distance clear.\n\n" +
            "No supreme deity is depicted, no initiatory office is used as a rank, and no " +
            "sacred regalia is shown. Ancestors appear only as abstract light. Where the game " +
            "draws on a specific people's belief, it names that people. Corrections from " +
            "members of these communities are welcomed.";

        public readonly struct Term
        {
            public readonly string Word;
            public readonly string Pronunciation;
            public readonly string Meaning;
            public readonly string Tradition;

            public Term(string word, string pronunciation, string meaning, string tradition)
            {
                Word = word;
                Pronunciation = pronunciation;
                Meaning = meaning;
                Tradition = tradition;
            }
        }

        public static readonly Term[] Glossary =
        {
            new Term("Àṣẹ", "AH-sheh", "The power to make things happen; divine life-force. What the cultivator gathers.", "Yoruba"),
            new Term("Orí", "OH-ree", "The inner head — one's chosen destiny, aligned through effort. The game's title and namesake.", "Yoruba"),
            new Term("Ayé", "ah-YEH", "The visible world, \"the marketplace\" — the first tier of cultivation.", "Yoruba"),
            new Term("Ọ̀run", "aw-ROON", "The unseen realm, \"home\" — the second tier, facing the crossing.", "Yoruba"),
            new Term("Ìrékọjá", "ee-REH-kaw-JAH", "\"The Crossing\" — the Tribulation, framed as a homecoming across the river, not a judgment.", "Yoruba"),
            new Term("Ọmọ Ayé", "aw-maw ah-YEH", "\"Child of the World\" — Stage 1, a soul newly arrived.", "Yoruba"),
            new Term("Akẹ́kọ̀ọ́", "ah-KEH-kaw-AW", "\"The Learner\" — Stage 2.", "Yoruba"),
            new Term("Awo", "ah-WOH", "\"The Initiate\" — Stage 3, where a Path is chosen.", "Yoruba"),
            new Term("Aláàṣẹ", "ah-LAH-ah-sheh", "\"Wielder of Àṣẹ\" — Stage 4, one whose word makes things happen.", "Yoruba"),
            new Term("Àgbà", "ahg-BAH", "\"The Elder\" — Stage 5.", "Yoruba"),
            new Term("Aṣẹ́gun", "ah-SHEH-goon", "\"The Victor\" — Stage 6, standing at the river's edge.", "Yoruba"),
            new Term("Ane (Anẹ̀)", "AH-neh", "The earth deity — the Path of Earth, of patience and endurance.", "Igala"),
            new Term("Ṣàngó", "SHAN-go", "The thunder orisha — the Path of Thunder, of sudden force.", "Yoruba"),
            new Term("Ọ̀ṣun", "AW-shun", "The river orisha, mother of generations — the Path of the River, of lineage.", "Yoruba"),
            new Term("Ayé l'ọjà, ọ̀run nilé", "ah-YEH law-JAH, aw-ROON nee-LEH", "\"The world is a marketplace; ọ̀run is home.\" The game's framing proverb.", "Yoruba"),
        };
    }
}
