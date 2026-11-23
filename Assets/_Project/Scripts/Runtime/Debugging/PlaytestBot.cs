using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItHunts.Artifacts;
using ItHunts.Combat;
using ItHunts.Economy;
using ItHunts.Enemies;
using ItHunts.Input;
using ItHunts.Level;
using ItHunts.Level.Acts;
using ItHunts.Movement;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.AI;

namespace ItHunts.Debugging
{
    /// <summary>
    /// Bot de playtest (nuit 4, vérification) : joue un acte du début à la fin à la place du joueur, par les mêmes entrées que lui
    /// (PlayerInputReader.Override, vue par PlayerLook.SetView). Il visite toutes les salles (chemins sur le NavMesh des goules, saut quand
    /// la marche est haute, dash et traction quand il n'avance plus), ouvre les coffres et achète au marchand quand il a les âmes, tire
    /// sur les goules à vue, pare un coup annoncé, fuit le traqueur en chasse, paie la porte de sortie dès qu'elle est ouverte et entre
    /// dans le hall ; au boss, il tire, esquive les annonces et vise les points faibles. Il note tout ce qui ressemble à un défaut du
    /// jeu : blocage (aucun progrès), chute hors de la carte, salle inaccessible, exception du journal, achat impossible, et les mesures
    /// (durée réelle de l'acte, âmes gagnées, images par seconde). Outil de mesure : jamais en partie.
    /// </summary>
    public sealed class PlaytestBot : MonoBehaviour
    {
        public enum Task : byte { Explorer, Coffre, Marchand, Fuir, Sortie, Hall, Boss, Attente, Fini }

        [Tooltip("Invulnérable : les coups sont comptés, pas subis (le bot teste la carte, pas sa propre adresse).")]
        public bool invulnerable = true;
        [Tooltip("Vitesse de marche voulue (0..1) ; 1 : course.")]
        [Range(0.2f, 1f)] public float moveScale = 1f;
        [Tooltip("Rayon où une goule est prise pour cible (m).")]
        public float fightRange = 16f;
        [Tooltip("Le traqueur en chasse à moins de cette distance : fuite.")]
        public float fleeDistance = 30f;
        public float fleeUntil = 48f;

        public BotReport Report { get; private set; } = new BotReport();
        public Task Current { get; private set; } = Task.Explorer;
        public string Status { get; private set; } = "";
        public bool Done => Current == Task.Fini;

        private PlayerHealth _health;
        private PlayerMotor _motor;
        private PlayerLook _look;
        private PlayerInputReader _reader;
        private PlayerInteractor _interactor;
        private ZoneState _zone;
        private ZoneNavMesh _nav;
        private ActLevel _level;
        private Stalker _stalker;

        private NavMeshPath _path;                                     // créé dans Awake (interdit dans un initialiseur de champ)
        private readonly List<Vector3> _corners = new List<Vector3>();
        private int _corner;
        private Vector3 _goal;
        private bool _hasGoal;
        private float _goalSet, _lastProgress, _bestDistance = float.PositiveInfinity;
        private float _stuckSince = float.NegativeInfinity;
        private int _tractionTries;

        private readonly HashSet<int> _visited = new HashSet<int>();
        private readonly HashSet<Chest> _chestsDone = new HashSet<Chest>();
        private int _targetRoom = -1;
        private Chest _targetChest;
        private IInteractable _targetMerchant;
        private bool _merchantDone;
        private float _merchantAgainAt;
        private float _nextShot, _nextMenuAction, _lastYawChange;
        private readonly Dictionary<Ghoul, float> _signals = new Dictionary<Ghoul, float>();
        private readonly HashSet<Ghoul> _parriedSignal = new HashSet<Ghoul>();
        private PlayerInputReader.Frame _frame;
        private float _pitch;
        private float _startTime, _lastFpsSample;
        private int _frames;
        private float _frameTimeSum;
        private float _lowestFloor;
        private float _outsideSince = float.NegativeInfinity;
        private int _hallEnteredFrame;
        private float _fleeStarted;
        private float _attractHoldUntil;
        private Vector3 _attractAim;
        private float _dodgeUntil;
        private Vector3 _dodgeDir;
        private float _bossStrafe = 1f, _bossStrafeChange;
        private bool _bossOnPillar;
        private float _pillarUntil;

        /// <summary>Pose le bot sur ce joueur (remplace ses entrées).</summary>
        public static PlaytestBot Attach(PlayerHealth p, BotReport report = null)
        {
            if (p == null)
                return null;
            PlaytestBot b = p.GetComponent<PlaytestBot>();
            if (b == null)
                b = p.gameObject.AddComponent<PlaytestBot>();
            if (report != null)
                b.Report = report;
            return b;
        }

        private void Awake()
        {
            _path = new NavMeshPath();
            _health = GetComponent<PlayerHealth>();
            _motor = GetComponent<PlayerMotor>();
            _look = GetComponent<PlayerLook>();
            _reader = GetComponent<PlayerInputReader>();
            _interactor = GetComponent<PlayerInteractor>();
        }

        private void OnEnable()
        {
            if (_reader != null)
                _reader.Override = Fill;
            PlayerHealth.Hurt += OnHurt;
            PlayerHealth.Died += OnDied;
            ParryEvents.Parried += OnParry;
            Application.logMessageReceived += OnLog;
            _startTime = Time.time;
            _lastFpsSample = Time.realtimeSinceStartup;
            if (invulnerable)
                PlayerHealth.DebugInvulnerable = true;
            Report.Begin(RunSession.Act);
        }

        private void OnDisable()
        {
            if (_reader != null && _reader.Override == (System.Func<PlayerInputReader, PlayerInputReader.Frame>)Fill)
                _reader.Override = null;
            PlayerHealth.Hurt -= OnHurt;
            PlayerHealth.Died -= OnDied;
            ParryEvents.Parried -= OnParry;
            Application.logMessageReceived -= OnLog;
        }

