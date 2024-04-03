using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;                 // For Debug.WriteLine
using System.IO;                          // For file handling

namespace polyframework
{
    public class MinigameExampleTwoCars : Minigame_Base.MinigameBase
    {
        //
        // Constants
        //
        // Game constants
        const string MINIGAME_NAME = "TwoCars";
        const string LEVEL_NAME = "Level1";

        // Car settings constants
        const string CAR_PICTURE_NAME = "graycar";

        //
        // Instance variables
        //
        Texture2D plrTex;
        SpriteFont font;

        public MinigameExampleTwoCars()
        {
            IsMouseVisible = true;
            // Don't use fullscreen when debugging, because drawing can stop working then
            if (!Debugger.IsAttached)
            {
                _graphics.IsFullScreen = true;
                _graphics.ApplyChanges();
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            // This is a top-down game, so turn off gravity
            world.Gravity = new Vector2(0, 0);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load the player texture
            string carPicPath = Path.Combine(MINIGAME_NAME, CAR_PICTURE_NAME);
            plrTex = Content.Load<Texture2D>(carPicPath);

            // Load the font
            font = Content.Load<SpriteFont>("font");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
