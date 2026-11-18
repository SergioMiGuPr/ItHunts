using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Art.Menus
{
    /// <summary>
    /// Scène d'aperçu des menus (DA) : F1 titre, F2 campement, F3 choix, Échap pause (et Paramètres depuis la pause),
    /// F5 gèle / dégèle les effets. Musique du menu en boucle, feu du campement en boucle sur l'écran du campement.
    /// </summary>
    public sealed class MenusApercu : MonoBehaviour
    {
        public GameObject titre, campement, choix, pause, parametres;
        public AudioSource musique, feu;
        [Range(0, 1)] public float volumeFeu = 1f;
        public Selectable premierPause, premierParametres;

        private GameObject _ecran;

        private void Start() => Montre(titre);

        public void Montre(GameObject ecran)
        {
            foreach (GameObject g in new[] { titre, campement, choix })
                if (g != null) g.SetActive(g == ecran);
            if (pause != null) pause.SetActive(false);
            if (parametres != null) parametres.SetActive(false);
            _ecran = ecran;
            if (feu != null)
            {
                bool f = ecran == campement;
                if (f && !feu.isPlaying) feu.Play();
                if (!f) feu.Stop();
                feu.volume = volumeFeu;
            }
        }

        public void Pause(bool on)
        {
            if (pause == null) return;
            pause.SetActive(on);
            if (parametres != null) parametres.SetActive(false);
            if (on && premierPause != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(premierPause.gameObject);
        }

        public void Parametres(bool on)
        {
            if (parametres == null) return;
            parametres.SetActive(on);
            if (pause != null) pause.SetActive(!on && pause.activeSelf);
            if (on && premierParametres != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(premierParametres.gameObject);
        }

        private void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.f1Key.wasPressedThisFrame) Montre(titre);
            if (k.f2Key.wasPressedThisFrame) Montre(campement);
            if (k.f3Key.wasPressedThisFrame) Montre(choix);
            if (k.f5Key.wasPressedThisFrame) MenuFige.Regle(Shader.GetGlobalFloat("_ItHuntsMenuFige") < 0.5f);
            if (k.escapeKey.wasPressedThisFrame)
            {
                if (parametres != null && parametres.activeSelf) { parametres.SetActive(false); Pause(true); }
                else Pause(pause != null && !pause.activeSelf);
            }
        }
    }
}