        private void OnHurt(PlayerHurtEvent e)
        {
            if (e.Victim == _health)
                Report.Hits++;
        }

        private void OnDied(PlayerHealth p)
        {
            if (p == _health)
                Report.Note("mort", $"{p.LastCause} à {Clock()}");
        }

        private void OnParry(ParryEvent e)
        {
            if (e.Player == transform)
                Report.ParriesSeen++;
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                Report.Error(message.Split('\n')[0], stack);
        }

        private string Clock()
        {
            float t = _zone != null ? _zone.Elapsed : Time.time - _startTime;
            return $"{Mathf.FloorToInt(t / 60f)}:{Mathf.FloorToInt(t % 60f):00}";
        }

        private PlayerInputReader.Frame Fill(PlayerInputReader r) => _frame;

        private void Update()
        {
            _frames++;
            _frameTimeSum += Time.unscaledDeltaTime;
            if (Time.realtimeSinceStartup - _lastFpsSample > 2f)
            {
                Report.FrameSamples.Add(_frames / _frameTimeSum);
                _frames = 0;
                _frameTimeSum = 0f;
                _lastFpsSample = Time.realtimeSinceStartup;
            }

            var f = new PlayerInputReader.Frame();
            if (_health == null || _health.Dead || Current == Task.Fini)
            {
                _frame = f;
                return;
            }

            if (_zone == null)
                _zone = FindFirstObjectByType<ZoneState>();
            if (_nav == null)
                _nav = FindFirstObjectByType<ZoneNavMesh>();
            if (_level == null)
                _level = ActLevel.Current;
            if (_stalker == null)
                _stalker = FindFirstObjectByType<Stalker>();
            if (_level != null && _level.Loading)
            {
                _frame = f;
                return;
            }

            if (_lowestFloor == 0f && _level != null && _level.Plan != null && _level.Plan.Rooms.Count > 0)
                _lowestFloor = _level.Plan.Rooms.Min(x => x.Floor);

            Vector3 pos = transform.position;
            CheckOutside(pos);
            Report.MaxSpeed = Mathf.Max(Report.MaxSpeed, new Vector2(_motor.State.Velocity.x, _motor.State.Velocity.z).magnitude);

            // Menu ouvert (coffre, marchand) : acheter ce qu'on peut, puis fermer.
            ShopMenu menu = ShopMenu.Of(_health);
            if (menu != null && menu.IsOpen)
            {
                HandleMenu(menu);
                _frame = f;
                return;
            }

            // Écran de fin : la run est finie.
            if (RunEndScreen.Current != null)
            {
                Report.Note("fin", (RunEndScreen.Current.Victory ? "victoire" : "mort") + " à " + Clock());
                Current = Task.Fini;
                _frame = f;
                return;
            }

            if (_zone != null && _zone.Finished)
            {
                if (Current != Task.Fini && Current != Task.Attente)
                {
                    Report.ActSeconds = _zone.Elapsed;
                    Report.Note("acte", $"terminé à {Clock()} (temps de jeu)");
                    Current = Task.Attente;
                }

                _frame = f;
                return;
            }

            try
            {
                Decide(pos);
                Act(pos, ref f);
            }
            catch (System.Exception e)
            {
                // Une exception du bot ne doit pas spammer le journal du jeu : notée une fois (avec sa pile), le bot continue.
                _botErrors ??= new HashSet<string>();
                Report ??= new BotReport();
                string key = e.Message + e.StackTrace;
                if (_botErrors.Add(key))
                {
                    string nulls = $"health={(_health == null)} motor={(_motor == null)} look={(_look == null)} reader={(_reader == null)} interactor={(_interactor == null)} level={(_level == null)} zone={(_zone == null)} nav={(_nav == null)} chests={(_chestsDone == null)} signals={(_signals == null)}";
                    Report.Fail("bot", $"exception du bot : {e.GetType().Name} : {e.Message} | {e.StackTrace} | nuls : {nulls}");
                    Debug.LogWarning("Bot : " + e + " | " + nulls);
                }

                Current = Task.Explorer;
                _hasGoal = false;
            }

            _frame = f;
        }

        private HashSet<string> _botErrors = new HashSet<string>();

        // ---------- Décision ----------

