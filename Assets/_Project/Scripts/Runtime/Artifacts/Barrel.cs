using ItHunts.Movement;
using ItHunts.Settings;

namespace ItHunts.Artifacts
{
    /// <summary>État du barillet d'un joueur : données seulement, copiables (réseau). Les délais sont en ticks.</summary>
    public struct BarrelState
    {
        public int Rounds;
        public bool Reloading;
        public int ReloadStartTick;
        public int LastShotTick;
        public int ShotCount;

        /// <summary>Barillet inactif : l'orbe tient un objet (il reste visible, mais ne tire ni ne recharge).</summary>
        public bool Inactive;

        public int LastConsumedFirePressTick;
        public int LastConsumedReloadPressTick;
        public int LastReloadDoneTick;
        public int LastReloadCancelTick;

        public static BarrelState Create(int rounds) => new BarrelState
        {
            Rounds = rounds,
            Reloading = false,
            ReloadStartTick = MovementState.NeverTick,
            LastShotTick = MovementState.NeverTick,
            LastConsumedFirePressTick = MovementState.NeverTick,
            LastConsumedReloadPressTick = MovementState.NeverTick,
            LastReloadDoneTick = MovementState.NeverTick,
            LastReloadCancelTick = MovementState.NeverTick,
        };
    }

    /// <summary>Ce qui s'est passé ce tick (le monde tire la balle, la vue joue le retour).</summary>
    public struct BarrelStep
    {
        public bool Fired;
        public bool ReloadStarted;
        public bool ReloadDone;
        public bool ReloadCancelled;
    }

    /// <summary>
    /// Le barillet, un tick à la fois (D-032), logique pure et testable. Appelé par PlayerMotor après la télékinésie, avec
    /// la même entrée : le clic gauche / RT sert au lancer si l'orbe tenait un objet au début du tick, sinon au tir ; tir
    /// et lancer ne partent jamais au même tick (le clic d'un lancer est consommé ici sans tirer).
    /// - Tir : une munition, au plus un tir par fireInterval ; un clic un peu trop tôt part dès que possible (pressBuffer).
    /// - Rechargement : automatique dès que le barillet est vide, ou sur R / Y s'il manque une munition ; pas de tir
    ///   pendant le rechargement.
    /// - Objet tenu : barillet inactif (ni tir, ni rechargement qui démarre) ; un rechargement en cours est annulé si
    ///   grabCancelsReload (sinon il se termine).
    /// </summary>
    public static class Barrel
    {
        public static BarrelStep Step(ref BarrelState b, int tick, int firePressTick, int reloadPressTick, bool heldAtTickStart,
            bool holdingNow, BarrelSettings cfg, float dt)
        {
            var step = new BarrelStep();
            int bufferTicks = MovementSettings.SecondsToTicks(cfg.pressBuffer, dt);
            int intervalTicks = MovementSettings.SecondsToTicks(cfg.fireInterval, dt);
            int reloadTicks = MovementSettings.SecondsToTicks(cfg.reloadTime, dt);
            bool fire = IsFresh(firePressTick, b.LastConsumedFirePressTick, tick, bufferTicks);
            bool reload = IsFresh(reloadPressTick, b.LastConsumedReloadPressTick, tick, bufferTicks);

            // Le clic appartenait à la télékinésie (lancer), ou l'orbe tient un objet : consommé sans tirer.
            if (fire && (heldAtTickStart || holdingNow))
            {
                b.LastConsumedFirePressTick = firePressTick;
                fire = false;
            }

            b.Inactive = holdingNow;
            if (holdingNow && b.Reloading && cfg.grabCancelsReload)
            {
                b.Reloading = false;
                b.LastReloadCancelTick = tick;
                step.ReloadCancelled = true;
            }

            if (b.Reloading && tick - b.ReloadStartTick >= reloadTicks)
            {
                b.Reloading = false;
                b.Rounds = cfg.rounds;
                b.LastReloadDoneTick = tick;
                step.ReloadDone = true;
            }

            if (reload)
            {
                b.LastConsumedReloadPressTick = reloadPressTick;
                if (!holdingNow && !b.Reloading && b.Rounds < cfg.rounds)
                    StartReload(ref b, tick, ref step);
            }

            if (holdingNow)
                return step;

            if (b.Rounds <= 0 && !b.Reloading)
                StartReload(ref b, tick, ref step);

            if (fire)
            {
                bool ready = !b.Reloading && b.Rounds > 0
                             && (b.LastShotTick == MovementState.NeverTick || tick - b.LastShotTick >= intervalTicks);
                if (ready)
                {
                    b.LastConsumedFirePressTick = firePressTick;
                    b.Rounds--;
                    b.LastShotTick = tick;
                    b.ShotCount++;
                    step.Fired = true;
                    if (b.Rounds <= 0)
                        StartReload(ref b, tick, ref step);
                }
                else if (tick - firePressTick >= bufferTicks)
                {
                    b.LastConsumedFirePressTick = firePressTick;     // trop tôt et plus mémorisé : perdu, sans refus
                }
            }

            return step;
        }

        /// <summary>
        /// Éventail (C7, D-100) : le clic tenu vide le barillet en rafale, à rateFactor × la cadence. Vrai si une balle part ce
        /// tick (une munition de moins) ; faux pendant un rechargement, un objet tenu ou avant l'intervalle.
        /// </summary>
        public static bool TryBurstFire(ref BarrelState b, int tick, bool held, bool holdingNow, float rateFactor, BarrelSettings cfg, float dt)
        {
            if (!held || holdingNow || b.Reloading || b.Rounds <= 0 || rateFactor <= 0f)
                return false;
            int interval = System.Math.Max(1, MovementSettings.SecondsToTicks(cfg.fireInterval / rateFactor, dt));
            if (b.LastShotTick != MovementState.NeverTick && tick - b.LastShotTick < interval)
                return false;
            if (b.LastShotTick == tick)
                return false;
            b.Rounds--;
            b.LastShotTick = tick;
            b.ShotCount++;
            return true;
        }

        /// <summary>Rechargement instantané (Sixième chambre Mythique, Main leste Mythique).</summary>
        public static void InstantReload(ref BarrelState b, int tick, BarrelSettings cfg)
        {
            b.Reloading = false;
            b.Rounds = cfg.rounds;
            b.LastReloadDoneTick = tick;
        }

        /// <summary>Avancement du rechargement (0..1) ; 0 s'il n'y en a pas.</summary>
        public static float ReloadProgress(in BarrelState b, int tick, BarrelSettings cfg, float dt)
        {
            if (!b.Reloading)
                return 0f;
            int reloadTicks = MovementSettings.SecondsToTicks(cfg.reloadTime, dt);
            return reloadTicks <= 0 ? 1f : UnityEngine.Mathf.Clamp01((tick - b.ReloadStartTick) / (float)reloadTicks);
        }

        private static void StartReload(ref BarrelState b, int tick, ref BarrelStep step)
        {
            b.Reloading = true;
            b.ReloadStartTick = tick;
            step.ReloadStarted = true;
        }

        private static bool IsFresh(int pressTick, int consumedTick, int tick, int bufferTicks)
        {
            int since = tick - pressTick;
            return pressTick > 0 && pressTick > consumedTick && since >= 0 && since <= bufferTicks;
        }
    }
}
