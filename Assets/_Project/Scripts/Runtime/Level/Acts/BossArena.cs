using System.Collections.Generic;
using ItHunts.Enemies;
using ItHunts.Orbs;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Arène du boss final (nuit 3, point 8), en contrebas du jardin : on y descend par le grand escalier du kit. Cuvette ouverte de 46 m
    /// de rayon sous le ciel, falaises tout autour (limite évidente, non magnétiques en haut), piliers brisés et statue géante (points
    /// d'accroche de traction pour atteindre les points faibles en hauteur), pans de mur (couverts), buttes (relief), caisses à lancer
    /// qui reviennent, candélabres et braseros. Construite au chargement ; le combat démarre quand le joueur a descendu l'escalier.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class BossArena : MonoBehaviour
    {
        [SerializeField] private FinalBossSettings boss;
        [SerializeField] private Transform stalker;
        [SerializeField] private Transform playerStart;
        [SerializeField] private ZoneNavMesh zoneNav;
        [SerializeField] private GameObject gameplayRoot;
        [SerializeField] private GameObject ghoul;
        [SerializeField] private GameObject crate;
        [SerializeField] private Material floorMaterial, wallMaterial;
        [Header("Kit du jardin")]
        [SerializeField] private GameObject cliff, stairs, pillar, cover, mound, statue, candelabra, brazier, sky, mist;
        [SerializeField] private float radius = 46f;

        public static BossArena Current { get; private set; }
        public FinalBoss Boss { get; private set; }
        public Vector3 Center => Vector3.zero;
        public float Radius => radius;
        public readonly List<Vector3> PillarTops = new List<Vector3>();

        private Transform _root;
        private readonly List<Vector3> _cratePiles = new List<Vector3>();
        private float _nextCrates;
        private bool _won;

        private void Awake()
        {
            Current = this;
            Build();
            if (gameplayRoot != null)
                gameplayRoot.SetActive(true);
        }

        private void OnDestroy()
        {
            if (Current == this)
                Current = null;
            FinalBoss.Defeated -= OnDefeated;
        }

        private GameObject Piece(GameObject prefab, Vector3 at, Vector3 forward, Vector3 scale, bool colliders)
        {
            if (prefab == null)
                return null;
            forward.y = 0f;
            GameObject go = Instantiate(prefab, at, Quaternion.LookRotation(forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward), _root);
            go.name = prefab.name;
            go.transform.localScale = scale;
            if (!colliders)
                foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                    Destroy(c);
            return go;
        }

        private GameObject Box(string name, Vector3 center, Vector3 size, Material mat, float yaw = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));
            go.AddComponent<BoxCollider>().size = size;
            if (mat != null)
            {
                var v = new GameObject("Kit_Boite");
                v.transform.SetParent(go.transform, false);
                v.transform.localScale = size;
                v.AddComponent<MeshFilter>().sharedMesh = ActLevel.CubeMesh;
                v.AddComponent<MeshRenderer>().sharedMaterial = mat;
            }

            return go;
        }

        private void Build()
        {
            _root = new GameObject("Arène du boss final").transform;
            // Sol de la cuvette, falaises autour (collision en anneau, non magnétique au-dessus de 12 m).
            Box("Sol de l'arène", new Vector3(0f, -2f, 0f), new Vector3(radius * 2.6f, 4f, radius * 2.6f), floorMaterial);
            int n = 22;
            for (int i = 0; i < n; i++)
            {
                float a = i * 360f / n;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                if (Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 12f)
                {
                    // Brèche de l'escalier (nord) : murs bas de chaque côté de la descente.
                    continue;
                }

                Vector3 at = dir * (radius + 2f);
                float seg = 2f * Mathf.PI * (radius + 2f) / n + 1.5f;
                Box("Falaise (collision)", at + Vector3.up * 6f, new Vector3(seg, 12f, 3f), wallMaterial, a);
                GameObject high = Box("Falaise haute (collision)", at + Vector3.up * 30f, new Vector3(seg, 36f, 3f), null, a);
                high.AddComponent<NonMagneticSurface>();
                Piece(cliff, dir * (radius + 3.5f), -dir, new Vector3(seg / 16.7f, 1.6f, 1f), false);
            }

            // Escalier de descente (nord) : palier en haut, rampe lisse pour la collision, escalier du kit pour la vue.
            Vector3 north = Vector3.forward;
            Vector3 topAt = north * (radius + 12f) + Vector3.up * 8.5f;
            Box("Palier de l'escalier", topAt + north * 6f - Vector3.up * 2f, new Vector3(16f, 4f, 14f), floorMaterial);
            Box("Mur du palier", topAt + north * 13f + Vector3.up * 8f, new Vector3(20f, 24f, 2f), wallMaterial);
            foreach (float s in new[] { -1f, 1f })
                Box("Mur de l'escalier", north * (radius + 4f) + Vector3.right * s * 8.5f + Vector3.up * 8f, new Vector3(2f, 24f, 22f), wallMaterial);
            KitBuild.Ramp(_root, "Rampe de l'escalier (collision)", north * (radius - 6f), north, 18f, 8.5f, 12f, null);
            Piece(stairs, topAt - north * 0.5f, -north, Vector3.one, false);
            if (playerStart != null)
                playerStart.SetPositionAndRotation(topAt + north * 3f + Vector3.up * 0.2f, Quaternion.LookRotation(-north));
            foreach (Player.PlayerHealth p in FindObjectsByType<Player.PlayerHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                p.transform.SetPositionAndRotation(topAt + north * 3f + Vector3.up * 0.2f, Quaternion.LookRotation(-north));

            // Piliers brisés en anneau (traction, dessus plat pour viser les points faibles), statue géante, couverts, buttes.
            for (int i = 0; i < 6; i++)
            {
                Vector3 d = Quaternion.Euler(0f, 30f + i * 60f, 0f) * Vector3.forward;
                Vector3 at = d * 25f;
                GameObject p = Piece(pillar, at, -d, Vector3.one, false);
                var cap = new GameObject("Pilier (collision)");
                cap.transform.SetParent(_root, false);
                cap.transform.position = at;
                var cc = cap.AddComponent<CapsuleCollider>();
                cc.radius = 1.2f;
                cc.height = 9.6f;
                cc.center = Vector3.up * 4.8f;
                Box("Sommet du pilier", at + Vector3.up * 9.75f, new Vector3(3f, 0.5f, 3f), null);
                PillarTops.Add(at + Vector3.up * 10f);
                _cratePiles.Add(at - d * 4f);
            }

            // Nuit 4, point 8 : le centre (20 m) reste dégagé pour les charges et les esquives ; statue et couverts sur l'anneau extérieur.
            Vector3 statueAt = new Vector3(-23f, 0f, -22f);
            GameObject st = Piece(statue, statueAt, new Vector3(0.6f, 0f, 1f), Vector3.one, true);
            if (st != null)
            {
                Box("Socle de la statue (plateforme)", statueAt + Vector3.up * 7.5f, new Vector3(3.6f, 0.5f, 3.6f), null);
                PillarTops.Add(statueAt + Vector3.up * 7.8f);
            }

            for (int i = 0; i < 5; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 72f + 12f, 0f) * Vector3.forward;
                Piece(cover, d * (i % 2 == 0 ? 31f : 36f), -d, Vector3.one, true);
            }

            for (int i = 0; i < 3; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 120f + 70f, 0f) * Vector3.forward;
                Piece(mound, d * 33f, -d, Vector3.one, true);
            }

            // Lumières : candélabres et braseros autour, ciel de lune, brume au sol.
            for (int i = 0; i < 10; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 36f + 18f, 0f) * Vector3.forward;
                GameObject lamp = Piece(i % 2 == 0 ? brazier : candelabra, d * (radius - 4f), -d, Vector3.one * (i % 2 == 0 ? 1.6f : 1.4f), false);
                if (lamp != null)
                {
                    // Collision simple (nuit 4 : collisions sur tous les décors) ; le boss l'enjambe.
                    var cc = lamp.AddComponent<CapsuleCollider>();
                    cc.radius = i % 2 == 0 ? 0.9f : 0.5f;
                    cc.height = 3f;
                    cc.center = Vector3.up * 1.5f;
                    lamp.AddComponent<StalkerPassesThrough>();
                }
            }

            Piece(sky, Vector3.zero, Vector3.forward, Vector3.one, true);
            Piece(mist, new Vector3(0f, 0f, -10f), Vector3.forward, Vector3.one, true);
            SpawnCrates();

            var vol = new GameObject("Volume : arène du boss");
            vol.transform.SetParent(_root, false);
            vol.transform.position = Vector3.up * 15f;
            vol.AddComponent<RoomVolume>().Configure("Arène du boss", new Vector3(radius * 2f, 30f, radius * 2f), RoomKind.Arene);
            if (zoneNav != null)
                zoneNav.Configure(new Vector3(0f, 5f, 8f), new Vector3(radius * 2.6f, 40f, radius * 2.8f));
            if (stalker != null)
            {
                // Nuit 4, point 1 : on ne le voit pas attendre au milieu ; il est perché au sommet de la falaise sud, hors de vue, et bondit
                // dans la cuvette quand le joueur descend l'escalier (Entrance).
                stalker.position = Vector3.back * (radius + 6f) + Vector3.up * 14f;
                stalker.rotation = Quaternion.LookRotation(Vector3.forward);
            }

            FinalBoss.Defeated -= OnDefeated;
            FinalBoss.Defeated += OnDefeated;
        }

        /// <summary>Caisses à lancer au pied des piliers : elles reviennent (toutes les 15 s) tant qu'il en manque.</summary>
        private void SpawnCrates()
        {
            if (crate == null)
                return;
            int existing = 0;
            foreach (Grabbable g in Grabbable.All)
                if (g != null && !g.IsEnemy && !g.Broken)
                    existing++;
            foreach (Vector3 pile in _cratePiles)
                for (int k = 0; k < 2 && existing < 16; k++, existing++)
                    Instantiate(crate, pile + new Vector3(k * 1.2f - 0.6f, 0.6f, 0f), Quaternion.identity, _root).name = "Caisse de l'arène";
        }

        private void Update()
        {
            if (Boss == null && stalker != null && Time.timeSinceLevelLoad > 2f)
            {
                // Le combat démarre quand un joueur a descendu l'escalier (il entre dans la cuvette).
                foreach (Player.PlayerHealth p in Player.PlayerHealth.All)
                    if (p != null && !p.Dead && p.transform.position.y < 3f && p.transform.position.magnitude < radius - 2f)
                    {
                        Boss = stalker.gameObject.GetComponent<FinalBoss>() ?? stalker.gameObject.AddComponent<FinalBoss>();
                        Boss.GhoulPrefab = ghoul;
                        StartCoroutine(Entrance());
                        break;
                    }
            }

            if (Time.time >= _nextCrates)
            {
                _nextCrates = Time.time + 15f;
                SpawnCrates();
            }
        }

        /// <summary>
        /// Entrée du boss (nuit 4, point 8) : cri depuis la falaise sud, bond de 1.4 s dans la cuvette (arc de 10 m), poussière et secousse à
        /// la réception, puis le combat commence (barre de vie, annonces).
        /// </summary>
        private System.Collections.IEnumerator Entrance()
        {
            Vector3 from = stalker.position, to = new Vector3(0f, 0f, -14f);
            var art = ItHunts.Audio.GameAudio.Settings;
            AudioClip appear = art != null ? ItHunts.Audio.GameAudio.Pick(art.stalkerAppearCries) : null;
            ItHunts.Audio.GameAudio.PlayAt(appear != null ? appear : ItHunts.Audio.GameAudio.Pick(art != null ? art.stalkerEnragedCries : null), from + Vector3.up * 10f, 1f, 160f);
            foreach (Player.CameraShake sh in FindObjectsByType<Player.CameraShake>(FindObjectsSortMode.None))
                sh.AddTrauma(0.35f);
            yield return new WaitForSeconds(0.9f);
            float t = 0f;
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 1.4f);
                stalker.position = Vector3.Lerp(from, to, u) + Vector3.up * (Mathf.Sin(u * Mathf.PI) * 10f);
                stalker.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                yield return null;
            }

            stalker.position = to;
            var vfx = ItHunts.Combat.GameVfx.Settings;
            if (vfx != null)
                ItHunts.Combat.GameVfx.Spawn(vfx.bossGroundImpact != null ? vfx.bossGroundImpact : vfx.stalkerLandingDust, to, Quaternion.identity, 2.5f, null, 3f);
            ItHunts.Audio.GameAudio.PlayAt(art != null ? art.bossSlam : null, to, 1f, 120f);
            foreach (Player.CameraShake sh in FindObjectsByType<Player.CameraShake>(FindObjectsSortMode.None))
                sh.AddTrauma(0.7f);
            yield return new WaitForSeconds(0.4f);
            Boss.Begin(boss, Vector3.zero, radius);
        }

        private void OnDefeated(FinalBoss b)
        {
            if (_won)
                return;
            _won = true;
            Invoke(nameof(Win), 4f);
        }

        private void Win()
        {
            ZoneState z = FindFirstObjectByType<ZoneState>();
            if (z != null)
                z.LevelWon();
        }
    }
}