        private void Decide(Vector3 pos)
        {
            if (_level == null || _level.Plan == null)
                return;
            ActPlan p = _level.Plan;
            FinalBoss boss = FinalBoss.Current_;
            if (boss != null && boss.Started && !boss.Dead)
            {
                if (Current != Task.Boss)
                    Report.Note("boss", "combat commencé à " + Clock());
                Current = Task.Boss;
                return;
            }

            // Sortie payable : elle passe avant la fuite tant que le traqueur n'est pas à moins de 10 m (un joueur file vers la porte).
            ExitDoor exitFirst = _level.ExitDoorObject;
            int soulsNow = RunState.Current != null ? RunState.Current.Souls(_health.PlayerId) : 0;
            bool exitReady = exitFirst != null && _zone != null && _zone.ExitUnlocked && (exitFirst.Open || soulsNow >= exitFirst.Price);
            bool stalkerClose = _stalker != null && Vector3.Distance(_stalker.transform.position, pos) < 10f;
            if (exitReady && !stalkerClose && Current == Task.Fuir)
            {
                Current = Task.Explorer;
                _hasGoal = false;
            }

            // Fuite : traqueur en chasse, proche et qui nous voit.
            if (!(exitReady && !stalkerClose) && _stalker != null && (_stalker.Mode == StalkerMode.Chasse || _stalker.Mode == StalkerMode.Enrage))
            {
                float d = Vector3.Distance(_stalker.transform.position, pos);
                if (Current != Task.Fuir && d < fleeDistance && _stalker.Target == _health)
                {
                    Current = Task.Fuir;
                    _fleeStarted = Time.time;
                    Report.Flees++;
                    SetGoal(FleePoint(pos), "fuite");
                    return;
                }

                if (Current == Task.Fuir)
                {
                    if (d > fleeUntil || Time.time - _fleeStarted > 25f)
                    {
                        Current = Task.Explorer;
                        _hasGoal = false;
                    }
                    else if (!_hasGoal || Vector3.Distance(pos, _goal) < 4f)
                        SetGoal(FleePoint(pos), "fuite");
                    return;
                }
            }
            else if (Current == Task.Fuir)
            {
                Current = Task.Explorer;
                _hasGoal = false;
            }

            if (Current == Task.Hall)
                return;

            // Sortie ouverte et payable : on y va.
            ExitDoor exit = _level.ExitDoorObject;
            int souls = RunState.Current != null ? RunState.Current.Souls(_health.PlayerId) : 0;
            Report.Souls = souls;
            if (exit != null && _zone != null && _zone.ExitUnlocked)
            {
                if (exit.Open)
                {
                    if (Current != Task.Hall)
                    {
                        ActRoom hall = p.Rooms[p.ExitHall];
                        Vector3 toHall = p.CellCenter(hall.CenterCell, hall.Floor) - exit.transform.position;
                        toHall.y = 0f;
                        SetGoal(p.CellCenter(hall.CenterCell, hall.Floor) + toHall.normalized * 2f, "hall de sortie");
                        Current = Task.Hall;
                        _hallEnteredFrame = Time.frameCount;
                    }

                    return;
                }

                if (souls >= exit.Price)
                {
                    if (Current != Task.Sortie)
                    {
                        SetGoal(_level.ExitPoint, "porte de sortie");
                        Current = Task.Sortie;
                        Report.Note("sortie", $"payable, {souls} âmes à {Clock()}");
                    }

                    return;
                }

                if (Current == Task.Sortie)
                    Current = Task.Explorer;
                // Pas assez d'âmes et toutes les salles vues : on chasse autour, et au-delà de 10 min on note le manque.
                if (_zone.Elapsed > 600f && !Report.SoulsShort)
                {
                    Report.SoulsShort = true;
                    Report.Note("économie", $"âmes insuffisantes pour la sortie à 10:00 : {souls} / {exit.Price} ; ajoutées par l'admin pour continuer");
                    RunState.Current?.AddSouls(_health.PlayerId, exit.Price - souls);
                }
            }

            // Coffre proche payable.
            if (Current == Task.Explorer || Current == Task.Coffre)
            {
                if (_targetChest != null && (_chestsDone.Contains(_targetChest) || _targetChest.OpenedFor(_health.PlayerId)))
                {
                    _targetChest = null;
                    Current = Task.Explorer;
                    _hasGoal = false;
                }

                if (_targetChest == null)
                {
                    Chest best = null;
                    float bestD = 140f;
                    foreach (Chest c in Chest.AllChests)
                    {
                        if (c == null || !c.isActiveAndEnabled || _chestsDone.Contains(c) || c.OpenedFor(_health.PlayerId))
                            continue;
                        int price;
                        try
                        {
                            price = c.PriceFor(_health);
                        }
                        catch (System.Exception e)
                        {
                            if (_botErrors.Add("prix " + c.name))
                                Report.Error($"prix du coffre « {c.name} » : {e.GetType().Name} {e.Message}", e.StackTrace);
                            continue;
                        }

                        if (price > souls || c.GetComponentInParent<LockedDoor>() != null)
                            continue;
                        int cr = _level.Plan.RoomAt(c.transform.position);
                        if (cr >= 0 && _level.Plan.Rooms[cr].Role == RoomRole.Verrouillee)
                            continue;                                  // derrière une porte payante : pas pour l'exploration
                        float d = Vector3.Distance(c.transform.position, pos);
                        if (d < bestD && Reachable(pos, c.transform.position))
                        {
                            bestD = d;
                            best = c;
                        }
                    }

                    if (best != null)
                    {
                        _targetChest = best;
                        Current = Task.Coffre;
                        SetGoal(best.transform.position, "coffre");
                        return;
                    }
                }
                else if (Current == Task.Coffre)
                    return;
            }

            // Marchand : une visite par acte, quand on a de quoi acheter (au moins 40 âmes).
            if ((Current == Task.Explorer || Current == Task.Marchand) && Time.time > _merchantAgainAt && souls >= 60)
            {
                if (_targetMerchant == null)
                {
                    IInteractable best = null;
                    float bestD = 260f;
                    foreach (IInteractable i in Interactables.All)
                    {
                        if (!(i is ArtifactMerchant) && !(i is Merchant))
                            continue;
                        float d = Vector3.Distance(i.Point, pos);
                        if (d < bestD && Reachable(pos, i.Point))
                        {
                            bestD = d;
                            best = i;
                        }
                    }

                    if (best != null)
                    {
                        _targetMerchant = best;
                        Current = Task.Marchand;
                        SetGoal(((Component)best).transform.position, "marchand");
                        return;
                    }
                }
                else
                    return;
            }

            // Exploration : la salle non visitée la plus proche (chemin), sinon la plus proche en ligne droite ; toutes vues : patrouille.
            if (Current == Task.Explorer && (!_hasGoal || _targetRoom < 0 || Vector3.Distance(pos, _goal) < 3f))
            {
                int here = p.RoomAt(pos);
                if (here >= 0)
                    _visited.Add(here);
                int next = -1;
                float bestD = float.PositiveInfinity;
                foreach (ActRoom r in p.Rooms)
                {
                    if (_visited.Contains(r.Id) || r.Special)
                        continue;
                    Vector3 c = RoomPoint(r);
                    float d = PathLength(pos, c);
                    if (d < bestD)
                    {
                        bestD = d;
                        next = r.Id;
                    }
                }

                if (next < 0)
                {
                    if (!Report.AllRoomsVisited)
                    {
                        Report.AllRoomsVisited = true;
                        Report.Note("exploration", $"toutes les salles visitées à {Clock()} ({_visited.Count} / {p.Rooms.Count})");
                    }

                    // Patrouille : la salle la plus éloignée du traqueur, pour tuer des goules en attendant la sortie.
                    ActRoom far = p.Rooms.Where(r => !r.Special).OrderBy(_ => Random.value).First();
                    next = far.Id;
                }

                _targetRoom = next;
                SetGoal(RoomPoint(p.Rooms[next]), "salle " + p.Rooms[next].Name);
            }
        }

