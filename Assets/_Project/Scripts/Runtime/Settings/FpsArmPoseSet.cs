using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Poses des bras FPS de la DA (clips BrasFPS|Pose_* de SK_BrasFPS_*, mêmes os pour les 4 personnages), cuites dans l'éditeur
    /// (FpsArmsSetup) : rotation locale de chaque os, pour chaque pose et chaque côté. Le jeu passe d'une pose à l'autre sans
    /// Animator (FpsArms). Pose_Attraper a trois clés : _0 (repos), _1 (ouverte), _2 (refermée).
    /// </summary>
    [CreateAssetMenu(fileName = "FpsArmPoses", menuName = "ItHunts/Vue FPS/Poses des bras")]
    public sealed class FpsArmPoseSet : ScriptableObject
    {
        [Serializable]
        public sealed class Pose
        {
            public string name;
            public Quaternion[] rotations;
            [Tooltip("Vrai : lue dans un clip qui a des courbes ; faux : clip vide à l'import (le jeu ferme les doigts lui-même).")]
            public bool fromClip;
        }

        [Serializable]
        public sealed class Side
        {
            public string[] bones;
            [Tooltip("Faux : les clips importés étaient vides (aucune courbe) ; le jeu ferme alors les doigts lui-même.")]
            public bool fromClips;
            public List<Pose> poses = new List<Pose>();

            public Pose Find(string name)
            {
                foreach (Pose p in poses)
                    if (p.name == name)
                        return p;
                return null;
            }
        }

        public Side right = new Side();
        public Side left = new Side();

        [Header("Passage d'une pose à l'autre")]
        [Tooltip("Vitesse du lissage vers la pose de fond (1/s) et vers la pose d'un geste (plus vif).")]
        [Min(0.1f)] public float blendRate = 14f;
        [Min(0.1f)] public float gestureBlendRate = 35f;
    }
}
