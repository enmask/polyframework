using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using nkast.Aether.Physics2D.Dynamics;
using System.Diagnostics;                 // For Debug.WriteLine
using System.IO;                          // For file handling
using System.Collections.Generic;         // For lists
using static System.Math;                 // For sin and cos etc.

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
        List<Thing> players;
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

            // Add red and green players
            plr1 = AddPlayer(PLR1_COLOR, PLR1_STARTPOS);
            plr2 = AddPlayer(PLR2_COLOR, PLR2_STARTPOS);

            // It's nice to have a list of just the players, not all the things
            players = new List<Thing>();
            players.Add(plr1);
            players.Add(plr2);

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

            foreach (Thing plr in players)
                ApplyUserInput(plr);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // We don't need to draw the things, because the base.Draw call takes care of that,
            // but let's draw some text info.
            _spriteBatch.Begin();
            // Make a nice string...
            string posStr = "Car 1 x: " + Round(plr1.body.Position.X, 1) +
                            "      y: " + Round(plr1.body.Position.Y, 1);
            // ... and draw the string.
            _spriteBatch.DrawString(font, posStr, new Vector2(170, 135), Color.White);
            _spriteBatch.End();

            // This call takes care of drawing all the Thing:s
            base.Draw(gameTime);
        }


        void ApplyUserInput(Thing plr)
        {
            if (IsActionActive("accelerate", plr))
            {
                plr.body.ApplyLinearImpulse(
                    0.04f *
                    new Vector2((float)Cos(plr.body.Rotation),
                                (float)Sin(plr.body.Rotation))
                );
            }
            if (IsActionActive("turnleft", plr))
                plr.body.ApplyAngularImpulse(0.02f);

            if (IsActionActive("turnright", plr))
                plr.body.ApplyAngularImpulse(-0.02f);
        }


        // Returns true if the action is active for a player
        bool IsActionActive(string action, Thing plr)
        {
            for (int plrIx = 0; plrIx < players.Count; plrIx++)
            {
                if (plr == players[plrIx])
                {

                    Debug.WriteLine("plr.id: " + plr.id + " action: " + action + "group: " + plr.group);


                    switch (action)
                    {
                        case "accelerate":
                            switch (plrIx)
                            {
                                case 0:
                                    return Keyboard.GetState().IsKeyDown(Keys.Up);   // Player 1 accelerate
                                case 1:
                                    return Keyboard.GetState().IsKeyDown(Keys.W);    // Player 2 accelerate
                                default:
                                    return false;
                            }
                        case "turnleft":
                            switch (plrIx)
                            {
                                case 0:
                                    return Keyboard.GetState().IsKeyDown(Keys.Left); // Player 1 turnleft
                                case 1:
                                    return Keyboard.GetState().IsKeyDown(Keys.A);    // Player 2 turnleft
                                default:
                                    return false;
                            }
                        case "turnright":
                            switch (plrIx)
                            {
                                case 0:
                                    return Keyboard.GetState().IsKeyDown(Keys.Right); // Player 1 turnright
                                case 1:
                                    return Keyboard.GetState().IsKeyDown(Keys.D);    // Player 2 turnright
                                default:
                                    return false;
                            }
                    }
                }
            }
            return false;
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