        private Vector3 RoomPoint(ActRoom r)
        {
            Vector3 c = _level.Plan.CellCenter(r.CenterCell, r.Floor);
            if (r.Use.TryGetValue(Vector2Int.RoundToInt(r.CenterCell), out CellUse u) && u == CellUse.Terrasse)
                c.y += r.TerraceHeight;
            return c;
        }

        private Vector3 FleePoint(Vector3 pos)
        {
            ActPlan p = _level.Plan;
            Vector3 s = _stalker.transform.position;
            ActRoom best = null;
            float bestScore = float.NegativeInfinity;
            foreach (ActRoom r in p.Rooms)
            {
                if (r.Special)
                    continue;
                Vector3 c = RoomPoint(r);
                float fromStalker = Vector3.Distance(c, s), fromMe = Vector3.Distance(c, pos);
                if (fromMe < 12f || fromMe > 90f)
                    continue;
                // Loin du traqueur, pas trop loin de nous, et pas en passant par lui.
                Vector3 dir = (c - pos).normalized, toS = (s - pos).normalized;
                float score = fromStalker - fromMe * 0.6f - Mathf.Max(0f, Vector3.Dot(dir, toS)) * 30f;
                if (score > bestScore && Reachable(pos, c))
                {
                    bestScore = score;
                    best = r;
                }
            }

            return best != null ? RoomPoint(best) : pos + (pos - s).normalized * 25f;
        }

        // ---------- Action ----------

        private void Act(Vector3 pos, ref PlayerInputReader.Frame f)
        {
            if (Current == Task.Boss)
            {
                BossStep(pos, ref f);
                return;
            }

            // Interaction au but : coffre, marchand, porte.
            if (Current == Task.Coffre && _targetChest != null && Vector3.Distance(pos, _targetChest.Point) < 3.2f)
            {
                Aim(_targetChest.Point, ref f);
                if (Time.time > _nextMenuAction)
                {
                    _nextMenuAction = Time.time + 0.5f;
                    if (_interactor.Focus == _targetChest)
                        f.Interact = true;
                    else
                        _targetChest.Interact(_health);                  // visée gênée (décor) : l'interaction est la même
                    Report.ChestTries++;
                }

                return;
            }

            if (Current == Task.Marchand && _targetMerchant != null && Vector3.Distance(pos, _targetMerchant.Point) < 6f)
            {
                Aim(_targetMerchant.Point, ref f);
                if (Time.time > _nextMenuAction)
                {
                    _nextMenuAction = Time.time + 0.5f;
                    if (_targetMerchant.Available(_health))
                        _targetMerchant.Interact(_health);
                    else
                    {
                        _merchantDone = true;
                        _merchantAgainAt = Time.time + 90f;
                        _targetMerchant = null;
                        Current = Task.Explorer;
                        _hasGoal = false;
                    }
                }

                return;
            }

            if (Current == Task.Sortie && _level.ExitDoorObject != null && Vector3.Distance(pos, _level.ExitDoorObject.Point) < 8.5f)
            {
                Aim(_level.ExitDoorObject.Point, ref f);
                if (Time.time > _nextMenuAction)
                {
                    _nextMenuAction = Time.time + 0.6f;
                    if (_interactor.Focus == _level.ExitDoorObject)
                        f.Interact = true;
                    else
                        _level.ExitDoorObject.Interact(_health);
                    Report.Note("sortie", $"porte payée à {Clock()} : {(_level.ExitDoorObject.Open ? "ouverte" : "refusée")}");
                }

                return;
            }

            if (Current == Task.Hall && Time.frameCount - _hallEnteredFrame > 60 * 25 && !Report.HallFailed && _zone != null && !_zone.Finished)
            {
                Report.HallFailed = true;
                Report.Fail("porte", $"la porte de sortie est ouverte mais l'acte ne se termine pas après 25 s dans le hall ({Clock()})");
            }

            // Combat en marchant (toutes les tâches sauf la fuite : on tire quand même en fuyant si une goule barre la route).
            bool fought = Fight(pos, ref f);
            Parry(pos, ref f);
            Navigate(pos, ref f, !fought);
        }

