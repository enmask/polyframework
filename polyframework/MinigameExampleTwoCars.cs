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

        // Font constants
        const string FONT_NAME = "Fonts/font";

        // Car settings constants
        const string CAR_PICTURE_NAME = "Pictures/graycar";
        const float CAR_ACCELERATION = 0.04f;
        const float CAR_TURN_POWER = 0.02f;

        // Player settings constants. Positions are in meters, not pixels,
        // and origin is bottom left corner of the game window.
        readonly Vector2 PLR1_STARTPOS = new Vector2(5.2f, 0.8f);
        readonly Vector2 PLR2_STARTPOS = new Vector2(5.2f, 1.6f);
        readonly Vector2 PLR3_STARTPOS = new Vector2(5.2f, 2.4f);
        readonly Vector2 PLR4_STARTPOS = new Vector2(5.2f, 3.2f);
        readonly Color PLR1_COLOR = Color.Red;
        readonly Color PLR2_COLOR = new Color(0, 255, 0, 255);
        readonly Color PLR3_COLOR = new Color(0, 0, 255, 255);
        readonly Color PLR4_COLOR = new Color(0, 255, 255, 255);

        //
        // Instance variables
        //
        Texture2D plrTex;
        Thing plr1, plr2, plr3, plr4;
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
            plr3 = AddPlayer(PLR3_COLOR, PLR3_STARTPOS);
            plr4 = AddPlayer(PLR4_COLOR, PLR4_STARTPOS);

            // It's nice to have a list of just the players, not all the things
            players = new List<Thing>();
            players.Add(plr1);
            players.Add(plr2);
            players.Add(plr3);
            players.Add(plr4);

            // This is a top-down game, so turn off gravity
            world.Gravity = new Vector2(0, 0);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load the player texture
            string carPicPath = MINIGAME_NAME + "/" + CAR_PICTURE_NAME;
            plrTex = Content.Load<Texture2D>(carPicPath);
            Debug.WriteLine("Subclass LoadContent loaded texture: " + carPicPath);

            // Load the font
            string fontPath = MINIGAME_NAME + "/" + FONT_NAME;
            font = Content.Load<SpriteFont>(fontPath);

            // Load level (textures, colliders, and map)
            LoadLevel(Content.RootDirectory, "");
        }

        protected override void Update(GameTime gameTime)
        {
            // Get the server user input from keyboard etc, and the client user input from the network.
            PrepareUserInput();

            if (IsServer())
            {
                foreach (Thing plr in players)
                    ApplyUserInput(plr);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // We don't need to draw the things, because the base.Draw call takes care of that,
            // but let's draw some text info.
            _spriteBatch.Begin();

            // Draw the FPS (framerate) and update rate
            _spriteBatch.DrawString(font, $"FPS: {frameRate}       Update rate: {updateRate}", new Vector2(170, 110), Color.White);

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
                Vector2 direction = new Vector2((float)Cos(plr.body.Rotation),
                                                (float)Sin(plr.body.Rotation));
                plr.body.ApplyLinearImpulse(CAR_ACCELERATION * direction);
            }
            if (IsActionActive("turnleft", plr))
                plr.body.ApplyAngularImpulse(CAR_TURN_POWER);

            if (IsActionActive("turnright", plr))
                plr.body.ApplyAngularImpulse(-CAR_TURN_POWER);
        }

        // Returns true if the action is active for a player
        bool IsActionActive(string action, Thing plr)
        {
            KeyboardState? keyboardState = null;

            for (int plrIx = 0; plrIx < players.Count; plrIx++)
            {
                keyboardState = inputStates[plrIx].Item1;
                if (plr == players[plrIx])
                {
                    switch (action)
                    {
                        case "accelerate":
                            switch (plrIx)
                            {
                                case 0:
                                    if (keyboardState != null)
                                    {
                                        bool isKeyDown = (bool)keyboardState?.IsKeyDown(Keys.Up);   // Player 1 accelerate
                                        return isKeyDown;
                                    }
                                    else
                                        return false;
                                case 1:
                                    if (keyboardState != null)
                                    {
                                        bool isKeyDown = (bool)keyboardState?.IsKeyDown(Keys.Up);   // Player 2 accelerate
                                        return isKeyDown;
                                    }
                                    else
                                        return false;
                                case 2:
                                    if (keyboardState != null)
                                    {
                                        bool isKeyDown = (bool)keyboardState?.IsKeyDown(Keys.Up);   // Player 3 accelerate
                                        return isKeyDown;
                                    }
                                    else
                                        return false;
                                case 3:
                                    if (keyboardState != null)
                                    {
                                        bool isKeyDown = (bool)keyboardState?.IsKeyDown(Keys.Up);   // Player 4 accelerate
                                        return isKeyDown;
                                    }
                                    else
                                        return false;
                                default:
                                    return false;
                            }
                        case "turnleft":
                            switch (plrIx)
                            {
                                case 0:
                                    if (keyboardState != null)
                                    {
                                        bool isKeyDown = (bool)keyboardState?.IsKeyDown(Keys.Left);   // Player 1 turnleft
                                        return isKeyDown;
                                    }
                                    else
                                        return false;
                                    //return Keyboard.GetState().IsKeyDown(Keys.Left);   // Player 1 turnleft
                                case 1:
                                    if (keyboardState != null)
                                    {
                                        bool isKeyDown = (bool)keyboardState?.IsKeyDown(Keys.Left);   // Player 2 turnleft
                                        return isKeyDown;
                                    }
                                    else
                                        return false;
                                    //return Keyboard.GetState().IsKeyDown(Keys.A);      // Player 2 turnleft
                                case 2:
                                    if (keyboardState != null)
                                    {
                                        bool isKeyDown = (bool)keyboardState?.IsKeyDown(Keys.Left);   // Player 3 turnleft
                                        return isKeyDown;
                                    }
                                    else
                                        return false;
                                //return Keyboard.GetState().IsKeyDown(Keys.Left);   // Player 3 turnleft
                                case 3:
                                    if (keyboardState != null)
                                    {
                                        bool isKeyDown = (bool)keyboardState?.IsKeyDown(Keys.Left);   // Player 4 turnleft
                                        return isKeyDown;
                                    }
                                    else
                                        return false;
                                //return Keyboard.GetState().IsKeyDown(Keys.Left);   // Player 4 turnleft
                                default:
                                    return false;
                            }
                        case "turnright":
                            switch (plrIx)
                            {
                                case 0:
                                    if (keyboardState != null)
                                    {
                                        bool isKeyDown = (bool)keyboardState?.IsKeyDown(Keys.Right);   // Player 1 turnleft
                                        return isKeyDown;
                                    }
                                    else
                                        return false;
                                    //return Keyboard.GetState().IsKeyDown(Keys.Right); // Player 1 turnright
                                case 1:
                                    if (keyboardState != null)
                                    {
                                        bool isKeyDown = (bool)keyboardState?.IsKeyDown(Keys.Right);   // Player 2 turnright
                                        return isKeyDown;
                                    }
                                    else
                                        return false;
                                //return Keyboard.GetState().IsKeyDown(Keys.D);    // Player 2 turnright
                                case 2:
                                    if (keyboardState != null)
                                    {
                                        bool isKeyDown = (bool)keyboardState?.IsKeyDown(Keys.Right);   // Player 3 turnright
                                        return isKeyDown;
                                    }
                                    else
                                        return false;
                                case 3:
                                    if (keyboardState != null)
                                    {
                                        bool isKeyDown = (bool)keyboardState?.IsKeyDown(Keys.Right);   // Player 4 turnright
                                        return isKeyDown;
                                    }
                                    else
                                        return false;
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

