using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Presentation.Entities;

namespace Helpers
{
	public class LevelData
	{
		public int Width;
		public int Height;
		public LevelTileComponent[,] Tiles;
		public List<Entity> Entities;
        public Entity[,] TilesEntities;
    }
}