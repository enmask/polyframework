using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Collision.Shapes;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using static Minigame_Base.MinigameBase;

//namespace PolyFrameworkVt24
namespace polyframework
{
    public class CarRace : Minigame_Base.MinigameBase
    {
        //
        // Constants
        //
        const string MINIGAME_NAME = "CarRace";
        const string LEVEL_NAME = "Level1";
        const string CAR_PICTURE_NAME = "TwoCars/Pictures/graycar";

        // Level settings
        //readonly Vector2 TILE_OFFSET = new Vector2(5.5f, 5.3f);

        // Car settings
        const float CAR_ACCELERATION = 0.04f;        // Normal: 0.04
        const float CAR_TURN_POWER = 0.02f;          //         0.02
        const float CAR_ANGULAR_DAMPING = 5.5f;       //         5.5
        const float CAR_STRAIGHTEN_POWER = 0.004f;    //         0.004
        const float CAR_RADIAL_DAMPING = 0.04f;       //         0.04
        const float CAR_TANGENTIAL_DAMPING = 0.003f;  //         0.003

        // Player settings
        readonly Vector2 PLR1_STARTPOS = new Vector2(5.2f, 0.8f);
        readonly Vector2 PLR2_STARTPOS = new Vector2(5.2f, 1.6f);
        readonly Vector2 PLR3_STARTPOS = new Vector2(4.0f, 0.8f);
        readonly Vector2 PLR4_STARTPOS = new Vector2(4.0f, 1.6f);
        readonly Color PLR1_COLOR = Color.Red;
        readonly Color PLR2_COLOR = new Color(0, 255, 0, 255);
        readonly Color PLR3_COLOR = new Color(0, 0, 255, 255);
        readonly Color PLR4_COLOR = new Color(255, 255, 0, 255);

        //
        // Instance variables
        //
        Texture2D plrTex;
        Thing plr1;
        Thing plr2;
        Thing plr3;
        Thing plr4;
        List<Thing> players;
        bool plr1IsInGoal;
        bool plr1WasInGoal;
        SpriteFont font;
        Core.Timer car1Timer;
        Core.Timer car2Timer;

        public CarRace()
        {
            // Don't use fullscreen when debugging, because drawing can stop working then
            if (!Debugger.IsAttached)
            {
                _graphics.IsFullScreen = true;
                _graphics.ApplyChanges();
            }
        }

        protected override void Initialize()
        {
            //frameNo = 0;
            plr1IsInGoal = false;
            plr1WasInGoal = false;

            base.Initialize();

            // Add red, green, blue, and yellow player
            plr1 = AddPlayer(PLR1_COLOR, PLR1_STARTPOS);
            plr2 = AddPlayer(PLR2_COLOR, PLR2_STARTPOS);
            plr3 = AddPlayer(PLR3_COLOR, PLR3_STARTPOS);
            plr4 = AddPlayer(PLR4_COLOR, PLR4_STARTPOS);

            players = new List<Thing>();
            players.Add(plr1);
            players.Add(plr2);
            players.Add(plr3);
            players.Add(plr4);


            car1Timer = AddTimer();
            car2Timer = AddTimer();

            // This is a top-down game, so turn off gravity
            world.Gravity = new Vector2(0, 0);
        }

        protected override void LoadContent()
        {
            // Create a SpriteBatch that can be used to draw textures and text in the game window
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load the font
            font = Content.Load<SpriteFont>("TwoCars/Fonts/font");

            // Load the player texture
            plrTex = Content.Load<Texture2D>(CAR_PICTURE_NAME);

            // Load level (textures, colliders, and map)
            string levelFolder = Path.Combine(MINIGAME_NAME, LEVEL_NAME);
            base.LoadLevel(Content.RootDirectory, levelFolder);
        }

        protected override void Update(GameTime gameTime)
        {
            // Get the server user input from keyboard etc, and the client user input from the network.
            PrepareUserInput();

            // Check if plr1 reached goal
            plr1IsInGoal = Plr1ReachedGoal();

            if ((!plr1WasInGoal && plr1IsInGoal) || Keyboard.GetState().IsKeyDown(Keys.T))
                car1Timer.CaptureTime();

            // Reset plr1 and timer
            if (Keyboard.GetState().IsKeyDown(Keys.D1))
            {
                plr1.body.Position = PLR1_STARTPOS;
                plr1.body.LinearVelocity = new Vector2(0, 0);
                plr1.body.Rotation = 0;
                car1Timer.CaptureTime();
            }

            foreach (Thing plr in players)
                ApplyUserInput(plr);

            ApplyRoadGrip(plr1);
            ApplyRoadGrip(plr2);
            ApplyRoadGrip(plr3);
            ApplyRoadGrip(plr4);

            plr1WasInGoal = plr1IsInGoal;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            // Show total time in-game
            string totalStr = $"Total time: {gameTime.TotalGameTime.TotalSeconds:F2} s";
            _spriteBatch.DrawString(font, totalStr, new Vector2(170, 55), Color.White);
            // Show plr1 position
            string posStr = $"pos: {plr1.body.Position}";
            _spriteBatch.DrawString(font, posStr, new Vector2(170, 135), Color.White);

            _spriteBatch.End();

            // This call takes care of drawing all the Thing:s
            base.Draw(gameTime);
        }

