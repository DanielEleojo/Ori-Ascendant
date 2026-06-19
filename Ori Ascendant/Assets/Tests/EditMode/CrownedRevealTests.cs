using NUnit.Framework;
using OriAscendant.Data;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Pipeline seam for the crowned Ascended reveal (PRD Phase 6, issue #11).
    /// TribulationConfig.RevealSprite() selects the bespoke crowned portrait when
    /// the slot is filled and the outcome is ascend; otherwise falls back to the
    /// Stage-6 humble Victor portrait + gold-FX overlay (slice 6 fallback).
    /// </summary>
    public class CrownedRevealTests
    {
        private static Sprite MakeSprite() =>
            Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);

        [Test]
        public void CrownedAscendedRevealPortrait_DefaultsNull()
        {
            var config = ScriptableObject.CreateInstance<TribulationConfig>();
            Assert.IsNull(config.crownedAscendedRevealPortrait,
                "slot must be null until funded art clears §7.10");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void RevealSprite_FallsBackToVictoryPortrait_WhenCrownedNull()
        {
            var config = ScriptableObject.CreateInstance<TribulationConfig>();
            Sprite victoryPortrait = MakeSprite();
            Assert.AreEqual(victoryPortrait, config.RevealSprite(true, victoryPortrait),
                "no crowned art → Stage-6 portrait + FX fallback path");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void RevealSprite_UsesCrownedPortrait_WhenAscendedAndCrownedSet()
        {
            var config = ScriptableObject.CreateInstance<TribulationConfig>();
            Sprite victoryPortrait = MakeSprite();
            Sprite crownedPortrait = MakeSprite();
            config.crownedAscendedRevealPortrait = crownedPortrait;

            Assert.AreEqual(crownedPortrait, config.RevealSprite(true, victoryPortrait),
                "crowned art present + ascended → bespoke reveal");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void RevealSprite_NeverUsesCrownedPortrait_OnFall()
        {
            var config = ScriptableObject.CreateInstance<TribulationConfig>();
            Sprite victoryPortrait = MakeSprite();
            Sprite crownedPortrait = MakeSprite();
            config.crownedAscendedRevealPortrait = crownedPortrait;

            Assert.AreEqual(victoryPortrait, config.RevealSprite(false, victoryPortrait),
                "crown goes on only after a successful Crossing — never shown on fall");
            Object.DestroyImmediate(config);
        }
    }
}
