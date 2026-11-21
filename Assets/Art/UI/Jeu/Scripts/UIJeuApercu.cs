using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Scène d'aperçu Test_UI_Jeu (éditeur) : 1, 2, 3 = réticule neutre, attrapable, ennemi ; espace = dépense une charge
    /// (elles se rechargent l'une après l'autre) ; P = parade (recharge), O = contre réussi ; M = marchand, K = coffre,
    /// Échap = fermer le panneau.
    /// </summary>
    public sealed class UIJeuApercu : MonoBehaviour
    {
        public ReticuleJeu reticule;
        public ChargesJeu charges;
        public ParadeJeu parade;
        public PanneauBoutique marchand, coffre;
        public float dureeRechargeCharge = 1.2f;
        public float dureeRechargeParade = 3f;

        private float _charges = 3f, _parade = 1f;

        private void Start()
        {
            if (marchand != null) { marchand.gameObject.SetActive(false); marchand.Ferme += () => marchand.gameObject.SetActive(false); }
            if (coffre != null) { coffre.gameObject.SetActive(false); coffre.Ferme += () => coffre.gameObject.SetActive(false); }
        }

        private void Update()
        {
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.digit1Key.wasPressedThisFrame) reticule?.SetEtat(EtatReticule.Neutre);
                if (k.digit2Key.wasPressedThisFrame) reticule?.SetEtat(EtatReticule.Attrapable);
                if (k.digit3Key.wasPressedThisFrame) reticule?.SetEtat(EtatReticule.Ennemi);
                if (k.spaceKey.wasPressedThisFrame && _charges >= 1f) _charges -= 1f;
                if (k.pKey.wasPressedThisFrame && _parade >= 1f) _parade = 0f;
                if (k.oKey.wasPressedThisFrame) parade?.Contre();
                if (k.mKey.wasPressedThisFrame && marchand != null) marchand.gameObject.SetActive(!marchand.gameObject.activeSelf);
                if (k.kKey.wasPressedThisFrame && coffre != null) coffre.gameObject.SetActive(!coffre.gameObject.activeSelf);
                if (k.escapeKey.wasPressedThisFrame)
                {
                    if (marchand != null) marchand.gameObject.SetActive(false);
                    if (coffre != null) coffre.gameObject.SetActive(false);
                }
            }
            _charges = Mathf.Min(3f, _charges + Time.deltaTime / dureeRechargeCharge);
            _parade = Mathf.Min(1f, _parade + Time.deltaTime / dureeRechargeParade);
            if (charges != null)
                charges.RegleTout(Mathf.FloorToInt(_charges), _charges - Mathf.Floor(_charges));
            if (parade != null)
                parade.Regle(_parade);
        }
    }
}
