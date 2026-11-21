using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Os du traqueur par rôle (préparation du gréement Humanoid, 2026-11-21) : aujourd'hui les noms du gréement v4 (Generic) ;
    /// avec un avatar Humanoid, l'os vient de Animator.GetBoneTransform. Le visage et l'IK des membres passent par ici : changer de
    /// gréement ne demandera que cette table (et les os sans équivalent Humanoid : mâchoire, cheveux, points de contact).
    /// Liste de ce qui changera : docs/design/features/boss-traqueur/humanoid.md.
    /// </summary>
    public static class StalkerRig
    {
        public enum Role : byte
        {
            Tete, Cou, Machoire,
            CuisseD, JambeD, PiedD, CuisseG, JambeG, PiedG,
            BrasD, AvantBrasD, MainD, BrasG, AvantBrasG, MainG,
            ContactPiedD, ContactPiedG, ContactMainD, ContactMainG,
        }

        /// <summary>Nom de l'os dans le gréement v4 (Generic).</summary>
        public static string V4Name(Role r) => r switch
        {
            Role.Tete => "Tete", Role.Cou => "Cou_2", Role.Machoire => "Machoire",
            Role.CuisseD => "Cuisse_D", Role.JambeD => "Jambe_D", Role.PiedD => "Pied_D",
            Role.CuisseG => "Cuisse_G", Role.JambeG => "Jambe_G", Role.PiedG => "Pied_G",
            Role.BrasD => "Bras_D", Role.AvantBrasD => "AvantBras_D", Role.MainD => "Main_D",
            Role.BrasG => "Bras_G", Role.AvantBrasG => "AvantBras_G", Role.MainG => "Main_G",
            Role.ContactPiedD => "Contact_Pied_D", Role.ContactPiedG => "Contact_Pied_G",
            Role.ContactMainD => "Contact_Main_D", Role.ContactMainG => "Contact_Main_G",
            _ => null,
        };

        /// <summary>Équivalent Humanoid (null : pas d'os Humanoid, il restera cherché par son nom ou ajouté au gréement).</summary>
        public static HumanBodyBones? Human(Role r) => r switch
        {
            Role.Tete => HumanBodyBones.Head, Role.Cou => HumanBodyBones.Neck, Role.Machoire => HumanBodyBones.Jaw,
            Role.CuisseD => HumanBodyBones.RightUpperLeg, Role.JambeD => HumanBodyBones.RightLowerLeg, Role.PiedD => HumanBodyBones.RightFoot,
            Role.CuisseG => HumanBodyBones.LeftUpperLeg, Role.JambeG => HumanBodyBones.LeftLowerLeg, Role.PiedG => HumanBodyBones.LeftFoot,
            Role.BrasD => HumanBodyBones.RightUpperArm, Role.AvantBrasD => HumanBodyBones.RightLowerArm, Role.MainD => HumanBodyBones.RightHand,
            Role.BrasG => HumanBodyBones.LeftUpperArm, Role.AvantBrasG => HumanBodyBones.LeftLowerArm, Role.MainG => HumanBodyBones.LeftHand,
            _ => null,                                                   // points de contact : os ajoutés, gardés par leur nom
        };

        /// <summary>L'os d'un rôle : avatar Humanoid s'il y en a un, sinon par son nom v4 dans la liste des os.</summary>
        public static Transform Find(Transform[] bones, Role r, Animator animator = null)
        {
            if (animator != null && animator.isHuman && Human(r) is HumanBodyBones h)
            {
                Transform t = animator.GetBoneTransform(h);
                if (t != null)
                    return t;
            }

            string n = V4Name(r);
            return bones != null && n != null ? System.Array.Find(bones, t => t != null && t.name == n) : null;
        }
    }
}
