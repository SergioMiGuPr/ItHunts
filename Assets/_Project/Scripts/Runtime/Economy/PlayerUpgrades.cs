using ItHunts.Artifacts;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Economy
{
    /// <summary>
    /// Améliorations d'un joueur (étape 7, D-043), sur son prefab (ajoutée à la première amélioration) : cumule les effets et les
    /// applique à ses systèmes. Les réglages partagés (mouvement, magnétisme, barillet) sont copiés pour lui seul ; les dégâts des
    /// objets lancés et de la répulsion, la collecte des âmes et les PV passent par ses valeurs. La liste fait foi dans RunState.
    /// </summary>
    public sealed class PlayerUpgrades : MonoBehaviour
    {
        public int ExtraCharges { get; private set; }
        public float TractionRechargeCut { get; private set; }
        public float CollectRadiusBonus { get; private set; }
        public float ThrowDamageBonus { get; private set; }
        public float RepelDamage { get; private set; }
        public float MaxHealthBonus { get; private set; }
        public int ExtraRounds { get; private set; }
        public float PullSpeedBonus { get; private set; }
        public float BarrelDamageBonus { get; private set; }
        public float FireIntervalCut { get; private set; }
        public float ReloadCut { get; private set; }
        public float RangeBonus { get; private set; }
        public float LanternRangeBonus { get; private set; }
        public float LanternConeBonus { get; private set; }
        public float LanternCooldownCut { get; private set; }
        public float LanternBurnDps { get; private set; }

        private MovementSettings _moveOrig, _moveCopy;
        private MagnetismOrbSettings _magOrig, _magCopy;
        private ArtifactDefinition _artOrig, _artCopy;

        public static PlayerUpgrades Of(Component player)
        {
            if (player == null)
                return null;
            var u = player.GetComponentInParent<PlayerUpgrades>();
            if (u == null)
            {
                var h = player.GetComponentInParent<PlayerHealth>();
                if (h != null)
                    u = h.gameObject.AddComponent<PlayerUpgrades>();
            }

            return u;
        }

        /// <summary>Améliorations du joueur d'identifiant donné (null sans amélioration).</summary>
        public static PlayerUpgrades OfId(int playerId)
        {
            PlayerHealth p = PlayerHealth.Find(playerId);
            return p != null ? p.GetComponent<PlayerUpgrades>() : null;
        }

        /// <summary>Multiplicateur des dégâts des objets lancés ou repoussés par ce joueur.</summary>
        public static float ThrowMultiplier(int playerId)
        {
            PlayerUpgrades u = OfId(playerId);
            return u != null ? 1f + u.ThrowDamageBonus : 1f;
        }

        /// <summary>Ajoute l'effet d'une amélioration et l'applique.</summary>
        public void Apply(UpgradeDefinition d)
        {
            if (d == null)
                return;
            switch (d.kind)
            {
                case UpgradeKind.ChargeDeMouvement: ExtraCharges += Mathf.RoundToInt(d.amount); break;
                case UpgradeKind.RechargeTraction: TractionRechargeCut += d.amount; break;
                case UpgradeKind.RayonDeCollecte: CollectRadiusBonus += d.amount; break;
                case UpgradeKind.DegatsObjetsLances: ThrowDamageBonus += d.amount; break;
                case UpgradeKind.DegatsRepulsion: RepelDamage += d.amount; break;
                case UpgradeKind.PvMax: MaxHealthBonus += d.amount; break;
                case UpgradeKind.BallesBarillet: ExtraRounds += Mathf.RoundToInt(d.amount); break;
                case UpgradeKind.AspirationAmes: PullSpeedBonus += d.amount; break;
                case UpgradeKind.DegatsBarillet: BarrelDamageBonus += d.amount; break;
                case UpgradeKind.CadenceBarillet: FireIntervalCut += d.amount; break;
                case UpgradeKind.RechargementBarillet: ReloadCut += d.amount; break;
                case UpgradeKind.PorteeBarillet: RangeBonus += d.amount; break;
                case UpgradeKind.PorteeLanterne: LanternRangeBonus += d.amount; break;
                case UpgradeKind.ConeLanterne: LanternConeBonus += d.amount; break;
                case UpgradeKind.RechargeLanterne: LanternCooldownCut += d.amount; break;
                case UpgradeKind.BrulureLanterne: LanternBurnDps += d.amount; break;
            }

            Refresh(d.kind);
        }

        private void Refresh(UpgradeKind changed)
        {
            var motor = GetComponent<PlayerMotor>();
            if (motor != null && (changed == UpgradeKind.ChargeDeMouvement || changed == UpgradeKind.RechargeTraction))
            {
                if (_moveOrig == null)
                    _moveOrig = motor.Settings;
                if (_magOrig == null)
                    _magOrig = motor.MagnetismSettings;
                if (_moveOrig != null)
                {
                    if (_moveCopy != null)
                        Destroy(_moveCopy);
                    _moveCopy = Instantiate(_moveOrig);
                    _moveCopy.name = _moveOrig.name + " (améliorations)";
                    _moveCopy.movementCharges = _moveOrig.movementCharges + ExtraCharges;
                }

                if (_magOrig != null)
                {
                    if (_magCopy != null)
                        Destroy(_magCopy);
                    _magCopy = Instantiate(_magOrig);
                    _magCopy.name = _magOrig.name + " (améliorations)";
                    _magCopy.attractChargeRechargeTime = Mathf.Max(0.5f, _magOrig.attractChargeRechargeTime - TractionRechargeCut);
                }

                motor.UseSettings(_moveCopy, _magCopy);
            }

            if (changed == UpgradeKind.PvMax)
            {
                var hp = GetComponent<PlayerHealth>();
                if (hp != null)
                    hp.SetMaxHealthBonus(MaxHealthBonus);
            }

            if (changed == UpgradeKind.PorteeLanterne || changed == UpgradeKind.ConeLanterne || changed == UpgradeKind.RechargeLanterne
                || changed == UpgradeKind.BrulureLanterne)
                RefreshLantern();

            if (changed == UpgradeKind.BallesBarillet || changed == UpgradeKind.DegatsBarillet || changed == UpgradeKind.CadenceBarillet
                || changed == UpgradeKind.RechargementBarillet || changed == UpgradeKind.PorteeBarillet)
            {
                var holder = GetComponent<ArtifactHolder>();
                if (holder != null && holder.Current != null)
                {
                    if (_artOrig == null)
                        _artOrig = holder.Current;
                    if (_artOrig is BarrelSettings b)
                    {
                        Discard(_artCopy);
                        var copy = Instantiate(b);
                        copy.name = b.name + " (améliorations)";
                        copy.rounds = b.rounds + ExtraRounds;
                        copy.damage = b.damage * (1f + BarrelDamageBonus);
                        copy.fireInterval = Mathf.Max(0.05f, b.fireInterval * (1f - Mathf.Min(0.8f, FireIntervalCut)));
                        copy.reloadTime = Mathf.Max(0.2f, b.reloadTime * (1f - Mathf.Min(0.8f, ReloadCut)));
                        copy.range = b.range + RangeBonus;
                        _artCopy = copy;
                        holder.Equip(copy);
                    }
                }
            }
        }

        /// <summary>Lanterne de feu : copie de ses réglages pour ce joueur, avec ses améliorations (portée, cône, recharge, brûlure).</summary>
        private void RefreshLantern()
        {
            var holder = GetComponent<ArtifactHolder>();
            if (holder == null)
                return;
            holder.EnsureReady();
            if (_artOrig == null)
                _artOrig = holder.Current;
            if (!(_artOrig is FireLanternSettings f))
                return;
            Discard(_artCopy);
            var copy = Instantiate(f);
            copy.name = f.name + " (améliorations)";
            copy.range = f.range + LanternRangeBonus;
            copy.coneAngle = Mathf.Min(170f, f.coneAngle + LanternConeBonus);
            copy.cooldown = Mathf.Max(0.3f, f.cooldown - LanternCooldownCut);
            copy.burnDps = f.burnDps + LanternBurnDps;
            _artCopy = copy;
            holder.Equip(copy);
        }

        private static void Discard(Object o)
        {
            if (o == null)
                return;
            if (Application.isPlaying)
                Destroy(o);
            else
                DestroyImmediate(o);
        }

        private void OnDestroy()
        {
            foreach (Object o in new Object[] { _moveCopy, _magCopy, _artCopy })
                if (o != null)
                    Destroy(o);
        }
    }
}
