using System.Reflection;
using Game.Editor;
using Game.Gameplay;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class SceneBuilderM5StartupTests
    {
        [Test]
        public void M5GreyboxScene_WeaponControllerStart_EquipsPistolFromSerializedCatalog()
        {
            SceneBuilder.BuildM5GreyboxScene();
            EditorSceneManager.OpenScene(SceneBuilder.M5GreyboxScenePath, OpenSceneMode.Single);
            var weapon = Object.FindAnyObjectByType<WeaponController>();
            Assert.IsNotNull(weapon);
            Assert.AreEqual(string.Empty, weapon.EquippedWeaponId);

            typeof(WeaponController)
                .GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(weapon, null);

            Assert.AreEqual("pistol", weapon.EquippedWeaponId);
        }
    }
}
