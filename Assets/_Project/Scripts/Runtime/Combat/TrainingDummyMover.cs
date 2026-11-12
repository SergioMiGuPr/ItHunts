using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Ennemi factice mobile : allers-retours en ligne droite (corps cinématique déplacé à pas fixe), pour viser en
    /// mouvement. S'arrête pendant qu'il est mort.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TrainingDummyMover : MonoBehaviour
    {
        [SerializeField] private TrainingDummySettings settings;
        [Tooltip("Trajet depuis la position de départ (m, monde).")]
        [SerializeField] private Vector3 travel = new Vector3(20f, 0f, 0f);

        private Rigidbody _body;
        private Damageable _damageable;
        private EnemyBody _enemy;
        private Vector3 _start;
        private float _progress;
        private int _direction = 1;
        private float _pause;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _damageable = GetComponent<Damageable>();
            _enemy = GetComponent<EnemyBody>();
            _start = transform.position;
        }

        private void FixedUpdate()
        {
            if (settings == null || (_damageable != null && _damageable.Dead) || (_enemy != null && _enemy.Busy))
                return;            // mort, repoussé ou étourdi : il s'arrête
            float length = travel.magnitude;
            if (length < 1e-3f)
                return;
            float dt = Time.fixedDeltaTime;
            if (_pause > 0f)
            {
                _pause -= dt;
                return;
            }

            // Repoussé loin de son trajet : il y revient d'abord, à sa vitesse de marche.
            Vector3 onPath = _start + travel / length * _progress;
            Vector3 current = _body.position;
            if ((onPath - current).sqrMagnitude > 0.05f * 0.05f)
            {
                _body.MovePosition(Vector3.MoveTowards(current, onPath, settings.moveSpeed * dt));
                return;
            }

            _progress += _direction * settings.moveSpeed * dt;
            if (_progress >= length || _progress <= 0f)
            {
                _progress = Mathf.Clamp(_progress, 0f, length);
                _direction = -_direction;
                _pause = settings.pauseAtEnds;
            }

            _body.MovePosition(_start + travel / length * _progress);
        }
    }
}
