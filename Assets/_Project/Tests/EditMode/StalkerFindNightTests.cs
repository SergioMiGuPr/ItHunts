using ItHunts.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>D-124 : la demi-largeur d'un lien ne compte qu'en travers de lui, ni en hauteur, ni dans son axe.</summary>
    public class StalkerFindNightTests
    {
        private static readonly Vector3 A = Vector3.zero, B = new Vector3(0f, 4f, 16f);

        [Test]
        public void Marge_EnTraversSeulement()
        {
            Assert.IsTrue(StalkerPassageMath.Near(B + new Vector3(2.9f, 0f, 0f), B, A, B, 2.5f, 2f), "2.9 m en travers : 0.9 m après la marge");
            Assert.IsFalse(StalkerPassageMath.Near(B + new Vector3(5f, 0f, 0f), B, A, B, 2.5f, 2f), "5 m en travers : trop loin");
            Assert.IsFalse(StalkerPassageMath.Near(B + new Vector3(0f, 3f, 0f), B, A, B, 2.5f, 2f), "3 m plus haut : la marge ne joue pas");
            Assert.IsFalse(StalkerPassageMath.Near(B + new Vector3(0f, 0f, 3f), B, A, B, 2.5f, 2f), "3 m dans l'axe : la marge ne joue pas");
        }

        [Test]
        public void SansMarge_OuLienVertical_RegleDeDistance()
        {
            Assert.IsTrue(StalkerPassageMath.Near(B + Vector3.right * 2.4f, B, A, B, 2.5f, 0f));
            Assert.IsFalse(StalkerPassageMath.Near(B + Vector3.right * 2.6f, B, A, B, 2.5f, 0f));
            Vector3 top = new Vector3(0f, 10f, 0f);
            Assert.IsFalse(StalkerPassageMath.Near(top + Vector3.right * 3f, top, A, top, 2.5f, 2f), "lien vertical : distance simple");
        }
    }
}