        private void HandleMenu(ShopMenu menu)
        {
            if (Time.time < _nextMenuAction)
                return;
            _nextMenuAction = Time.time + 0.4f;
            int souls = RunState.Current != null ? RunState.Current.Souls(_health.PlayerId) : 0;
            // Coffre : le premier choix ; marchand : l'offre la moins chère que l'on peut payer (le soin si rien d'autre).
            int pick = -1, cheapest = int.MaxValue;
            for (int i = 0; i < menu.Offers.Count; i++)
            {
                if (menu.Taken(i) || !menu.Affordable(i))
                    continue;
                int price = menu.Price(i);
                if (menu.Type == ShopMenu.Kind.Coffre)
                {
                    pick = i;
                    break;
                }

                if (price < cheapest && (menu.Offers[i].Upgrade != null || _health.CurrentHealth < _health.MaxHealth * 0.6f))
                {
                    cheapest = price;
                    pick = i;
                }
            }

            if (pick >= 0 && menu.Buy(pick))
            {
                Report.Purchases++;
                int paid = cheapest == int.MaxValue ? 0 : cheapest;
                string where = menu.Type == ShopMenu.Kind.Coffre ? "coffre" : "marchand";
                Report.Note("achat", $"{where} : {menu.Name(pick)} ({paid} âmes, reste {RunState.Current?.Souls(_health.PlayerId)}) à {Clock()}");
                if (menu.Type == ShopMenu.Kind.Marchand)
                    return;                                              // une autre offre peut-être
            }

            if (menu.IsOpen)
                menu.Close();
            if (menu.Type == ShopMenu.Kind.Coffre && _targetChest != null)
            {
                _chestsDone.Add(_targetChest);
                _targetChest = null;
            }

            if (menu.Type == ShopMenu.Kind.Marchand)
            {
                _merchantDone = true;
                _merchantAgainAt = Time.time + 90f;
                _targetMerchant = null;
            }

            Current = Task.Explorer;
            _hasGoal = false;
        }

        // ---------- Navigation ----------

        private NavMeshQueryFilter Filter => new NavMeshQueryFilter
        {
            agentTypeID = _nav != null ? _nav.SmallAgent : 0, areaMask = NavMesh.AllAreas,
        };

        private bool Reachable(Vector3 from, Vector3 to)
        {
            if (_nav == null || !_nav.Built)
                return true;
            if (!NavMesh.SamplePosition(from, out NavMeshHit a, 4f, Filter) || !NavMesh.SamplePosition(to, out NavMeshHit b, 6f, Filter))
                return false;
            return NavMesh.CalculatePath(a.position, b.position, Filter, _path) && _path.status == NavMeshPathStatus.PathComplete;
        }

        private float PathLength(Vector3 from, Vector3 to)
        {
            if (_nav == null || !_nav.Built)
                return Vector3.Distance(from, to);
            if (!NavMesh.SamplePosition(from, out NavMeshHit a, 4f, Filter) || !NavMesh.SamplePosition(to, out NavMeshHit b, 6f, Filter))
                return 1e5f + Vector3.Distance(from, to);
            if (!NavMesh.CalculatePath(a.position, b.position, Filter, _path) || _path.status != NavMeshPathStatus.PathComplete)
                return 1e5f + Vector3.Distance(from, to);
            float len = 0f;
            for (int i = 1; i < _path.corners.Length; i++)
                len += Vector3.Distance(_path.corners[i - 1], _path.corners[i]);
            return len;
        }

        private void SetGoal(Vector3 goal, string what)
        {
            _goal = goal;
            _hasGoal = true;
            _goalSet = Time.time;
            _bestDistance = float.PositiveInfinity;
            _lastProgress = Time.time;
            _stuckSince = float.NegativeInfinity;
            _tractionTries = 0;
            Status = what;
            Replan(transform.position);
        }

        private void Replan(Vector3 pos)
        {
            _corners.Clear();
            _corner = 0;
            if (_nav == null || !_nav.Built)
            {
                _corners.Add(_goal);
                return;
            }

            bool ok = NavMesh.SamplePosition(pos, out NavMeshHit a, 4f, Filter) && NavMesh.SamplePosition(_goal, out NavMeshHit b, 6f, Filter)
                      && NavMesh.CalculatePath(a.position, b.position, Filter, _path) && _path.status != NavMeshPathStatus.PathInvalid;
            if (ok)
            {
                _corners.AddRange(_path.corners.Skip(1));
                if (_path.status == NavMeshPathStatus.PathPartial)
                    _corners.Add(_goal);                                 // le bout en ligne droite (saut, traction)
            }

            if (_corners.Count == 0)
                _corners.Add(_goal);
        }

