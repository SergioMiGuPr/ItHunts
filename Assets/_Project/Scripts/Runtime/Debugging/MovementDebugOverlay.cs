using System.Text;
using ItHunts.Input;
using ItHunts.Movement;
using ItHunts.Orbs;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Debugging
{
    /// <summary>
    /// Affichage de debug (F3 au clavier, Select à la manette), en trois pages qu'on fait défiler (puis caché) :
    /// 1. mouvement (vitesses, état, pente, coyote, buffer, charges, dash, sauts) ;
    /// 2. orbe (commandes, derniers appuis, magnétisme, télékinésie) ;
    /// 3. combat et vue (ennemis : PV, recul, étourdi, immunité ; objet tenu ; geste des mains ; HUD discret).
    /// </summary>
    public sealed class MovementDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool visibleOnStart = false;

        private const int HistoryTicks = 60;

        private PlayerMotor _motor;
        private PlayerInputReader _reader;
        private PlayerLook _look;
        private DebugTeleporter _teleporter;
        private readonly float[] _speedHistory = new float[HistoryTicks];
        private int _historyIndex;
        private int _page;
        private const int PageCount = 3;
        private static readonly string[] PageNames = { "", "Mouvement", "Orbe", "Combat et vue" };
        private ItHunts.Player.FirstPersonView _view;
        private ItHunts.Hud.PlayerHud _hud;
        private int _seenJumpTick = MovementState.NeverTick;
        private float _speedAtLastJump;
        private float _lastJumpGain;
        private GUIStyle _style;
        private readonly StringBuilder _text = new StringBuilder(512);

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _reader = GetComponent<PlayerInputReader>();
            _look = GetComponent<PlayerLook>();
            _teleporter = GetComponent<DebugTeleporter>();
            _page = visibleOnStart ? 1 : 0;
            _view = GetComponent<ItHunts.Player.FirstPersonView>();
            _hud = GetComponent<ItHunts.Hud.PlayerHud>();
        }

        private void FixedUpdate()
        {
            if (_motor == null)
                return;

            MovementState s = _motor.State;
            float speed = s.HorizontalSpeed;
            _speedHistory[_historyIndex] = speed;
            _historyIndex = (_historyIndex + 1) % HistoryTicks;

            if (s.LastJumpTick != _seenJumpTick && s.LastJumpTick != MovementState.NeverTick)
            {
                _lastJumpGain = _seenJumpTick == MovementState.NeverTick ? 0f : speed - _speedAtLastJump;
                _speedAtLastJump = speed;
                _seenJumpTick = s.LastJumpTick;
            }
        }

        private void Update()
        {
            if (_reader != null && _reader.ToggleDebugPressed)
                _page = (_page + 1) % (PageCount + 1);       // 1, 2, 3, puis caché
        }

        private static readonly string[] DashSectors =
            { "avant", "avant-droite", "droite", "arrière-droite", "arrière", "arrière-gauche", "gauche", "avant-gauche" };

        /// <summary>Direction du dash par rapport à la caméra, en 8 secteurs.</summary>
        private static string DashLabel(Vector2 move)
        {
            if (move.sqrMagnitude < 0.01f)
                return "avant (sans entrée)";
            float angle = Mathf.Repeat(Mathf.Atan2(move.x, move.y) * Mathf.Rad2Deg, 360f);
            return DashSectors[Mathf.RoundToInt(angle / 45f) % 8];
        }

        private void AppendMagnetism(in MovementState s, float dt)
        {
            MagnetismOrbSettings mc = _motor.MagnetismSettings;
            if (mc == null)
                return;

            MagnetismState m = _motor.Magnetism;
            int now = s.Tick;   // (un paramètre « in » ne peut pas être capturé par la fonction locale)
            string Status(OrbStatus status, int lastTick, float cooldown) => status switch
            {
                OrbStatus.EnUsage => "EN USAGE",
                OrbStatus.Recharge => $"recharge {Mathf.Max(0f, cooldown - (now - lastTick) * dt):0.00} s",
                _ => "prête",
            };
            string aim = m.AimHit ? $"surface à {m.AimDistance:0.0} m" : $"rien à portée ({mc.attractRange:0} m)";
            string attract = m.AttractStatus == OrbStatus.EnUsage ? "EN USAGE" : m.AttractStatus == OrbStatus.Recharge ? "vide" : "prête";
            _text.AppendLine($"Magnétisme : attraction {attract}, charges {Mathf.Max(0f, m.AttractCharges):0.0}/{mc.attractCharges} " +
                             $"(1 toutes les {mc.attractChargeRechargeTime:0.#} s) ; répulsion " +
                             (m.RepelHolding ? $"MAINTENUE ({MagnetismOrb.RepelHoldRemaining(m, mc, now, dt):0.00} s restantes)"
                                 : m.RepelStatus == OrbStatus.Recharge ? $"recharge {MagnetismOrb.RepelCooldownRemaining(m, mc, now, dt):0.00} s"
                                 : m.RepelStatus == OrbStatus.EnUsage ? "EN USAGE" : "prête"));
            _text.AppendLine($"Visée : {aim}{(m.RepelInRange ? $" (répulsion possible, ≤ {mc.repelRange:0} m)" : m.AimHit ? $" (trop loin pour la répulsion, {mc.repelRange:0} m)" : "")}");

            if (s.Mode == MovementMode.Traction)
                _text.AppendLine($"Traction : {s.Velocity.magnitude:0.0} m/s, {Vector3.Distance(MagnetismOrb.EyePosition(s, _motor.Settings), s.TractionPoint):0.0} m " +
                                 $"du point, {(s.Tick - s.TractionStartTick) * dt:0.00} s");
            else if (m.LastTractionEnd != TractionEnd.Aucune)
                _text.AppendLine($"Dernière traction : {m.LastTractionStartSpeed:0.0} → max {m.LastTractionPeakSpeed:0.0} → fin {m.LastTractionEndSpeed:0.0} m/s, " +
                                 $"{m.LastTractionTicks * dt:0.00} s, fin : {EndLabel(m.LastTractionEnd)}");

            if (m.LastRepelTick != MovementState.NeverTick)
            {
                Vector3 b = m.LastRepelVelocityBefore, a = m.LastRepelVelocityAfter;
                _text.AppendLine($"Dernière répulsion : horizontale {new Vector2(b.x, b.z).magnitude:0.0} → {new Vector2(a.x, a.z).magnitude:0.0} m/s, " +
                                 $"verticale {b.y:0.0} → {a.y:0.0} m/s");
            }

            if (m.LastRefusalTick != MovementState.NeverTick && (s.Tick - m.LastRefusalTick) * dt < 2f)
                _text.AppendLine($"Refusé ({(m.LastRefusalMode == MagnetMode.Attraction ? "attraction" : "répulsion")}) : " +
                                 (m.LastRefusal switch
                                 {
                                     MagnetRefusal.PasDeCharge => "plus de charge d'attraction",
                                     MagnetRefusal.Recharge => "en recharge",
                                     MagnetRefusal.NonMagnetique => "surface non magnétique",
                                     _ => "rien à portée",
                                 }));
        }

        private void AppendTelekinesis(in MovementState s, float dt)
        {
            TelekinesisOrbSettings tc = _motor.TelekinesisSettings;
            if (tc == null)
                return;
            TelekinesisState t = _motor.Telekinesis;
            string status = t.Status switch
            {
                OrbStatus.EnUsage => "TIENT UN OBJET",
                OrbStatus.Recharge => "recharge",
                _ => "prête",
            };
            Grabbable aimed = t.AimHit ? Grabbable.Find(t.AimId) : null;
            string aim = aimed == null ? $"rien d'attrapable (≤ {tc.range:0} m)"
                : $"{aimed.name} ({aimed.Mass:0} kg{(t.AimTooHeavy ? ", TROP LOURD" : "")}) à {t.AimDistance:0.0} m" +
                  (t.AimAngle > 0f ? $", aide {t.AimAngle:0.0}° / cône {GrabAssist.ConeAngle(tc, t.AimDistance):0.0}°" : ", visée exacte");
            _text.AppendLine($"Télékinésie : {status}   Visée : {aim}");
            Grabbable held = _motor.Holder != null ? _motor.Holder.Held : null;
            if (held != null)
            {
                Vector3 hold = TelekinesisHolder.HoldPoint(tc, MagnetismOrb.EyePosition(s, _motor.Settings),
                    _look != null ? MagnetismOrb.AimDirection(_look.Yaw, _look.Pitch) : Vector3.forward);
                _text.AppendLine($"Tenu : {held.name} ({held.Mass:0} kg), écart au point de maintien {Vector3.Distance(hold, held.Body.position):0.00} m");
            }
            else if (t.LastAction == TelekinesisCommandKind.Lancer && _motor.Holder != null)
            {
                _text.AppendLine($"Dernier lancer : {_motor.Holder.LastThrowVelocity.magnitude:0.0} m/s");
            }

            if (t.LastRefusalTick != MovementState.NeverTick && (s.Tick - t.LastRefusalTick) * dt < 2f)
                _text.AppendLine("Refusé (télékinésie) : " + t.LastRefusal switch
                {
                    TelekinesisRefusal.RienAttrapable => "rien d'attrapable",
                    TelekinesisRefusal.TropLourd => "trop lourd",
                    TelekinesisRefusal.Recharge => "en recharge",
                    _ => "?",
                });
        }

        /// <summary>Temps depuis un appui (« — » si aucun).</summary>
        private static string Since(int pressTick, int tick, float dt) =>
            pressTick > 0 ? $"il y a {Mathf.Max(0, tick - pressTick) * dt:0.0} s" : "—";

        private static string EndLabel(TractionEnd end) => end switch
        {
            TractionEnd.Relache => "relâché",
            TractionEnd.Arrivee => "arrivé au point",
            TractionEnd.LigneCoupee => "ligne de vue coupée",
            TractionEnd.DureeMax => "durée max",
            TractionEnd.Saut => "saut",
            TractionEnd.Dash => "dash",
            TractionEnd.Repulsion => "répulsion",
            TractionEnd.Bloquee => "bloquée par un obstacle",
            _ => "—",
        };

        private void OnGUI()
        {
            ItHunts.Enemies.Stalker.ShowDebugMarker = _page == 3;       // point prédit du traqueur, page combat
            if (_page == 0 || _motor == null || _motor.Settings == null)
                return;

            _style ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(10, 10, 8, 8),
            };

            MovementState s = _motor.State;
            MovementSettings cfg = _motor.Settings;
            float dt = Time.fixedDeltaTime;
            int coyoteTicks = MovementSettings.SecondsToTicks(cfg.coyoteTime, dt);
            int bufferTicks = MovementSettings.SecondsToTicks(cfg.jumpBuffer, dt);

            int coyoteLeft = s.Grounded ? coyoteTicks
                : s.LastGroundedTick == MovementState.NeverTick ? 0
                : Mathf.Max(0, coyoteTicks - (s.Tick - s.LastGroundedTick));

            int bufferLeft = 0;
            if (_reader != null && _reader.JumpPressedTick > s.LastConsumedJumpPressTick)
                bufferLeft = Mathf.Max(0, bufferTicks - (s.Tick - _reader.JumpPressedTick));

            float peak = 0f;
            foreach (float v in _speedHistory)
                peak = Mathf.Max(peak, v);

            string scheme = _reader == null ? "?"
                : _reader.UsingGamepad ? "Manette"
                : _reader.ControlScheme == PlayerInputReader.KeyboardMouseScheme ? "Clavier-souris"
                : _reader.ControlScheme ?? "?";

            _text.Clear();
            _text.AppendLine($"Debug, page {_page}/{PageCount} : {PageNames[_page]}   (F3 / Select : page suivante, puis caché)");
            _text.AppendLine($"Contrôle : {scheme}");
            if (_page == 2)
                AppendOrbPage(s, dt);
            else if (_page == 3)
                AppendCombatPage(s, dt);
            else
                AppendMovementPage(s, cfg, dt, coyoteLeft, coyoteTicks, bufferLeft, bufferTicks, peak);
            if (_teleporter != null)
                _text.AppendLine(string.IsNullOrEmpty(_teleporter.LastLabel)
                    ? "Téléport : F4 départ, F5 ouvert, F6 ligne droite, F7 pentes, F8 murs, F9 écarts, F10 magnétisme, F11 grande salle, F12 télékinésie"
                    : $"Téléport : {_teleporter.LastLabel}   (F4 départ, F5 à F12 zones)");
            _text.Append($"Tick : {s.Tick} ({Mathf.RoundToInt(1f / dt)} Hz)");
            if (_look != null)
                _text.Append($"   FOV : {_look.CurrentFov:0.0}°");

            int index = _reader != null ? _reader.PlayerIndex : 0;
            var content = new GUIContent(_text.ToString());
            Vector2 size = _style.CalcSize(content);
            GUI.Box(new Rect(10f + index * (size.x + 10f), 10f, size.x, size.y), content, _style);
        }

        private void AppendMovementPage(in MovementState s, MovementSettings cfg, float dt, int coyoteLeft, int coyoteTicks,
            int bufferLeft, int bufferTicks, float peak)
        {
            _text.AppendLine($"Vitesse horizontale : {s.HorizontalSpeed:0.00} m/s   (pic sur 1 s : {peak:0.00})");
            _text.AppendLine($"Vitesse verticale : {s.Velocity.y:0.00} m/s");
            string mode = s.Mode switch
            {
                MovementMode.Ground => s.Crouched ? "Sol (accroupi)" : "Sol",
                MovementMode.Slide => "Glissade",
                MovementMode.WallSlide => "Wall slide",
                MovementMode.Traction => "Traction (magnétisme)",
                MovementMode.Mantle => "Hissage",
                _ => "Air",
            };
            int wallCoyoteTicks = MovementSettings.SecondsToTicks(cfg.wallCoyoteTime, dt);
            int wallLeft = s.LastWallTick == MovementState.NeverTick ? 0
                : Mathf.Max(0, wallCoyoteTicks - (s.Tick - s.LastWallTick));
            int lockLeft = Mathf.Max(0, s.SteerLockUntilTick - s.Tick);
            int boostTicks = MovementSettings.SecondsToTicks(cfg.slideBoostCooldown, dt);
            int boostLeft = s.LastSlideBoostTick == MovementState.NeverTick ? 0
                : Mathf.Max(0, boostTicks - (s.Tick - s.LastSlideBoostTick));
            _text.AppendLine($"État : {mode}   Au sol : {(s.Grounded ? "oui" : "non")}");
            _text.AppendLine($"Élan au sol : {s.HorizontalSpeed:0.0} m/s (course {cfg.runSpeed:0.0}, plafond {cfg.groundSpeedCap:0}) — " + s.Momentum switch
            {
                MomentumStatus.Garde => "GARDÉ",
                MomentumStatus.AvancerLache => "perdu : avancer lâché",
                MomentumStatus.Recule => "perdu : recul / freinage",
                MomentumStatus.HorsDuCone => $"perdu : entrée hors du cône ({cfg.momentumConeAngle:0}°)",
                MomentumStatus.Virage => $"cassé : virage au-delà de {cfg.momentumTurnAngle:0}°",
                MomentumStatus.Mur => "cassé : mur percuté",
                MomentumStatus.Plafond => "gardé, redescente vers le plafond",
                MomentumStatus.Accroupi => "perdu : accroupi",
                _ => "—",
            });
            _text.AppendLine(boostLeft > 0
                ? $"Bonus de glissade : recharge {boostLeft * dt:0.0} s"
                : "Bonus de glissade : prêt");
            _text.AppendLine($"Mur : {(s.OnWall ? "contact" : "—")}   Wall-coyote : {wallLeft}/{wallCoyoteTicks} ticks   " +
                             $"Verrou : {lockLeft} ticks   Même mur bloqué : {(s.LastWallJumpId != 0 && s.LastWallJumpId == s.LastWallId ? "oui" : "non")}");
            _text.AppendLine($"Pente : {Vector3.Angle(s.GroundNormal, Vector3.up):0.0}°");
            _text.AppendLine($"Coyote : {coyoteLeft}/{coyoteTicks} ticks   Buffer : {bufferLeft}/{bufferTicks} ticks");
            _text.AppendLine($"Charges de mouvement : {Mathf.Max(0f, s.Charges):0.0}/{cfg.movementCharges}   " +
                             $"(recharge {cfg.chargeRechargeTimeGround:0.#} s au sol, {cfg.chargeRechargeTimeAir:0.#} s en l'air)");
            if (s.LastDashTick != MovementState.NeverTick)
                _text.AppendLine($"Dernier dash : {DashLabel(s.LastDashMove)}, {s.LastDashStartSpeed:0.0} → {s.LastDashEndSpeed:0.0} m/s, " +
                                 $"{s.LastDashDistance:0.0} m{(s.DashTicksLeft > 0 ? " (en cours)" : "")}, charges restantes {Mathf.Max(0f, s.Charges):0.0}");
            if (s.LastDashJumpTick != MovementState.NeverTick)
                _text.AppendLine(s.LastDashJumpSuccess
                    ? $"Dash-jump : RÉUSSI  {s.LastDashJumpSpeedBefore:0.0} → {s.LastDashJumpSpeedAfter:0.0} m/s"
                    : $"Dash-jump : raté, trop tard de {s.LastDashJumpLateTicks * dt * 1000f:0} ms (fenêtre {cfg.dashJumpWindow * 1000f:0} ms)");
            AppendJumps(s, cfg);
        }

        private void AppendOrbPage(in MovementState s, float dt)
        {
            if (_reader != null)
            {
                _text.AppendLine(_reader.UsingGamepad
                    ? "Orbe : RB maintenu = traction ; LT = répulsion ; X = attraper / relâcher ; RT = lancer (sans objet : artefact, plus tard)"
                    : "Orbe : A maintenu = traction ; clic droit = répulsion ; F = attraper / relâcher ; clic gauche = lancer (sans objet : artefact, plus tard)");
                _text.AppendLine($"Derniers appuis : traction {Since(_reader.AttractPressedTick, s.Tick, dt)}{(_reader.AttractHeld ? " (tenu)" : "")}, " +
                                 $"répulsion {Since(_reader.RepelPressedTick, s.Tick, dt)}, attraper {Since(_reader.GrabPressedTick, s.Tick, dt)}, " +
                                 $"lancer {Since(_reader.ThrowPressedTick, s.Tick, dt)}");
            }
            AppendMagnetism(s, dt);
            AppendTelekinesis(s, dt);
        }

        private void AppendJumps(in MovementState s, MovementSettings cfg)
        {
            _text.AppendLine($"Sauts restants : {Mathf.Max(0, cfg.maxJumps - s.JumpsUsed)}/{cfg.maxJumps}   " +
                             $"Auto-bhop : {(cfg.autoBunnyHop ? "oui" : "non")}   Contrôle aérien : {cfg.airControl:0.00}");
            _text.AppendLine(_seenJumpTick == MovementState.NeverTick
                ? "Dernier saut : —"
                : $"Dernier saut : {_speedAtLastJump:0.00} m/s (gain {_lastJumpGain:+0.00;-0.00;0.00})");
        }

        private static float Barrel(in ItHunts.Artifacts.BarrelState b, ItHunts.Settings.BarrelSettings cfg, int tick, float dt) =>
            ItHunts.Artifacts.Barrel.ReloadProgress(b, tick, cfg, dt);

        /// <summary>Page 3 : ennemis (PV, recul, étourdi, immunité), objet tenu, geste des mains, HUD discret.</summary>
        private void AppendCombatPage(in MovementState s, float dt)
        {
            foreach (ItHunts.Combat.Damageable d in ItHunts.Combat.Damageable.All)
            {
                if (d == null)
                    continue;
                ItHunts.Combat.EnemyBody b = d.Body;
                string body = b == null ? "fixe"
                    : b.Stunned ? $"ÉTOURDI ({b.StunnedUntil - Time.time:0.0} s, impact {b.LastStunImpactSpeed:0.0} m/s)"
                    : b.StunImmune ? "immunisé"
                    : b.Knocked ? $"repoussé {b.Velocity.magnitude:0.0} m/s"
                    : "—";
                _text.AppendLine($"{d.name} : {(d.Dead ? "mort" : $"{d.Health.Current:0}/{d.Health.Max:0} PV")}, {body}, " +
                                 $"à {Vector3.Distance(transform.position, d.transform.position):0} m");
            }

            ItHunts.Orbs.Grabbable held = _motor.Holder != null ? _motor.Holder.Held : null;
            _text.AppendLine(held == null ? "Objet tenu : —"
                : $"Objet tenu : {held.name} ({held.Mass:0} kg), lancers restants {held.ThrowsLeft}/{held.MaxDurability}" +
                  $"{(held.BreaksOnAnyImpact ? ", fragile" : "")}");
            if (_motor.Holder != null && _motor.Telekinesis.LastAction == ItHunts.Orbs.TelekinesisCommandKind.Lancer)
                _text.AppendLine($"Dernier lancer : {_motor.Holder.LastThrowVelocity.magnitude:0.0} m/s " +
                                 $"(bonus de vitesse plafonné à {_motor.TelekinesisSettings.throwSpeedBonusCap:0} m/s)");
            ItHunts.Artifacts.ArtifactHolder art = _motor.Artifact;
            if (art != null && art.Current != null)
            {
                ItHunts.Artifacts.BarrelState br = art.Barrel;
                ItHunts.Settings.BarrelSettings bc = art.BarrelSettings;
                ItHunts.Artifacts.BarrelShot shot = art.LastShot;
                var run = FindFirstObjectByType<ItHunts.Artifacts.RunState>();
                _text.AppendLine($"Artefact : {art.Current.displayName}{(run != null ? $" (seed {run.Seed})" : "")}" +
                                 (bc == null ? "" : $", {br.Rounds}/{bc.rounds} coups" +
                                                    (br.Reloading ? $", RECHARGE {Barrel(br, bc, s.Tick, dt) * 100f:0} %" : "") +
                                                    (br.Inactive ? ", inactif (objet tenu)" : "") + $", tirs {br.ShotCount}"));
                ItHunts.Settings.FireLanternSettings lc = art.LanternSettings;
                if (lc != null)
                {
                    ItHunts.Artifacts.LanternState ls = art.Lantern;
                    float cd = ItHunts.Artifacts.FireLantern.CooldownProgress(ls, s.Tick, lc, dt);
                    _text.AppendLine($"Lanterne : {(ls.Jetting ? "JET" : cd >= 1f ? "prête" : $"recharge {cd * 100f:0} %")}" +
                                     $"{(ls.Inactive ? ", inactive (objet tenu)" : "")}, jets {ls.JetCount}, cibles du dernier jet " +
                                     $"{art.LastJetHits}, total {art.TotalJetHits}{(art.Character != null ? $" ; personnage {art.Character.displayName}" : "")}");
                }

                if (bc != null && br.ShotCount > 0)
                    _text.AppendLine($"Dernier tir : {shot.Kind}, {shot.Distance:0.0} m, {shot.Damage:0} dégâts" +
                                     $"{(shot.Collider != null ? $" ({shot.Collider.name})" : "")}");
            }

            foreach (ItHunts.Enemies.Stalker st in FindObjectsByType<ItHunts.Enemies.Stalker>(FindObjectsSortMode.None))
            {
                Vector3 pr = st.Predicted;
                var g = st.Graph;
                string node(int i) => g != null && i >= 0 ? g.NodeName(i) : "—";
                _text.AppendLine($"Traqueur : {st.Mode}, {st.Speed:0.0} m/s (voulue {st.WantedSpeed:0.0}), " +
                                 $"pointe {(st.BurstActive ? $"ACTIVE {st.BurstRemaining:0.0} s" : st.BurstCooldownRemaining > 0f ? $"recharge {st.BurstCooldownRemaining:0.0} s" : "prête")}, " +
                                 $"{(st.SeesTarget ? "te voit" : "ne te voit pas")}, à {Vector3.Distance(transform.position, st.transform.position):0} m, " +
                                 $"point prédit ({pr.x:0}, {pr.z:0}) à {Vector3.Distance(transform.position, pr):0.0} m de toi");
                _text.AppendLine($"  Poursuite : {(st.Direct ? "DROIT SUR TOI" : $"labyrinthe vers {node(st.GoalNode)}")}, dans {node(st.InNode)}, " +
                                 $"arrivé par {node(st.ArrivedFrom)}, demi-tours {st.UTurns}, {(st.Crouching ? $"BAISSÉ ({st.Crouch * st.Settings.height:0.0} m)" : "debout")}");
                _text.AppendLine($"  Main : {st.Grab}{(st.GrabThroughOpening ? " (par une ouverture)" : "")}{(st.GrabCooldownRemaining > 0f ? $", recharge {st.GrabCooldownRemaining:0.0} s" : "")}, " +
                                 $"épaule à {Vector3.Distance(st.Shoulder, transform.position):0.0} m (portée {st.Settings.GrabReach:0.0} m : bras {st.Settings.armLength:0.0} + fente {st.Settings.grabLunge:0.0}), " +
                                 $"{(st.TargetSheltered ? "HORS DE PORTÉE " : "")}prises {st.GrabAttempts} (dont {st.RoomGrabs} par une ouverture), tués {st.GrabKills} + {st.ContactKills} au contact");
                if (st.Mode == ItHunts.Enemies.StalkerMode.Enrage)
                    _text.AppendLine($"  Enragé : sur la même cible depuis {st.TimeOnTarget:0.0} s, palier {st.EnragedTier + 1}/{st.Settings.enragedRampSpeeds.Length} (base {st.EnragedBaseSpeed:0.0} m/s)");
                _text.AppendLine($"  Main plantée : {(st.Planted ? "OUI (passe maintenant)" : "non")}, prises ratées {st.MissedGrabs}");
                _text.AppendLine($"  Mains levées : {st.Overhead}{(st.OverheadCooldownRemaining > 0f ? $", recharge {st.OverheadCooldownRemaining:0.0} s" : "")}, " +
                                 $"gestes {st.OverheadAttempts}, tués {st.OverheadKills} ; morts au contact des bras ou des mains {st.LimbKills}");
            }

            // Goules : résumé (elles peuvent être nombreuses) et la plus proche.
            int alive = 0, onWall = 0, leaping = 0;
            ItHunts.Enemies.Ghoul nearest = null;
            float nearestD = float.PositiveInfinity;
            foreach (ItHunts.Enemies.Ghoul gh in ItHunts.Enemies.Ghoul.All)
            {
                var dmg = gh.GetComponent<ItHunts.Combat.Damageable>();
                if (dmg != null && dmg.Dead)
                    continue;
                alive++;
                if (gh.Mode == ItHunts.Enemies.GhoulMode.Mur)
                    onWall++;
                if (gh.Mode == ItHunts.Enemies.GhoulMode.Annonce || gh.Mode == ItHunts.Enemies.GhoulMode.Bond)
                    leaping++;
                float d = (gh.transform.position - transform.position).sqrMagnitude;
                if (d < nearestD)
                {
                    nearestD = d;
                    nearest = gh;
                }
            }

            if (alive > 0)
                _text.AppendLine($"Goules : {alive} en vie, {ItHunts.Enemies.Ghoul.HuntingCount} en chasse (chasseurs {ItHunts.Enemies.HuntDirector.ChaserCount} au total, {ItHunts.Enemies.HuntDirector.PerPlayer} par joueur ; en aggro {ItHunts.Enemies.HuntDirector.AggroCount}), {onWall} sur un mur, {leaping} en bond ; la plus proche à {Mathf.Sqrt(nearestD):0.0} m : " +
                                 $"{nearest.Mode}, {nearest.Speed:0.0} m/s, bond {(nearest.LeapCooldownRemaining > 0f ? $"dans {nearest.LeapCooldownRemaining:0.0} s" : "prêt")}");
            // Vagues (D-041) : phase, voulus / présents, dette, apparitions.
            var waves = FindFirstObjectByType<ItHunts.Enemies.WaveDirector>();
            if (waves != null)
                _text.AppendLine($"Vagues : {(waves.InPeak ? $"PIC {waves.PeakIndex}" : "accalmie")} ({waves.PhaseLeft:0} s) ; goules {waves.PresentGhouls}/{waves.WantedGhouls} " +
                                 $"(dernière vague {waves.LastGhoulWave}, reste {waves.GhoulWaveLeft}), volants {waves.PresentFlyers}/{waves.WantedFlyers} (vague {waves.LastFlyerWave}) ; " +
                                 $"en annonce {waves.PendingCount} ; apparitions {waves.Spawns}, tués {waves.Kills}, échecs {waves.SpawnFailures}, abandonnées {waves.Abandoned}");

            // Volants : résumé et le plus proche.
            int flyers = 0, diving = 0;
            ItHunts.Enemies.Flyer nearFlyer = null;
            float nearFlyerD = float.PositiveInfinity;
            foreach (ItHunts.Enemies.Flyer fl in ItHunts.Enemies.Flyer.All)
            {
                if (fl == null || fl.Dead)
                    continue;
                flyers++;
                if (fl.Mode == ItHunts.Enemies.FlyerMode.Annonce || fl.Mode == ItHunts.Enemies.FlyerMode.Pique)
                    diving++;
                float d = (fl.transform.position - transform.position).sqrMagnitude;
                if (d < nearFlyerD)
                {
                    nearFlyerD = d;
                    nearFlyer = fl;
                }
            }

            if (flyers > 0)
                _text.AppendLine($"Volants : {flyers} en vie, {ItHunts.Enemies.Flyer.HuntingCount} en chasse, " +
                                 $"{diving} en piqué ; le plus proche à {Mathf.Sqrt(nearFlyerD):0.0} m : {nearFlyer.Mode}, piqués {nearFlyer.Dives} (touchés {nearFlyer.DiveHits})");
            var zone = FindFirstObjectByType<ItHunts.Enemies.ZoneState>();
            if (zone != null)
                _text.AppendLine($"Zone : {zone.Elapsed:0} s écoulées, {(zone.InGrace ? $"grâce {zone.GraceLeft:0} s, " : "")}" +
                                 $"{(zone.Enraged ? "ENRAGÉ" : $"{zone.TimeLeft:0} s avant l'enragé")}, remises à zéro {zone.Resets - 1}");

            var hp = _motor.GetComponent<ItHunts.Player.PlayerHealth>();
            if (hp != null)
                _text.AppendLine($"PV du joueur : {hp.Health.Current:0}/{hp.Health.Max:0}, tir ami " +
                                 $"{(hp.Coop != null && hp.Coop.friendlyFire ? "ACTIVÉ" : "désactivé")}");
            if (_view != null)
                _text.AppendLine($"Mains : geste {_view.Gesture}, intensité du mouvement {_view.HandMotionIntensity * 100f:0} %, " +
                                 $"couche {(_view.Layer >= 0 ? LayerMask.LayerToName(_view.Layer) : "—")}");
            if (_hud != null)
            {
                Vector3 a = _hud.GaugeAlphas;
                _text.AppendLine($"HUD discret : mouvement {a.x * 100f:0} %, attraction {a.y * 100f:0} %, répulsion {a.z * 100f:0} %");
            }
        }
    }
}
