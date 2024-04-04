using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using nkast.Aether.Physics2D.Dynamics;
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

        // Player settings constants. Positions are in meters, not pixels,
        // and origin is bottom left corner of the game window.
        readonly Vector2 PLR1_STARTPOS = new Vector2(5.2f, 0.8f);
        readonly Vector2 PLR2_STARTPOS = new Vector2(5.2f, 1.6f);
        readonly Color PLR1_COLOR = Color.Red;
        readonly Color PLR2_COLOR = new Color(0, 255, 0, 255);

        //
        // Instance variables
        //
        Texture2D plrTex;
        Thing plr1, plr2;
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

        Thing AddPlayer(Color plrColor, Vector2 plrStartPos)
        {
            Thing t = new Thing(world,
                                plrTex,
                                plrStartPos,
                                BodyType.Dynamic,
                                isVisible: true,
                                drawTintRed: plrColor.R,
                                drawTintGreen: plrColor.G,
                                drawTintBlue: plrColor.B,
                                drawTintAlpha: plrColor.A,
                                originX: plrTex.Width / 2,
                                originY: plrTex.Height / 2);

            t.body.LinearDamping = 0.0f;
            AddThing(t);
            return t;
        }
    } // End of class MinigameExampleTwoCars
} // End of namespace polyframework