        void ApplyUserInput(Thing plr)
        {
            var gamePadState = GamePad.GetState(PlayerIndex.One);

            // Accelerate car
            if (IsActionActive("accelerate", plr))
            {
                float rotation = plr.body.Rotation;
                Vector2 direction = new Vector2((float)System.Math.Cos(rotation), (float)System.Math.Sin(rotation));
                Vector2 impulse = direction * CAR_ACCELERATION;
                plr.body.ApplyLinearImpulse(impulse);
            }

            // Brake car
            if (IsActionActive("brake", plr))
            {
                float rotation = plr.body.Rotation;
                Vector2 direction = new Vector2((float)System.Math.Cos(rotation), (float)System.Math.Sin(rotation));
                Vector2 impulse = -direction * CAR_ACCELERATION;
                plr.body.ApplyLinearImpulse(impulse);
            }

            // Turn left using keyboard
            if (IsActionActive("turnleft", plr))
                plr.body.ApplyAngularImpulse(CAR_TURN_POWER);

            // Turn right using keyboard
            if (IsActionActive("turnright", plr))
                plr.body.ApplyAngularImpulse(-CAR_TURN_POWER);

            // Turn left/right using gamepad thumbstick
            plr.body.ApplyAngularImpulse(CAR_TURN_POWER * GetActionValue());
        }

        void ApplyRoadGrip(Thing plr)
        {
            // Calculate the diff between orientation and linear velocity
            float diff = plr.body.Rotation - (float)System.Math.Atan2(plr.body.LinearVelocity.Y, plr.body.LinearVelocity.X);
            diff = NormalizeAngle(diff);

            if (plr.body.LinearVelocity.Length() > 0.1f)
            {
                if (diff >= -System.Math.PI / 2 && diff <= System.Math.PI / 2)
                {
                    // Apply an angular force proportional to the diff
                    plr.body.ApplyAngularImpulse(-diff * CAR_STRAIGHTEN_POWER);
                }
                else if (diff < -System.Math.PI / 2)
                    plr.body.ApplyAngularImpulse(diff * CAR_STRAIGHTEN_POWER);
                else
                    plr.body.ApplyAngularImpulse(diff * CAR_STRAIGHTEN_POWER);

                // Hämta bilens totala hastighet
                Vector2 velocity = plr.body.LinearVelocity;

                // Beräkna enhetsvektorn för bilens framåtriktning baserat på rotationen
                Vector2 forwardDirection = new Vector2((float)System.Math.Cos(plr.body.Rotation), (float)System.Math.Sin(plr.body.Rotation));

                // Beräkna tangentiell hastighet (komponenten av hastigheten i bilens framåtriktning)
                float tangentialVelocityMagnitude = Vector2.Dot(velocity, forwardDirection);
                Vector2 tangentialVelocity = tangentialVelocityMagnitude * forwardDirection;

                // Beräkna radiell hastighet (komponenten vinkelrät mot bilens framåtriktning)
                Vector2 rightDirection = new Vector2(-forwardDirection.Y, forwardDirection.X); // 90 grader roterad framåtriktning
                float radialVelocityMagnitude = Vector2.Dot(velocity, rightDirection);
                Vector2 radialVelocity = radialVelocityMagnitude * rightDirection;

                // Beräkna friktionsimpulser
                Vector2 frictionImpulseTangential = -CAR_TANGENTIAL_DAMPING * tangentialVelocity;
                Vector2 frictionImpulseRadial = -CAR_RADIAL_DAMPING * radialVelocity;

                // Applicera friktionsimpulserna på bilens masscentrum
                plr.body.ApplyLinearImpulse(frictionImpulseTangential + frictionImpulseRadial, plr.body.WorldCenter);
            }
        }

        bool IsActionActive(string action, Thing plr)
        {
            KeyboardState keyboardState;
            GamePadState? gamePadState;

            for (int plrIx = 0; plrIx < players.Count; plrIx++)
            {
                if (plr == players[plrIx])
                {
                    keyboardState = inputStates[plrIx].Item1;
                    gamePadState = inputStates[plrIx].Item3;

                    switch (action)
                    {
                        case "accelerate":
                            return keyboardState.IsKeyDown(Keys.Up) ||
                                   gamePadState?.Buttons.A == ButtonState.Pressed;    // Player 1-4 accelerate
                        case "brake":
                            return keyboardState.IsKeyDown(Keys.Down) ||
                                   gamePadState?.Buttons.X == ButtonState.Pressed;    // Player 1-4 brake
                        case "turnleft":
                            return keyboardState.IsKeyDown(Keys.Left) ||
                            gamePadState?.DPad.Left == ButtonState.Pressed;         // Player 1-4 turnleft
                        case "turnright":
                            return keyboardState.IsKeyDown(Keys.Right) ||         // Player 1-4 turnright
                            gamePadState?.DPad.Right == ButtonState.Pressed;         // Player 1-4 turnleft
                    }
                }
            }
            return false;
        }

        float GetActionValue()
        {
            return 0.0f;
        }

        bool Plr1ReachedGoal()
        {
            return (plr1.body.Position.X > 7.0f && plr1.body.Position.X < 8.0f &&
                    plr1.body.Position.Y > 0.0f && plr1.body.Position.Y < 2.0f);
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
                                originY: plrTex.Height / 2,
                                angularDamping: CAR_ANGULAR_DAMPING);

            t.body.LinearDamping = 0.0f;
            AddThing(t);
            return t;
        }

        float NormalizeAngle(float angle)
        {
            while (angle > System.Math.PI)
                angle -= 2 * (float)System.Math.PI;
            while (angle < -System.Math.PI)
                angle += 2 * (float)System.Math.PI;
            return angle;
        }

    } // End of class CarRace

} // End of namespace PolyFrameworkVt24
