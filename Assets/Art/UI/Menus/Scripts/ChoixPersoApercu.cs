using System;
using System.Reflection;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Art.Menus
{
    /// <summary>
    /// Écran de choix du personnage (aperçu DA). Vignettes du haut (une seule illuminée, à la couleur du personnage), grand
    /// personnage à gauche, artefact flottant à droite (les quatre illustrés, animés par leurs shaders : UI-HANDOFF § 11),
    /// textes Unity entre les deux filets :
    /// nom du personnage, nom de l'artefact, description courte. Les noms sont lus dans les données du jeu
    /// (CharacterRoster / CharacterDefinition) ; la description dans un champ « description » de l'artefact s'il existe
    /// (sinon le texte d'aperçu ci-dessous). Gauche / droite (clavier, manette) ou clic sur une vignette.
    /// </summary>
    public sealed class ChoixPersoApercu : MonoBehaviour
    {
        [Serializable]
        public sealed class Entree
        {
            public string id;
            public Image vignetteDiscrete;
            public Image vignetteIlluminee;
            public GameObject grand;            // grand personnage (et sa lueur)
            public GameObject artefact;          // calques de l'artefact (Artefacts/Artefact_<Nom>), vide = « à venir »
            public string nomApercu;
            public string artefactApercu;
            [TextArea] public string descriptionApercu;
        }

        public CharacterRoster roster;
        public Entree[] entrees = Array.Empty<Entree>();
        public Text nom;
        public Text nomArtefact;
        public Text description;
        [Tooltip("Texte quand le personnage n'a pas encore d'artefact.")]
        public string aVenir = "à venir";
        public int depart;

        private int _courant = -1;

        private void OnEnable() => Choisit(Mathf.Clamp(depart, 0, Mathf.Max(entrees.Length - 1, 0)));

        private void Update()
        {
            int d = 0;
            var k = Keyboard.current;
            if (k != null && (k.leftArrowKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame)) d = -1;
            if (k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame)) d = 1;
            var g = Gamepad.current;
            if (g != null && g.dpad.left.wasPressedThisFrame) d = -1;
            if (g != null && g.dpad.right.wasPressedThisFrame) d = 1;
            if (d != 0 && entrees.Length > 0)
                Choisit((_courant + d + entrees.Length) % entrees.Length);
        }

        private CharacterDefinition Def(string id)
        {
            if (roster == null)
                return null;
            foreach (CharacterDefinition c in roster.characters)
                if (c != null && c.id == id)
                    return c;
            return null;
        }

        private static string Description(ArtifactDefinition a)
        {
            if (a == null)
                return null;
            // champ prévu côté main ; lu par réflexion pour ne pas dépendre d'une version précise des données
            FieldInfo f = a.GetType().GetField("description", BindingFlags.Public | BindingFlags.Instance);
            return f != null ? f.GetValue(a) as string : null;
        }

        /// <summary>Sélectionne l'entrée i (vignette illuminée, grand personnage, artefact, textes).</summary>
        public void Choisit(int i)
        {
            _courant = i;
            for (int j = 0; j < entrees.Length; j++)
            {
                Entree e = entrees[j];
                bool on = j == i;
                if (e.vignetteIlluminee != null) e.vignetteIlluminee.gameObject.SetActive(on);
                if (e.vignetteDiscrete != null) e.vignetteDiscrete.gameObject.SetActive(!on);
                if (e.grand != null) e.grand.SetActive(on);
                if (e.artefact != null) e.artefact.SetActive(on);
            }
            if (i < 0 || i >= entrees.Length)
                return;
            Entree en = entrees[i];
            CharacterDefinition def = Def(en.id);
            string n = def != null ? def.displayName : en.nomApercu;
            string an = def != null && def.artifact != null ? def.artifact.displayName : (en.artefact != null ? en.artefactApercu : aVenir);
            string desc = def != null ? Description(def.artifact) : null;
            if (string.IsNullOrEmpty(desc))
                desc = en.artefact != null ? en.descriptionApercu : "";
            if (nom != null) nom.text = n;
            if (nomArtefact != null) nomArtefact.text = an;
            if (description != null) description.text = desc;
        }

        /// <summary>Clic sur une vignette (branché par le constructeur de l'écran).</summary>
        public void ChoisitVignette(BaseEventData _)
        {
            GameObject g = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            for (int j = 0; j < entrees.Length; j++)
                if (g != null && ((entrees[j].vignetteDiscrete != null && g == entrees[j].vignetteDiscrete.gameObject) ||
                                  (entrees[j].vignetteIlluminee != null && g == entrees[j].vignetteIlluminee.gameObject)))
                    Choisit(j);
        }

        public void ChoisitIndex(int j) => Choisit(j);
    }
}