        private void Navigate(Vector3 pos, ref PlayerInputReader.Frame f, bool lookAhead)
        {
            if (!_hasGoal)
                return;
            if (_corners.Count == 0 || Time.time - _goalSet > 1.5f && (Time.frameCount % 90) == 0)
                Replan(pos);
            while (_corner < _corners.Count - 1 && Flat(_corners[_corner] - pos) < 1.4f && Mathf.Abs(_corners[_corner].y - pos.y) < 2.5f)
            {
                _corner++;
                _lastProgress = Time.time;                               // un coin de passé : on avance, même si le but s'éloigne
            }
            Vector3 target = _corners[Mathf.Min(_corner, _corners.Count - 1)];
            Vector3 to = target - pos;
            float flat = Flat(to);
            float distGoal = Vector3.Distance(pos, _goal);
            if (distGoal < _bestDistance - 0.5f)
            {
                _bestDistance = distGoal;
                _lastProgress = Time.time;
                _stuckSince = float.NegativeInfinity;
            }

            if (flat < 0.3f && _corner >= _corners.Count - 1)
                return;                                                  // arrivé

            Vector3 dir = new Vector3(to.x, 0f, to.z).normalized;
            if (lookAhead)
                Aim(target + Vector3.up * 1.4f, ref f, keepPitchFlat: true);
            Vector3 fwd = Quaternion.Euler(0f, _look.Yaw, 0f) * Vector3.forward, right = Quaternion.Euler(0f, _look.Yaw, 0f) * Vector3.right;
            f.Move = new Vector2(Vector3.Dot(dir, right), Vector3.Dot(dir, fwd)) * moveScale;

            MovementState st = _motor.State;
            float speed = new Vector2(st.Velocity.x, st.Velocity.z).magnitude;
            // Marche haute devant : saut.
            if (st.Grounded && to.y > 0.6f && flat < 3.5f)
                f.JumpPress = true;
            // Plus de progrès : saut, puis dash, puis traction vers le but, puis téléportation (défaut noté).
            float noProgress = Time.time - _lastProgress;
            if (noProgress > 1.2f && st.Grounded && speed < 2.5f)
                f.JumpPress = (Time.frameCount % 20) == 0;
            if (noProgress > 2.5f && (Time.frameCount % 45) == 0)
                f.DashPress = true;
            if (noProgress > 4f && Time.time > _attractHoldUntil && _tractionTries < 3)
            {
                _tractionTries++;
                _attractAim = _goal + Vector3.up * 3f;
                if (Physics.Raycast(_health.Center, (_attractAim - _health.Center).normalized, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Ignore))
                    _attractAim = hit.point;
                _attractHoldUntil = Time.time + 1.6f;
                f.AttractPress = true;
                Report.Tractions++;
            }

            if (Time.time < _attractHoldUntil)
            {
                Aim(_attractAim, ref f);
                f.AttractHeld = true;
            }

            if (noProgress > 12f)
            {
                if (float.IsNegativeInfinity(_stuckSince))
                    _stuckSince = Time.time;
                // En fuite, le but change sans cesse et le traqueur barre souvent la route : c'est une limite du bot, pas du niveau.
                string text = $"aucun progrès vers « {Status} » pendant 12 s à {Clock()}, position {pos:F1} (salle {RoomName(pos)}) ; téléporté au point suivant";
                if (Current == Task.Fuir)
                    Report.Note("blocage en fuite", text);
                else
                    Report.Fail("blocage", text);
                if (Current == Task.Coffre && _targetChest != null)
                {
                    _chestsDone.Add(_targetChest);                     // coffre inatteignable : abandonné (le défaut est noté)
                    _targetChest = null;
                    Current = Task.Explorer;
                    _hasGoal = false;
                }
                Vector3 jump = _corner < _corners.Count ? _corners[_corner] : _goal;
                _motor.Teleport(jump + Vector3.up * 0.2f);
                _lastProgress = Time.time;
                _bestDistance = float.PositiveInfinity;
                Report.Teleports++;
            }
        }

