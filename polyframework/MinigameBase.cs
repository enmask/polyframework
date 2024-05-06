using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
//using SharpDX.Direct2D1.Effects;
using System.Collections.Generic;
using System.Diagnostics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Collision.Shapes;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading;
//using System.Reflection.Metadata.Ecma335;
//using System.Net.Mail;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Minigame_Base
{
    public class MinigameBase : Game
    {
        //
        // Constants
        //
        const int WINDOW_WIDTH = 1900;
        const int WINDOW_HEIGHT = 1080;
        protected const float SCALE = 128.0f;

        const float SIDE_IMPULSE = 0.1f;
        const float MAX_WALK_SPEED = 50.0f;

        public const float DEFAULT_BASE_LINEAR_DAMPING = 0.1f;
        public const float DEFAULT_EXTRA_LINEAR_DAMPING = 0.0f;
        public const float DEFAULT_ANGULAR_DAMPING = 1.0f;
        public const float DEFAULT_FRICTION = 0.05f;
        public const float DEFAULT_RESTITUTION = 0.3f;
        public readonly Color DEFAULT_TINT = Color.White;

        const float HORIZONTAL_FORCE_TO_MIDDLE_FACTOR = 0.0f;  // Was 0.0001f form Shuffleboard
        const float VERTICAL_FORCE_TO_MIDDLE_FACTOR = 0.0f; // Was 0.00035f for Shuffleboard

        // TODO: Maybe move these to Networking.cs
        const char OBJECT_SEPARATOR = '#';
        const char FIELD_SEPARATOR = ';';
        const string SPECIFIER_THING = "T";

        // These constants are used when sending draw data and client input over the network
        const int DRAWDATA_INDEX_SPECIFIER = 0;
        const int DRAWDATA_INDEX_TEXTURE_INDEX = 1;
        const int DRAWDATA_INDEX_XPOS = 2;
        const int DRAWDATA_INDEX_YPOS = 3;
        const int DRAWDATA_INDEX_COLOR = 4;
        const int DRAWDATA_INDEX_ROTATION = 5;
        const int DRAWDATA_INDEX_XORIGIN = 6;  // Added in the commit 5fc7cdd
        const int DRAWDATA_INDEX_YORIGIN = 7;  // Added in the commit 5fc7cdd
        const int DRAWDATA_INDEX_XSCALE = 8;   // Added in the commit 5fc7cdd
        const int DRAWDATA_INDEX_YSCALE = 9;  // Added in the commit 5fc7cdd

        const int CLIENTINPUT_INDEX_KEYS = 1;            // TODO: Not used yet
        const int CLIENTINPUT_INDEX_GAMEPAD_BUTTONS = 2; // TODO: Not used yet

        const int NUM_DECIMALS_POSITION = 2;
        const int NUM_DECIMALS_ROTATION = 2;
        const int NUM_DECIMALS_SCALE = 2;

        // Sometimes drawdata arrives late. Max time to wait for it in milliseconds.ä
        const int MAX_WAIT_TIME = 20;

        //
        // Instance variables
        //
        protected GraphicsDeviceManager _graphics;
        protected SpriteBatch _spriteBatch;

        // TODO: Maybe change to private
        protected int clientNo;
        protected World world;

        // The dictionary of things in the game
        public Dictionary<int, Thing> things;
        // The dictionary of the textures in the game
        private Dictionary<int, Texture2D> textures;
        private static int nextTextureId = 0;

        Dictionary<string, List<Shape>> colliderDict;

        readonly Vector2 TILE_OFFSET = new Vector2(5.45f, 5.23f);

        protected List<Core.Timer> timers = new List<Core.Timer>();

        //protected List<KeyboardState?> inputStates;
        protected List<(KeyboardState?, MouseState?, GamePadState?)> inputStates;

        int updateNo;
        int frameNo;

        // FPS-beräkning
        int updateCounter;
        int frameCounter;
        protected int updateRate;
        protected int frameRate;
        TimeSpan updateElapsedTime;
        TimeSpan frameElapsedTime;

        bool lWasDown;

        public MinigameBase()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = WINDOW_WIDTH;
            _graphics.PreferredBackBufferHeight = WINDOW_HEIGHT;

            /* Turn off fixed timestep and vsync for stress tests.
            IsFixedTimeStep = false;
            _graphics.SynchronizeWithVerticalRetrace = false;
            */

            _graphics.ApplyChanges();
        }

        public void Run(int clientNo)
        {
            Debug.WriteLine("MinigameBase Run called, clientNo = " + clientNo);
            this.clientNo = clientNo;
            base.Run();
        }


        protected override void Initialize()
        {
            world = new World();
            things = new Dictionary<int, Thing>();
            textures = new Dictionary<int, Texture2D>();

            // Dictionaries for looking up a loaded texture or shape
            colliderDict = new Dictionary<string, List<Shape>>();

            InitializeTextureMap();

            inputStates = new List<(KeyboardState?, MouseState?, GamePadState?)>();

            // TODO: Quick and dirty, just to see if the networking works. Remove!
            inputStates.Add((null, null, null));
            inputStates.Add((null, null, null));
            inputStates.Add((null, null, null));
            inputStates.Add((null, null, null));

            updateNo = 0;
            frameNo = 0;

            updateCounter = 0;
            frameCounter = 0;
            updateRate = 0;
            frameRate = 0;
            updateElapsedTime = TimeSpan.Zero;
            frameElapsedTime = TimeSpan.Zero;

            lWasDown = false;

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            Core.Tools.Log("\nTime: " + System.DateTime.Now.ToString("HHmmssfff") + "  Update START (isServer=" + IsServer() + ")" +
                           ", updateNo = " + updateNo + ", frameNo = " + frameNo);

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (!lWasDown && Keyboard.GetState().IsKeyDown(Keys.L))
            {
                Core.Tools.WriteLogListToFile();
                lWasDown = true;
            }
            else
                lWasDown = false;

            updateNo++;
            updateCounter++;
            UpdateFrequency(ref updateCounter, ref updateRate, ref updateElapsedTime, gameTime);

            // Update all timers that the minigame has added
            foreach (var timer in timers)
                timer.Update(gameTime.ElapsedGameTime.TotalSeconds);

            world.Step((float)gameTime.ElapsedGameTime.TotalSeconds * 1.0f);

            base.Update(gameTime);

            Core.Tools.Log("Time: " + System.DateTime.Now.ToString("HHmmssfff") + "  Update END (isServer=" + IsServer() + ")" +
                           ", updateNo = " + updateNo + ", frameNo = " + frameNo + "\n");
        }

        protected override void Draw(GameTime gameTime)
        {
            Core.Tools.Log("\nTime: " + System.DateTime.Now.ToString("HHmmssfff") + "  Draw START (isServer=" + IsServer() + ")" +
                  ", updateNo = " + updateNo + ", frameNo = " + frameNo);

            frameNo++;
            frameCounter++;
            UpdateFrequency(ref frameCounter, ref frameRate, ref frameElapsedTime, gameTime);

            _spriteBatch.Begin();

            if (IsServer())
            {
                string frameDrawData = "";

                // The server draws all things and sends draw data to the clients
                foreach (KeyValuePair<int, Thing> kvp in things)
                {
                    Thing thing = kvp.Value;

                    if (!thing.isVisible)
                        continue;
                    DrawThing(thing);
                    string thingDrawData = SerializeThing(thing);
                    frameDrawData += thingDrawData;
                }

                if (frameDrawData.Length > 0)
                {
                    string msgPrefix = PolyNetworking.Networking.SerializeMessagePrefix(clientNo.ToString());
                    frameDrawData = PolyNetworking.Networking.JoinMessageSections(msgPrefix, frameDrawData);
                    PolyNetworking.Networking.SendDrawData(frameDrawData);

                    Debug.WriteLine("Server sending frameDrawData: " + frameDrawData);
                    Core.Tools.Log("Time: " + System.DateTime.Now.ToString("HHmmssfff") +
                                   "  Draw: Server sends frameDrawData: <" + frameDrawData + ">");
                }
            }
            else
            {
                // The clients receive draw data from the server and draw the things
                // Get the latest drawData that has been received from the server
                string drawData = PolyNetworking.Networking.GetReceivedDrawData();

                bool dataReady = drawData is not null && drawData.Length > 0;
                if (!dataReady)
                {
                    Stopwatch timer = Stopwatch.StartNew();
                    while (!dataReady && timer.ElapsedMilliseconds < MAX_WAIT_TIME)
                    {
                        Thread.Sleep(1); // Väntar en kort tid för att kontrollera igen
                        drawData = PolyNetworking.Networking.GetReceivedDrawData();
                        dataReady = drawData is not null && drawData.Length > 0;
                    }
                }

                if (drawData is not null && drawData.Length > 0)
                {

                    string[] parts = drawData.Split('#');
                    string header = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        if (parts[i].Length == 0)
                            continue;
                        Thing t = DeserializeThing(parts[i]);
                        DrawThing(t, false);
                    }
                }
            }

            _spriteBatch.End();

            base.Draw(gameTime);

            Core.Tools.Log("Time: " + System.DateTime.Now.ToString("HHmmssfff") + "  Draw END (isServer=" + IsServer() + ")" +
                           ", updateNo = " + updateNo + ", frameNo = " + frameNo + "\n");
        }

        protected bool IsServer()
        {
            return clientNo == 0;
        }

        private void UpdateFrequency(ref int counter, ref int rate, ref TimeSpan elapsedTime, GameTime gameTime)
        {
            elapsedTime += gameTime.ElapsedGameTime;

            if (elapsedTime > TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);
                rate = counter;
                counter = 0;
            }
        }

        // HACK: When the client code calls this, it passes doConvertPos = false,
        // because those temporary things are already in screen coordinates.
        protected void DrawThing(Thing thing, bool doConvertPos = true)
        {
            Vector2 scrPos;
            if (doConvertPos)
                scrPos = ToScrPos(thing.body.Position);
            else
                scrPos = thing.body.Position;

            _spriteBatch.Draw(texture: thing.tex,
                                          position: scrPos,
                                          sourceRectangle: new Rectangle(0, 0, thing.tex.Width, thing.tex.Height),
                                          color: new Color(thing.drawTintRed, thing.drawTintGreen,
                                                           thing.drawTintBlue, thing.drawTintAlpha) /*Color.White*/,
                                          rotation: -thing.body.Rotation,
                                          origin: thing.origin,
                                          scale: thing.scale,
                                          effects: SpriteEffects.None,
                                          layerDepth: 0.0f);
        }

        protected void PrepareUserInput()
        {
            // Both server and clients detect local input and store it in inputStates
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
            inputStates[clientNo] = (keyboardState, null, gamePadState);

            // The client sends the input to the server
            if (!IsServer())
            {
                // Send the input triples to the clients
                SendClientInput(SerializeInput(inputStates[clientNo]));
            }
            // Create "fake" triples based on received client data
            DeserializeAndProcessNetworkInput();
        }

        private string SerializeInput((KeyboardState?, MouseState?, GamePadState?) triple)
        {




            // Get an array of pressed keys
            KeyboardState kState = triple.Item1 ?? Keyboard.GetState();
            var pressedKeys = kState.GetPressedKeys();

            // Serialize the array to a string
            string serializedKeys = string.Join(";", pressedKeys.Select(key => ((int)key).ToString("X")));

            // TODO: Only sends keyboard input for now, not mouse and gamepad
            var frameClientInput = serializedKeys + "#";

            if (frameClientInput.Length > 1)
            {

                frameClientInput = PolyNetworking.Networking.JoinMessageValues("K", frameClientInput);
                string msgPrefix = PolyNetworking.Networking.SerializeMessagePrefix(clientNo.ToString());
                frameClientInput = PolyNetworking.Networking.JoinMessageSections(msgPrefix, frameClientInput);

                /*
                // Add timestamp at the beginning of the message
                string timestamp = System.DateTime.Now.ToString("HHmmssfff");
                frameClientInput = timestamp + "#" + "K" + clientNo + ";" + frameClientInput;
                //SendClientInput(frameClientInput);
                */
            }

            return frameClientInput;
        }

        private void DeserializeAndProcessNetworkInput()
        {
            KeyboardState? kState = null;
            int clientNoOfInput = -1;

            // Get network data
            string networkData = PolyNetworking.Networking.GetReceivedClientsInput();

            // If there is no network data to proceass, return
            if (networkData is null || networkData.Length == 0)
                return;

            // Split the network data into an array of strings
            string[] networkDataArray = networkData.Split('#');

            bool isFirstPart = true;

            foreach (string data in networkDataArray)
            {
                if (isFirstPart)
                {
                    // Handle timestamp
                    string receivedTimestamp = data;
                    isFirstPart = false;
                    continue;
                }

                // Split the string into an array of values
                string[] values = data.Split(';');

                string specifier = values[0];

                // If there is no data, skip to the next iteration
                if (data.Length == 0)
                    continue;

                // If the specifier is "K" (for Keyboard), process keyboard data
                if (specifier[0] == 'K')
                {
                    clientNoOfInput = int.Parse(specifier.Substring(1, 1));

                    // Get the keyboard data
                    string keys = values[1];

                    // Create a HashSet<Keys> to store the pressed keys
                    HashSet<Keys> pressedKeys = new HashSet<Keys>();

                    for (int i = 0; i < keys.Length; i += 2)
                    {
                        // Convert the hex pair to a key and add it to the HashSet
                        Keys k = (Keys)int.Parse(keys.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                        pressedKeys.Add(k);
                    }

                    // Create a KeyboardState object based on the pressed keys
                    // TODO: Does ToArray() mean that we lose the performance benefits of a HashSet?
                    kState = new KeyboardState(pressedKeys.ToArray());

                    // Add the KeyboardState object to the list
                    inputStates[clientNoOfInput] = (kState, null, null);
                }
            }
            return;
        }

        void SendClientInput(string cInput)
        {
            PolyNetworking.Networking.SendClientInput(cInput);
        }

        protected string FormatValue(float value, int numDecimals)
            {
                // Round to specified number of decimals
                string formatted = value.ToString($"F{numDecimals}");

                // Remove unnecessary zeros and commas
                // Reverse index search from the end to remove zeros after the decimal point
                formatted = formatted.TrimEnd('0').TrimEnd(',');

                return formatted;
            }

        protected string SerializeThing(Thing thing)
        {
            return ThingToDrawData(thing);
        }

        protected string ThingToDrawData(Thing thing)
        {
            Vector2 scrPos = ToScrPos(thing.body.Position);
            //string timestamp = System.DateTime.Now.ToString("HHmmss");
            string colorHex = ColorToHex(thing.drawTintAlpha,
                                         thing.drawTintRed,
                                         thing.drawTintGreen,
                                         thing.drawTintBlue);

            string formattedX = FormatValue(scrPos.X, NUM_DECIMALS_POSITION);
            string formattedY = FormatValue(scrPos.Y, NUM_DECIMALS_POSITION);
            string formattedRotation = FormatValue(thing.body.Rotation, NUM_DECIMALS_ROTATION);
            string formattedScaleX = FormatValue(thing.scale.X, NUM_DECIMALS_SCALE);
            string formattedScaleY = FormatValue(thing.scale.Y, NUM_DECIMALS_SCALE);

            string str = SPECIFIER_THING + ";" +
                         GetTextureIndex(thing.tex.Name) + ";" +
                         formattedX + ";" + formattedY + ";" +
                         colorHex + ";" +
                         formattedRotation + ";" +
                         thing.origin.X + ";" +
                         thing.origin.Y + ";" +
                         formattedScaleX + ";" + formattedScaleY + "#";

            return str;
        }

        protected Thing DeserializeThing(string thingString)
        {
            // Split the thingString into an array of values
            string[] values = thingString.Split(';');

            // Get the texture
            Texture2D tex = GetTexture(int.Parse(values[DRAWDATA_INDEX_TEXTURE_INDEX]));

            // Get the position
            Vector2 pos = new Vector2(float.Parse(values[DRAWDATA_INDEX_XPOS]),
                                                     float.Parse(values[DRAWDATA_INDEX_YPOS]));

            // Get the color
            Color color = HexToColor(values[DRAWDATA_INDEX_COLOR]);

            // Get the rotation
            float rotation = float.Parse(values[DRAWDATA_INDEX_ROTATION]);

            // Get the origin
            Vector2 origin = new Vector2(float.Parse(values[DRAWDATA_INDEX_XORIGIN]),
                                                       float.Parse(values[DRAWDATA_INDEX_YORIGIN]));

            // Get the scale
            Vector2 scale = new Vector2(float.Parse(values[DRAWDATA_INDEX_XSCALE]),
                                                      float.Parse(values[DRAWDATA_INDEX_YSCALE]));
            
            // Create a new Thing object
            Thing thing = new Thing(world, tex, pos, BodyType.Static, true,
                                    drawTintAlpha: color.A, drawTintRed: color.R,
                                    drawTintGreen: color.G, drawTintBlue: color.B,
                                    rot: rotation,
                                    originX: origin.X,
                                    originY: origin.Y,
                                    scaleX: scale.X,
                                    scaleY: scale.Y);
            return thing;
        }

        // Mapping functions for textures
        //   int GetTextureIndex(Texture2D texture)    SKIP
        //   int GetTextureIndex(string textureName)   DONE
        //   int GetTexture(int textureIndex)          DONE
        //   int GetTexture(string textureName)        LATER
        //   int GetTextureName(string textureIndex)   SKIP
        //   int GetTextureName(string texture)        SKIP
        int GetTextureIndex(string textureName)
        {
            foreach (var item in textures)
            {
                if (item.Value.Name == textureName)
                    return item.Key;
            }
            throw new System.ArgumentException("Texture " + textureName + " not found in textures", nameof(textureName));
        }

        Texture2D GetTexture(int textureIndex)
        {
            return textures[textureIndex];
        }

        public static string ColorToHex(int alpha, int red, int green, int blue)
        {
            return alpha.ToString("X2") + red.ToString("X2") + green.ToString("X2") + blue.ToString("X2");
        }

        public static Color HexToColor(string hex)
        {
            if (hex.Length != 8)
            {
                throw new System.ArgumentException("Hex must be 8 characters long", nameof(hex));
            }

            int alpha = System.Convert.ToInt32(hex.Substring(0, 2), 16);
            int red = System.Convert.ToInt32(hex.Substring(2, 2), 16);
            int green = System.Convert.ToInt32(hex.Substring(4, 2), 16);
            int blue = System.Convert.ToInt32(hex.Substring(6, 2), 16);

            return new Color(red, green, blue, alpha);
        }

        protected void InitializeTextureMap()
        {
            string rootDirectory = Content.RootDirectory;
            string contentFullPath = System.Environment.CurrentDirectory + "/" + rootDirectory;

            // Build a mapping between texture indices and texture names
            var textureMapping = BuildTextureMapping(rootDirectory);
        }
        
        static Dictionary<int, string> BuildTextureMapping(string rootDirectory)
        {
            var textureFiles = Directory.GetFiles(rootDirectory, "*.xnb", SearchOption.AllDirectories);
            var mapping = new Dictionary<int, string>();

            int index = 0;
            foreach (var filePath in textureFiles)
            {
                string relativePath = Path.GetRelativePath(rootDirectory, filePath);

                // Replace backslashes with forward slashes
                relativePath = relativePath.Replace('\\', '/');

                mapping.Add(index, relativePath);
                index++;
            }

            return mapping;
        }

        static Dictionary<int, string> BuildIndexMapping(string rootDirectory)
        {
            var files = Directory.GetFiles(rootDirectory, "*.*", SearchOption.AllDirectories)
                        .Select(filePath => Path.GetRelativePath(rootDirectory, filePath)) // Make paths relative
                        .OrderBy(name => name) // Sort alphabetically
                        .ToList();

            var indexMapping = new Dictionary<int, string>();

            for (int i = 0; i < files.Count; i++)
            {
                string nameWithoutExtension = Path.ChangeExtension(files[i], null); // Remove .xnb file suffix
                indexMapping[i] = nameWithoutExtension.Replace('\\', '/');
            }

            return indexMapping;
        }

        protected void LoadLevel(string contentFolder, string levelFolder)
        {
            LevelData loadedLevel = null;
            // This dictionary is needed for a while, until it's time to build the shapes
            var collidersDataDict = new Dictionary<string, Core.CollidersData>();
            var allContentFiles = Directory.GetFiles(contentFolder + "/" + levelFolder, "*.*", SearchOption.AllDirectories);
            var normalizedPaths = allContentFiles.Select(path => path.Replace("\\", "/")).ToArray();

            // Load content files for pictures, colliders, and level
            foreach (var file in normalizedPaths)
            {
                var extension = Path.GetExtension(file).ToLower();

                // Get the path, e.g. "CarRace/Colliders/piece1.json"
                var assetPath = Path.GetRelativePath(Content.RootDirectory, file).Replace('\\', '/');

                // Remove file extension. E.g. change "CarRace/Colliders/piece1.json" to "CarRace/Colliders/piece1"
                assetPath = Path.ChangeExtension(assetPath, null);

                // Remove everything but the asset name. E.g. change "CarRace/Colliders/piece1" to "piece1"
                string assetName = assetPath.Substring(assetPath.LastIndexOf('/') + 1);

                // Pictures (.png, .jpg etc) have been build into .xnb using the MGCB (MonoGame Content Builder) tool
                if (extension == ".xnb")
                {
                    if (assetPath.Contains("/Pictures/") || assetPath.Contains("\\Pictures\\"))
                    {
                        var texture = Content.Load<Texture2D>(assetPath);
                        Debug.WriteLine("Loaded texture: " + assetPath);
                    }

                }
                // Handle .json files directly, they are not built to .xnb files
                else if (extension == ".json")
                {
                    // Collider .json files are in the "Colliders" subfolder
                    if (file.Contains("/Colliders/") || file.Contains("\\Colliders\\"))
                    {
                        Core.CollidersData collidersData = Core.ColliderManager.CreateColliderFromJson(File.ReadAllText(file));
                        collidersDataDict[assetName] = collidersData;
                    }
                    // The level .json files is directly in the level folder
                    else
                        loadedLevel = LoadLevelMap(assetPath);
                }
            }

            // Now that all textures are loaded, we can build the actual shapes for the loaded colliders
            // and build the level.
            BuildColliderShapes(collidersDataDict);
            BuildLevel(loadedLevel);
        }

        void BuildColliderShapes(Dictionary<string, Core.CollidersData> collidersDataDict)
        {
            foreach (var objName in collidersDataDict.Keys)
            {
                BuildThingColliderShapes(objName, collidersDataDict[objName]);
            }
        }

        void BuildThingColliderShapes(string objName, Core.CollidersData collidersData)
        {
            // TODO: This call is untested, need to load a tilemap for that
            Texture2D tex = GetTexture(GetTextureIndex(objName));
            colliderDict[objName] = Core.ColliderManager.CreateShapesFromCollidersData(collidersData, world, tex.Width, tex.Height, Minigame_Base.MinigameBase.SCALE);
        }

        protected LevelData LoadLevelMap(string assetPath)
        {
            string levelFilePath = Content.RootDirectory + "/" + assetPath + ".json";
            LevelData loadedLevel = LevelMapLoader.LoadLevelMap(levelFilePath);
            return loadedLevel;
        }

        void BuildLevel(LevelData loadedLevel)
        {
            // Loop over all GroundTiles in the loaded LevelData
            foreach (var tile in loadedLevel.GroundTiles)
                AddLevelThing(tile.TileName, tile.Position.ToVector2() /**/ + TILE_OFFSET /**/, 0.0f);
        }

        Thing AddLevelThing(string trackPieceName, Vector2 wallPos, float wallRot)
        {
            // TODO: This call is untested, need to load a tilemap for that
            Texture2D tex = GetTexture(GetTextureIndex(trackPieceName));

            Thing t = new Thing(world,
                                tex,
                                wallPos,
                                BodyType.Static,
                                isVisible: true,
                                bodyShape: BodyShape.Custom,
                                customShapes: colliderDict[trackPieceName],
                                originX: tex.Width / 2,
                                originY: tex.Height / 2,
                                rot: wallRot);

            AddThing(t);
            return t;
        }

        //
        // API for Minigame creation
        //

        public void AddThing(Thing thing)
        {
            things.Add(thing.id, thing);
            if (!textures.ContainsValue(thing.tex))
                textures.Add(nextTextureId++, thing.tex);
        }

        public void RemoveThing(Thing thing)
        {
            world.Remove(thing.body);
            things.Remove(thing.id);
        }

        public Thing GetThing(int id)
        {
            return things[id];
        }

        Vector2 ToScrPos(Vector2 physPos)
        {
            float scrX = SCALE * physPos.X;
            float scrY = WINDOW_HEIGHT - SCALE * physPos.Y;
            return new Vector2(scrX, scrY);
        }

        Vector2 ToPhysPos(Vector2 scrPos, int scrW, int scrH)
        {
            float physX = (scrPos.X) / SCALE;
            float physY = (WINDOW_HEIGHT - scrPos.Y) / SCALE;
            return new Vector2(physX, physY);
        }

        protected Core.Timer AddTimer()
        {
            var timer = new Core.Timer();
            timers.Add(timer);
            return timer;
        }

        //
        // Inner classes
        //
        public class Thing
        {
            private static int nextId = 0;
            public int id;
            public int group;
            public Body body;
            public Texture2D tex;
            public bool isVisible;
            public bool hasFallen;
            public float baseLinearDamping;
            public float extraLinearDamping;
            public Vector2 origin;
            public Vector2 scale;
            public int drawTintAlpha;
            public int drawTintRed;
            public int drawTintGreen;
            public int drawTintBlue;

            public Thing(World world,
                         Texture2D tex,
                         Vector2 pos,
                         BodyType bodyType,
                         bool isVisible,
                         int group = 0, // A way for the minigame coder to group the things
                         BodyShape bodyShape = BodyShape.Rectangle,
                         List<Shape> customShapes = null,
                         bool isSensor = false,
                         float baseLinearDamping = DEFAULT_BASE_LINEAR_DAMPING,
                         float extraLinearDamping = DEFAULT_EXTRA_LINEAR_DAMPING,
                         float angularDamping = DEFAULT_ANGULAR_DAMPING,
                         float friction = DEFAULT_FRICTION,
                         float restitution = DEFAULT_RESTITUTION,
                         float rot = 0.0f,
                         float originX = 0.0f,
                         float originY = 0.0f,
                         float scaleX = 1.0f,
                         float scaleY = 1.0f,
                         int drawTintAlpha = 255,
                         int drawTintRed = 255,
                         int drawTintGreen = 255,
                         int drawTintBlue = 255)
            {
                this.id = nextId;
                nextId++;

                Body b;
                if (bodyShape == BodyShape.Rectangle)
                {
                    b = world.CreateRectangle(tex.Width / SCALE,
                                              tex.Height / SCALE,
                                              density: 1.0f,
                                              pos,
                                              rot,
                                              bodyType);
                }
                else if (bodyShape == BodyShape.Circle)
                {
                    b = world.CreateCircle(radius: 0.5f * tex.Width / SCALE, density: 1.0f, pos, bodyType);
                }
                else
                {
                    // TODO: Tiles are never rotated, but can be if game is "sprite based"
                    b = world.CreateBody(pos, 0.0f, bodyType);
                    foreach (Shape shape in customShapes)
                        b.CreateFixture(shape);
                }

                // Store id in body.Tag. This makes it easier to find the Thing
                // given a body, for example in a collision handler.
                b.Tag = id;

                this.isVisible = isVisible;
                this.hasFallen = false;
                this.group = group;

                this.tex = tex;
                SetIsSensor(b, isSensor);

                this.baseLinearDamping = baseLinearDamping;
                this.extraLinearDamping = extraLinearDamping;
                b.AngularDamping = angularDamping;
                SetFriction(b, friction);
                SetRestitution(b, restitution);
                this.body = b;

                this.origin = new Vector2(originX, originY);
                this.scale = new Vector2(scaleX, scaleY);

                this.drawTintAlpha = drawTintAlpha;
                this.drawTintRed = drawTintRed;
                this.drawTintGreen = drawTintGreen;
                this.drawTintBlue = drawTintBlue;
            }

            public override string ToString()
            {
                string ret = "";
                ret += "\nThing\n-----";
                ret += "id: " + id + "\n";
                ret += "pos: " + body.Position + "\n";
                return ret;
            }
        }

        static void SetIsSensor(Body b, bool value)
        {
            foreach (var fixture in b.FixtureList)
                fixture.IsSensor = value;
        }

        static void SetFriction(Body b, float value)
        {
            foreach (var fixture in b.FixtureList)
                fixture.Friction = value;
        }

        static void SetRestitution(Body b, float value)
        {
            foreach (var fixture in b.FixtureList)
                fixture.Restitution = value;
        }
    }

    public class LevelData
    {
        public int LevelIndex { get; set; }
        public List<TileData> GroundTiles { get; set; }
        public List<TileData> UnitTiles { get; set; }
    }

    public class TileData
    {
        public Position Position { get; set; }
        public string TileName;
    }

    public class Position
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        // Converts Position to MonoGame's Vector2
        public Vector2 ToVector2() => new Vector2(x, y);
    }

    public class LevelMapLoader
    {
        public static LevelData LoadLevelMap(string filePath)
        {
            // Read JSON string from file
            string json = File.ReadAllText(filePath);
            // Deserialize the JSON string
            LevelData levelData = JsonConvert.DeserializeObject<LevelData>(json);
            return levelData;
        }
    }

    public enum BodyShape
    {
        Rectangle,
        Circle,
        Custom
    }
}