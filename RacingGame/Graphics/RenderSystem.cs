namespace RacingGame.Graphics
{
	public class RenderSystem
	{
		public Renderer2D Renderer2D { get; } = new Renderer2D();
		public Renderer3D Renderer3D { get; } = new Renderer3D();
	}
}
