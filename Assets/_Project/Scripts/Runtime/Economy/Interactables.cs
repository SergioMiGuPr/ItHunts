using System.Collections.Generic;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Economy
{
    /// <summary>Ce qu'on peut utiliser avec E / Y (coffre, orbe de choix ou d'offre, porte verrouillée) : D-043.</summary>
    public interface IInteractable
    {
        /// <summary>Point visé (centre de l'objet).</summary>
        Vector3 Point { get; }
        /// <summary>Portée (m) ; vrai pour les orbes (F marche aussi).</summary>
        float Range { get; }
        bool AcceptsGrab { get; }
        /// <summary>Utilisable par ce joueur maintenant ?</summary>
        bool Available(PlayerHealth p);
        /// <summary>Texte de l'aide (sans la touche) : « ouvrir le coffre (30 âmes) ».</summary>
        string Prompt(PlayerHealth p);
        /// <summary>Utilisé par ce joueur (paiement, refus en rouge s'il manque des âmes).</summary>
        void Interact(PlayerHealth p);
    }

    /// <summary>Registre des objets utilisables de la scène.</summary>
    public static class Interactables
    {
        public static readonly List<IInteractable> All = new List<IInteractable>();

        /// <summary>Joueur dont c'est la caméra du monde (PlayerInput.camera ; écran partagé compris), sinon null.</summary>
        public static PlayerHealth PlayerOfCamera(Camera cam)
        {
            if (cam == null)
                return null;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null)
                    continue;
                var input = p.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                if (input != null && input.camera == cam)
                    return p;
            }

            return cam.GetComponentInParent<PlayerHealth>();
        }

        /// <summary>Détruit un objet (orbe prise, collider) : en jeu à la fin de l'image, hors jeu (tests EditMode) tout de suite.</summary>
        public static void Discard(Object o)
        {
            if (o == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(o);
            else
                Object.DestroyImmediate(o);
        }

        /// <summary>Caméra du monde d'un joueur.</summary>
        public static Camera CameraOf(PlayerHealth p)
        {
            var input = p != null ? p.GetComponent<UnityEngine.InputSystem.PlayerInput>() : null;
            return input != null ? input.camera : null;
        }
    }
}