        private string RoomName(Vector3 pos)
        {
            int r = _level != null && _level.Plan != null ? _level.Plan.RoomAt(pos) : -1;
            return r >= 0 ? _level.Plan.Rooms[r].Name : "hors salle";
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        private void Aim(Vector3 at, ref PlayerInputReader.Frame f, bool keepPitchFlat = false)
        {
            Vector3 eye = _health.Center + Vector3.up * 0.6f;
            Vector3 d = at - eye;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float pitch = keepPitchFlat ? Mathf.Clamp(-Mathf.Atan2(d.y, Flat(d)) * Mathf.Rad2Deg, -25f, 25f) : -Mathf.Atan2(d.y, Flat(d)) * Mathf.Rad2Deg;
            // Vue lissée (pas de saut d'orientation brutal : la visée du jeu a une aide qui lit la vitesse de la souris).
            float curYaw = _look.Yaw, curPitch = _look.Pitch;
            float k = 1f - Mathf.Exp(-Time.deltaTime * 14f);
            float ny = Mathf.LerpAngle(curYaw, yaw, k), np = Mathf.Lerp(curPitch, pitch, k);
            _look.SetView(ny, Mathf.Clamp(np, -80f, 80f));
        }

        private void CheckOutside(Vector3 pos)
        {
            if (_level == null || _level.Plan == null)
                return;
            bool outside = pos.y < _lowestFloor - 30f;
            if (outside && float.IsNegativeInfinity(_outsideSince))
                _outsideSince = Time.time;
            else if (!outside)
                _outsideSince = float.NegativeInfinity;
            if (outside && Time.time - _outsideSince > 2f)
            {
                Report.Fail("chute", $"hors de la carte (y {pos.y:F1}, sol le plus bas {_lowestFloor:F1}) à {Clock()} ; ramené au départ");
                _motor.Respawn();
                _outsideSince = float.NegativeInfinity;
            }
        }

        // ---------- Combat ----------

        private Damageable _target;

        private bool Fight(Vector3 pos, ref PlayerInputReader.Frame f)
        {
            if (_motor.Holder != null && _motor.Holder.Held != null)
            {
                f.ThrowPress = true;                                     // objet attrapé par mégarde : lancé
                return false;
            }

            if (_target == null || _target.Dead || Vector3.Distance(_target.Center, pos) > fightRange + 4f)
            {
                _target = null;
                float best = fightRange;
                foreach (Damageable d in Damageable.All)
                {
                    if (d == null || d.Dead || d.ImmuneToOrbs || d.GetComponent<Stalker>() != null)
                        continue;
                    float dd = Vector3.Distance(d.Center, pos);
                    if (dd < best && Visible(d))
                    {
                        best = dd;
                        _target = d;
                    }
                }
            }

            if (_target == null)
                return false;
            Vector3 aimAt = _target.Center;
            Aim(aimAt, ref f);
            Vector3 eye = _health.Center + Vector3.up * 0.6f;
            float angle = Vector3.Angle(Quaternion.Euler(_look.Pitch, _look.Yaw, 0f) * Vector3.forward, aimAt - eye);
            ArtifactDefinition art = _motor.Artifact != null ? _motor.Artifact.Current : null;
            float dist = Vector3.Distance(aimAt, pos);
            if (art is BoneSwordSettings)
            {
                // Épée : s'approcher, frapper à portée.
                if (dist > 2.6f)
                {
                    Vector3 dir = (aimAt - pos).normalized;
                    Vector3 fwd = Quaternion.Euler(0f, _look.Yaw, 0f) * Vector3.forward, right = Quaternion.Euler(0f, _look.Yaw, 0f) * Vector3.right;
                    f.Move = new Vector2(Vector3.Dot(dir, right), Vector3.Dot(dir, fwd));
                    return true;
                }

                if (Time.time > _nextShot)
                {
                    f.ThrowPress = true;
                    _nextShot = Time.time + 0.32f;
                    Report.Shots++;
                }

                return true;
            }

            if (art is SpiritBookSettings)
            {
                if (angle < 6f && dist < 19f)
                {
                    f.ThrowHeld = true;
                    Report.Shots++;
                }

                return dist < 10f;
            }

            if (angle < 4f && Time.time > _nextShot && dist < (art is FireLanternSettings ? 12f : 50f))
            {
                f.ThrowPress = true;
                _nextShot = Time.time + (art is FireLanternSettings ? 0.7f : 0.28f);
                Report.Shots++;
            }

            // Une goule tout contre nous : on recule un peu en tirant (le barillet n'aime pas le corps à corps).
            return dist < 6f;
        }

        private bool Visible(Damageable d)
        {
            Vector3 eye = _health.Center + Vector3.up * 0.6f;
            Vector3 to = d.Center - eye;
            if (!Physics.Raycast(eye, to.normalized, out RaycastHit hit, to.magnitude + 0.5f, ~0, QueryTriggerInteraction.Ignore))
                return true;
            return hit.collider.GetComponentInParent<Damageable>() == d;
        }

        private readonly Dictionary<Ghoul, int> _seenStrikes = new Dictionary<Ghoul, int>();
        private readonly Dictionary<Ghoul, int> _seenLeaps = new Dictionary<Ghoul, int>();
        private readonly List<float> _pressAt = new List<float>();

        private void Parry(Vector3 pos, ref PlayerInputReader.Frame f)
        {
            float now = Time.time;
            foreach (Ghoul g in Ghoul.All)
            {
                if (g == null)
                    continue;
                float d = Vector3.Distance(g.Position, pos);
                if (d <= 6f && g.SignalsOn)
                    Report.SignalsSeen++;
                int strikes = g.Strikes, leaps = g.LeapCount;
                bool newStrike = _seenStrikes.TryGetValue(g, out int s0) && strikes > s0 && d < 5f;
                bool newLeap = _seenLeaps.TryGetValue(g, out int l0) && leaps > l0 && d < 9f;
                _seenStrikes[g] = strikes;
                _seenLeaps[g] = leaps;
                // Coup de griffes : impact 0.55 s après son début (D-096), appui à +0.40 s ; bond : appui à +0.15 s (pendant le vol, D-108).
                if (newStrike)
                    _pressAt.Add(now + 0.40f);
                else if (newLeap)
                    _pressAt.Add(now + 0.15f);
            }

            for (int i = _pressAt.Count - 1; i >= 0; i--)
                if (now >= _pressAt[i])
                {
                    _pressAt.RemoveAt(i);
                    f.Parry = true;
                    Report.ParryPresses++;
                }
        }

        // ---------- Boss ----------

        private void BossStep(Vector3 pos, ref PlayerInputReader.Frame f)
        {
            FinalBoss boss = FinalBoss.Current_;
            BossArena arena = BossArena.Current;
            if (boss == null)
                return;
            Transform bt = boss.transform;
            Vector3 toBoss = bt.position - pos;
            float dist = Flat(toBoss);
            // Esquive : annonce en cours → dash latéral.
            if (boss.TelegraphLeft > 0f && boss.TelegraphLeft < 0.45f && Time.time > _dodgeUntil)
            {
                _dodgeUntil = Time.time + 0.6f;
                _dodgeDir = Vector3.Cross(Vector3.up, toBoss.normalized) * _bossStrafe;
                f.DashPress = true;
                Report.Dodges++;
            }

            if (Time.time > _bossStrafeChange)
            {
                _bossStrafe = -_bossStrafe;
                _bossStrafeChange = Time.time + Random.Range(2.5f, 5f);
            }

            // Point faible ouvert : grimper sur un pilier par traction, viser le point faible (la lueur) ; sinon tirer sur le corps.
            Vector3 aimAt = bt.position + Vector3.up * 7f;
            Damageable weak = null;
            if (boss.WeakPointsOpen > 0)
            {
                float best = float.PositiveInfinity;
                foreach (Damageable d in Damageable.All)
                {
                    if (d == null || d.Dead || !d.name.StartsWith("Point faible"))
                        continue;
                    float dd = Vector3.Distance(d.Center, pos);
                    if (dd < best)
                    {
                        best = dd;
                        weak = d;
                    }
                }
            }

            if (weak != null)
                aimAt = weak.Center;
            if (arena != null && arena.PillarTops.Count > 0 && weak != null && !_bossOnPillar && Time.time > _pillarUntil)
            {
                Vector3 top = arena.PillarTops.OrderBy(t => Vector3.Distance(t, pos)).First();
                if (Vector3.Distance(top, pos) > 4f)
                {
                    Aim(top + Vector3.up * 1.5f, ref f);
                    f.AttractPress = (Time.frameCount % 30) == 0;
                    f.AttractHeld = true;
                    Report.Tractions++;
                    _pillarUntil = Time.time + 0.1f;
                    return;
                }

                _bossOnPillar = true;
                _pillarUntil = Time.time + 6f;
            }

            if (_bossOnPillar && Time.time > _pillarUntil)
                _bossOnPillar = false;

            Aim(aimAt, ref f);
            Vector3 eye = _health.Center + Vector3.up * 0.6f;
            float angle = Vector3.Angle(Quaternion.Euler(_look.Pitch, _look.Yaw, 0f) * Vector3.forward, aimAt - eye);
            ArtifactDefinition art = _motor.Artifact != null ? _motor.Artifact.Current : null;
            if (art is SpiritBookSettings)
                f.ThrowHeld = angle < 8f;
            else if (angle < 5f && Time.time > _nextShot)
            {
                f.ThrowPress = true;
                _nextShot = Time.time + (art is BoneSwordSettings ? 0.32f : art is FireLanternSettings ? 0.7f : 0.28f);
                Report.Shots++;
            }

            // Mouvement : garder 14 à 20 m (épée : au contact), en tournant autour.
            Vector3 fwd = Quaternion.Euler(0f, _look.Yaw, 0f) * Vector3.forward, right = Quaternion.Euler(0f, _look.Yaw, 0f) * Vector3.right;
            Vector3 want;
            float keep = art is BoneSwordSettings ? 2.5f : 16f;
            if (Time.time < _dodgeUntil)
                want = _dodgeDir;
            else if (dist > keep + 4f)
                want = new Vector3(toBoss.x, 0f, toBoss.z).normalized;
            else if (dist < keep - 3f)
                want = -new Vector3(toBoss.x, 0f, toBoss.z).normalized;
            else
                want = Vector3.Cross(Vector3.up, new Vector3(toBoss.x, 0f, toBoss.z).normalized) * _bossStrafe;
            if (arena != null && Flat(pos - arena.Center) > arena.Radius - 6f)
                want = (arena.Center - pos).normalized;                 // ne pas se coller aux falaises
            if (!_bossOnPillar)
                f.Move = new Vector2(Vector3.Dot(want, right), Vector3.Dot(want, fwd));
            Parry(pos, ref f);
        }
    }

