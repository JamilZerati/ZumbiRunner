using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class EnemyImporterTests
    {
        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void ImportAll_ImportsAll7Archetypes()
        {
            var errors = new List<string>();
            int count = EnemyImporter.ImportAll(errors: errors);

            Assert.IsEmpty(errors);
            Assert.AreEqual(7, count);
        }
    }
}
