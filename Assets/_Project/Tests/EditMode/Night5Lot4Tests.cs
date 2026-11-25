using ItHunts.Enemies;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Nuit 5, lot 4 de la DA (PR #57, ART-HANDOFF « plus de rouge, lien du livre, objet tenu, apparitions, sons de l'épée ») : réglages
    /// branchés, prefabs des ennemis, variantes de sons sans répétition, sortie du sol des goules.
    /// </summary>
    public class Night5Lot4Tests
    {
        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static void Near(Color c, float r, float g, float b, string what)
        {
            Assert.That(c.r, Is.EqualTo(r).Within(1e-3f), what);
            Assert.That(c.g, Is.EqualTo(g).Within(1e-3f), what);
            Assert.That(c.b, Is.EqualTo(b).Within(1e-3f), what);
        }

        [Test]
        public void Annonces_PlusDeRouge_IndicesSombresDeLaDA()
        {
            var ghoul = Load<GhoulSettings>("Assets/_Project/Settings/Ghoul_Default.asset");
            Near(ghoul.leapGlowColor, 0.45f, 0.36f, 0.26f, "lueur du bond : brun d'os");
            Assert.That(ghoul.leapBodyTint, Is.EqualTo(0f));
            Assert.That(ghoul.leapTelegraphMaterial, Is.Not.Null);
            Assert.That(ghoul.leapTelegraphMaterial.name, Is.EqualTo("M_VFX_Indice_Cercle"));

            var flyer = Load<FlyerSettings>("Assets/_Project/Settings/Flyer_Default.asset");
            Near(flyer.diveGlowColor, 0.45f, 0.36f, 0.26f, "yeux du piqué");
            Assert.That(flyer.contourAnnonce != null && flyer.contourAnnonce.name == "M_VFX_Volant_Annonce", Is.True);
            Assert.That(flyer.contourPrise != null && flyer.contourPrise.name == "M_VFX_Prise_Contour", Is.True);

            var stalker = Load<StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset");
            Assert.That(stalker.grabColumn, Is.False, "plus de colonne rouge");
            Assert.That(stalker.grabWarningMaterial != null && stalker.grabWarningMaterial.name == "M_VFX_Indice_Rect", Is.True);
            Assert.That(stalker.backhandZoneMaterial != null && stalker.backhandZoneMaterial.name == "M_VFX_Indice_Cercle", Is.True);

            // Valeurs par défaut du code (un nouvel asset) : les mêmes.
            var g = ScriptableObject.CreateInstance<GhoulSettings>();
            var f = ScriptableObject.CreateInstance<FlyerSettings>();
            var s = ScriptableObject.CreateInstance<StalkerSettings>();
            try
            {
                Near(g.leapGlowColor, 0.45f, 0.36f, 0.26f, "défaut goule");
                Near(f.diveGlowColor, 0.45f, 0.36f, 0.26f, "défaut volant");
                Assert.That(s.grabColumn, Is.False, "défaut traqueur");
            }
            finally
            {
                Object.DestroyImmediate(g);
                Object.DestroyImmediate(f);
                Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void PrefabsDesEnnemis_IndiceDuBond_PlusDeSphereDuPique()
        {
            var ghoul = Load<GameObject>("Assets/_Project/Prefabs/Niveau/Goule.prefab");
            Transform leap = ghoul.transform.Find("Annonce du bond");
            Assert.That(leap, Is.Not.Null);
            Assert.That(leap.GetComponent<Renderer>().sharedMaterial.name, Is.EqualTo("M_VFX_Indice_Cercle"));

            var flyer = Load<GameObject>("Assets/_Project/Prefabs/Niveau/Volant.prefab");
            Assert.That(flyer.transform.Find("Annonce du piqué"), Is.Null, "plus de boule rouge");
            Assert.That(flyer.GetComponent("ContourDiscret"), Is.Not.Null, "contour discret sur la racine");
        }

        [Test]
        public void ObjetTenu_LueursBasses_TraitDeLaDA()
        {
            var tk = Load<TelekinesisFeedbackSettings>("Assets/_Project/Settings/TelekinesisFeedback_Default.asset");
            Assert.That(tk.glowHeld, Is.EqualTo(0f), "plus d'objet tout violet");
            Assert.That(tk.glowAimed, Is.LessThanOrEqualTo(0.5f));
            Assert.That(tk.glowInRange, Is.LessThanOrEqualTo(0.15f));
            Assert.That(tk.heldOutline, Is.Not.Null, "contour de prise");
            Assert.That(tk.enemyStruggleGlow, Is.GreaterThan(0f), "le volant tenu qui va se libérer clignote encore");

            var vfx = Load<GameVfxSettings>("Assets/_Project/Resources/GameVfx_Default.asset");
            Assert.That(vfx.heldTrait, Is.Not.Null);
            Assert.That(vfx.heldTrait.GetComponent("LienVfx"), Is.Not.Null, "VFX_Trait_Prise : un LienVfx");
            Assert.That(vfx.spawnHole != null && vfx.spawnShadow != null && vfx.spawnGrate != null, Is.True, "effets d'apparition");
            Assert.That(vfx.spawnHoleRise.x, Is.EqualTo(0.25f).Within(1e-4f));
            Assert.That(vfx.spawnHoleRise.y, Is.EqualTo(0.9f).Within(1e-4f));
            Assert.That(vfx.spawnShadowDelay, Is.EqualTo(0.45f).Within(1e-4f));
        }

        [Test]
        public void LanterneDuHibou_EtHalosReservesAuDecor()
        {
            var owl = Load<CharacterDefinition>("Assets/_Project/Settings/Personnages/Character_hibou.asset");
            Near(owl.lanternColor, 0.36f, 0.44f, 0.58f, "lune froide et désaturée");
            var look = Load<HorrorLookSettings>("Assets/_Project/Settings/HorrorLook_Default.asset");
            Assert.That(look.lanternHalos, Is.False, "plus de boule orange autour de la lanterne des joueurs");
        }

        [Test]
        public void SonsDeLEpee_TroisVariantes_SansRepetition()
        {
            var audio = Load<GameAudioSettings>("Assets/_Project/Resources/GameAudio_Default.asset");
            Assert.That(audio.swordSwings.Length, Is.EqualTo(3));
            Assert.That(audio.swordHits.Length, Is.EqualTo(3));
            Assert.That(System.Array.TrueForAll(audio.swordSwings, c => c != null && c.name.StartsWith("Sfx_Epee_Vide_")), Is.True);
            Assert.That(System.Array.TrueForAll(audio.swordHits, c => c != null && c.name.StartsWith("Sfx_Epee_Impact_")), Is.True);
            Assert.That(audio.swordPitchVariation, Is.EqualTo(0.04f).Within(1e-4f), "hauteur ±4 %");

            for (int last = 0; last < 3; last++)
                for (float roll = 0f; roll < 1f; roll += 0.05f)
                {
                    int i = ItHunts.Audio.GameAudio.NoRepeatIndex(3, last, roll);
                    Assert.That(i, Is.InRange(0, 2));
                    Assert.That(i, Is.Not.EqualTo(last), "jamais la même variante deux fois de suite");
                }

            Assert.That(ItHunts.Audio.GameAudio.NoRepeatIndex(3, -1, 0.99f), Is.EqualTo(2), "premier tirage : les trois possibles");
            Assert.That(ItHunts.Audio.GameAudio.NoRepeatIndex(1, 0, 0.5f), Is.EqualTo(0), "une seule variante : elle se répète");
            Assert.That(ItHunts.Audio.GameAudio.NoRepeatIndex(0, -1, 0.5f), Is.EqualTo(-1));
        }

        [Test]
        public void Goule_SortDuTrou_DUnMetreJusquAuSol()
        {
            Assert.That(Ghoul.EmergeOffset(0f, 1f, 0.25f, 0.9f), Is.EqualTo(1f).Within(1e-4f), "sous le sol au début");
            Assert.That(Ghoul.EmergeOffset(0.25f, 1f, 0.25f, 0.9f), Is.EqualTo(1f).Within(1e-4f), "elle attend que le trou s'ouvre");
            float mid = Ghoul.EmergeOffset(0.575f, 1f, 0.25f, 0.9f);
            Assert.That(mid, Is.InRange(0.3f, 0.7f), "monte pendant la sortie");
            Assert.That(Ghoul.EmergeOffset(0.9f, 1f, 0.25f, 0.9f), Is.EqualTo(0f), "au sol à 0.9 s");
            Assert.That(Ghoul.EmergeOffset(2f, 1f, 0.25f, 0.9f), Is.EqualTo(0f));

            var door = new GameObject("Apparition (test)").AddComponent<SpawnPoint>();
            try
            {
                Assert.That(door.Effect, Is.EqualTo(SpawnEffect.Trou), "point posé à la main : trou au sol par défaut");
                door.Configure(SpawnKinds.Goule, SpawnEffect.Ombre);
                Assert.That(door.Effect, Is.EqualTo(SpawnEffect.Ombre));
                Assert.That(door.Accepts(SpawnKinds.Goule), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(door.gameObject);
            }
        }
    }
}
