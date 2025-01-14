using Terraria;
using Terraria.ID;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.DataStructures;
using ExampleMod.Content.Items;
using ExampleMod.Content.Projectiles;

namespace ExampleMod.Common.GlobalTiles
{
	public class GlobalShakeTrees : GlobalTile {
		// With this hook, we can make things happen when a tree is shook.
		// See ExampleSourceDependentItemTweaks for another example for tree shaking.
		public override void ShakeTree(int x, int y, TreeTypes treeType) {

			// Normal forest trees have a 33% chance to drop an Example Item.
			if (treeType == TreeTypes.Forest && WorldGen.genRand.NextBool(3)) {
				Item.NewItem(WorldGen.GetItemSource_FromTreeShake(x, y), x * 16, y * 16, 16, 16, ModContent.ItemType<ExampleItem>());
			}

			// Glowing Mushroom trees have 10% chance to drop between 3 and 10 Mushroom Torches.
			if (treeType == TreeTypes.Mushroom && WorldGen.genRand.NextBool(10)) {
				Item.NewItem(WorldGen.GetItemSource_FromTreeShake(x, y), x * 16, y * 16, 16, 16, ItemID.MushroomTorch, WorldGen.genRand.Next(3, 11));
			}

			// Snow (Boreal) trees have a 5% chance to spawn an Example Paper Airplane projectile that flies left or right.
			// The owner of the projectile is set to Main.myPlayer which means the server owns the projectile in multiplayer.
			if (treeType == TreeTypes.Snow && WorldGen.genRand.NextBool(20)) {
				Projectile.NewProjectile(new EntitySource_ShakeTree(x, y), x * 16, y * 16, Main.rand.Next(-16, 16), 0f, ModContent.ProjectileType<ExamplePaperAirplaneProjectile>(), Damage: 4, KnockBack: 1f, Owner: Main.myPlayer);
			}

			// Jungle (Mahogany) trees have a 14% chance to spawn a Giant Flying Fox if located on the surface and in Hardmode.
			// y == 0 is the top of the world, so y < Main.worldSurface is the area from the surface height to the top of the world.
			if (treeType == TreeTypes.Jungle && WorldGen.genRand.NextBool(7) && y < Main.worldSurface && Main.hardMode) {
				NPC.NewNPC(WorldGen.GetNPCSource_ShakeTree(x, y), x * 16, y * 16, NPCID.GiantFlyingFox);
			}

			// In this example, there is 20% chance for any tree to shoot a hostile arrow downwards on No Traps and Get Fixed Boi worlds.
			if (WorldGen.genRand.NextBool(5) && Main.noTrapsWorld) {
				Projectile.NewProjectile(new EntitySource_ShakeTree(x, y), x * 16, y * 16, Main.rand.Next(-100, 101) * 0.002f, 8f, ProjectileID.WoodenArrowHostile, Damage: 10, KnockBack: 0f, Owner: Main.myPlayer);
			}

			// ModTree will always count a Forest tree.
			// ModPalmTree will always count as Palm tree.
			// To make things happen when your modded tree is shook, override ModTree.Shake(). See ExampleTree.
		}
	}
}
