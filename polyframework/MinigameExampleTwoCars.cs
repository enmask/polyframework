﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;                 // For Debug.WriteLine

namespace polyframework
{
    public class MinigameExampleTwoCars : Minigame_Base.MinigameBase
    {
        //
        // Constants
        //
        // Game constants
        const string MINIGAME_NAME = "SmallGame";
        const string LEVEL_NAME = "Level1";

        // Car settings constants
        const string CAR_PICTURE_NAME = "graycar";

        //
        // Instance variables
        //
        Texture2D plrTex;

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
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load the player texture
            plrTex = Content.Load<Texture2D>(CAR_PICTURE_NAME);
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