    /// <summary>Compte rendu d'un acte joué par le bot.</summary>
    public sealed class BotReport
    {
        public int Act;
        public int Seed;
        public string Scene = "";
        public float ActSeconds;
        public float RealSeconds;
        public int Souls, Purchases, ChestTries, Hits, Shots, ParryPresses, ParriesSeen, Flees, Tractions, Teleports, Dodges, SignalsSeen;
        public float MaxSpeed;
        public bool AllRoomsVisited, SoulsShort, HallFailed;
        public readonly List<float> FrameSamples = new List<float>();
        public readonly List<string> Notes = new List<string>();
        public readonly List<string> Failures = new List<string>();
        public readonly List<string> Errors = new List<string>();
        private readonly HashSet<string> _errorKeys = new HashSet<string>();
        private float _realStart;

        public void Begin(int act)
        {
            Act = act;
            _realStart = Time.realtimeSinceStartup;
        }

        public void Note(string kind, string text) => Notes.Add($"[{kind}] {text}");

        public void Fail(string kind, string text)
        {
            Failures.Add($"[{kind}] {text}");
            Debug.LogWarning("Bot : " + kind + " : " + text);
        }

        public void Error(string message, string stack)
        {
            if (!_errorKeys.Add(message))
                return;
            string where = "";
            if (!string.IsNullOrEmpty(stack))
            {
                string[] lines = stack.Split('\n');
                where = lines.Length > 0 ? lines[0].Trim() : "";
            }

            Errors.Add(message + (where.Length > 0 ? " @ " + where : ""));
        }

        public void Close() => RealSeconds = Time.realtimeSinceStartup - _realStart;

        public float Fps => FrameSamples.Count > 0 ? FrameSamples.Average() : 0f;
        public float FpsLow => FrameSamples.Count > 0 ? FrameSamples.Min() : 0f;
        public bool Passed => Failures.Count == 0 && Errors.Count == 0;

        public string ToMarkdown()
        {
            var sb = new StringBuilder();
            string verdict = Passed ? "✅ réussi" : "❌ " + Failures.Count + " défaut(s), " + Errors.Count + " erreur(s)";
            sb.AppendLine($"## {Scene} (acte {Act + 1}, seed {Seed}) : {verdict}");
            sb.AppendLine();
            sb.AppendLine($"- Temps de jeu de l'acte : {Mathf.FloorToInt(ActSeconds / 60f)}:{Mathf.FloorToInt(ActSeconds % 60f):00} ; temps réel {RealSeconds:0} s (vitesse ×{Time.timeScale:0.#}).");
            sb.AppendLine($"- Âmes en fin d'acte : {Souls} ; achats : {Purchases} ; coffres essayés : {ChestTries}.");
            sb.AppendLine($"- Tirs : {Shots} ; coups reçus (comptés, invulnérable) : {Hits} ; parades : {ParryPresses} appuis, {ParriesSeen} réussies (images avec une annonce de goule à moins de 6 m : {SignalsSeen}).");
            sb.AppendLine($"- Fuites devant le traqueur : {Flees} ; tractions de secours : {Tractions} ; téléportations après blocage : {Teleports} ; esquives (boss) : {Dodges}.");
            sb.AppendLine($"- Vitesse max : {MaxSpeed:0.0} m/s ; images par seconde (éditeur) : moyenne {Fps:0}, minimum {FpsLow:0}.");
            sb.AppendLine($"- Toutes les salles visitées : {(AllRoomsVisited ? "oui" : "non")}.");
            if (Failures.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("**Défauts :**");
                foreach (string s in Failures)
                    sb.AppendLine("- " + s);
            }

            if (Errors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("**Erreurs du journal :**");
                foreach (string s in Errors.Take(20))
                    sb.AppendLine("- " + s);
            }

            sb.AppendLine();
            sb.AppendLine("<details><summary>Journal du bot</summary>");
            sb.AppendLine();
            foreach (string s in Notes)
                sb.AppendLine("- " + s);
            sb.AppendLine();
            sb.AppendLine("</details>");
            return sb.ToString();
        }
    }
}
