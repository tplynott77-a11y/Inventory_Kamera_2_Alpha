using System;
using Xunit;

namespace InventoryKamera.Tests
{
    public class MaterialCollectionRoutingTests
    {
        [Fact]
        public void CharacterDevelopmentItemsAreKeptOutOfOrdinaryMaterials()
        {
            var inventory = new Inventory();
            var destination = MaterialScraper.GetDestinationSet(inventory, InventoryPage.CharacterDevelopmentItems);
            Assert.Same(inventory.DevMaterials, destination);
            destination.Add(new Material("GuideToBallad", 4));
            Assert.Single(inventory.DevMaterials);
            Assert.Empty(inventory.Materials);
            Assert.Single(inventory.AllMaterials);
        }

        [Fact]
        public void OrdinaryMaterialsUseTheirOwnCollection()
        {
            var inventory = new Inventory();
            var destination = MaterialScraper.GetDestinationSet(inventory, InventoryPage.Materials);
            Assert.Same(inventory.Materials, destination);
            destination.Add(new Material("FalseWormBait", 1109));
            Assert.Single(inventory.Materials);
            Assert.Empty(inventory.DevMaterials);
            Assert.Single(inventory.AllMaterials);
        }

        [Fact]
        public void InvalidPageIsRejected()
        {
            var inventory = new Inventory();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MaterialScraper.GetDestinationSet(inventory, InventoryPage.Artifacts));
        }
    }
}
