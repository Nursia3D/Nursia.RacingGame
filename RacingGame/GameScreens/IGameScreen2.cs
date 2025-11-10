using Microsoft.Xna.Framework;
using RacingGame.Graphics;

namespace RacingGame.GameScreens
{
	public interface IGameScreen2
	{
		void Update(GameTime gameTime);
		void Render(RenderSystem renderSystem);
	}
}
