using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct2D1.Effects;
using System.Collections.Generic;
using System.Diagnostics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Collision.Shapes;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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
            

        //
        // Instance variables
        //
        protected GraphicsDeviceManager _graphics;
        protected SpriteBatch _spriteBatch;

        private bool isServer;
        protected World world;

        // The dictionaries of things in the game
        public Dictionary<int, Thing> things;
        
        Dictionary<string, List<Shape>> colliderDict;
        Dictionary<string, Texture2D> textureDict;

        Dictionary<int, string> testIndexMapping;

        readonly Vector2 TILE_OFFSET = new Vector2(5.45f, 5.23f);

        protected List<Core.Timer> timers = new List<Core.Timer>();


        public MinigameBase()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = WINDOW_WIDTH;
            _graphics.PreferredBackBufferHeight = WINDOW_HEIGHT;
            _graphics.ApplyChanges();
        }

        public void Run(bool isServer)
        {
            Debug.WriteLine("MinigameBase Run called, isServer = " + isServer);
            this.isServer = isServer;
            base.Run();
        }


        protected override void Initialize()
        {
            world = new World();
            things = new Dictionary<int, Thing>();

            // Dictionaries for looking up a loaded texture or shape
            textureDict = new Dictionary<string, Texture2D>();
            colliderDict = new Dictionary<string, List<Shape>>();

            // TEST, right now this is only for building the texture map
            //string levelFolder = Path.Combine(MINIGAME_NAME, LEVEL_NAME);
            //LoadLevel(Content.RootDirectory, MINIGAME_NAME);
            // Better use this one, because it's more general
            InitializeTextureMap();

            // TEST
            testIndexMapping = BuildIndexMapping(Content.RootDirectory);

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {            
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
                
            foreach (KeyValuePair<int, Thing> kvp in things)
            {
                Thing thing = kvp.Value;

                // Controls
                switch (thing.controlType)
                {
                    case ControlType.None:
                        break;
                    case ControlType.UpDownLeftRight:
                        ActOnUpDownLeftRightInput(thing);
                        break;
                    case ControlType.UpDown:
                        ActOnUpDownInput(thing);
                        break;
                    case ControlType.LeftRight:
                        ActOnLeftRightInput(thing);
                        break;
                    case ControlType.Custom:
                        // Custom controls should be taken care of in the subclass
                        break;
                    default:
                        Debug.WriteLine("Error: ControlType " + thing.controlType + " not supported (or NYI), exiting!");
                        Exit();
                        break;
                }

                // Fix higher damping for lower speeds
                //thing.body.LinearDamping = thing.baseLinearDamping;
                //dampingConstant + dampingVelocityDependencyFactor / body.LinearVelocity
                thing.SetAdjustedLinearDamping();

                // Simulate an impulse that makes the surface seem concave
                // First horizontal force
                float midLine = 18.8f;
                float distFromMidLine = thing.body.Position.X;
                float forceToMiddle = distFromMidLine - midLine;
                thing.body.ApplyLinearImpulse(new Vector2(-forceToMiddle * HORIZONTAL_FORCE_TO_MIDDLE_FACTOR, 0));

                // Then vertical force
                midLine = 8.0f;
                distFromMidLine = thing.body.Position.Y;
                forceToMiddle = distFromMidLine - midLine;
                thing.body.ApplyLinearImpulse(new Vector2(0, -forceToMiddle * VERTICAL_FORCE_TO_MIDDLE_FACTOR));
            }

            // Update all timers that the minigame has added
            foreach (var timer in timers)
                timer.Update(gameTime.ElapsedGameTime.TotalSeconds);

            world.Step((float)gameTime.ElapsedGameTime.TotalSeconds * 1.0f);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();

            if (isServer)
            {
                string frameDrawData = "";

                foreach (KeyValuePair<int, Thing> kvp in things)
                {
                    Thing thing = kvp.Value;

                    if (!thing.isVisible)
                        continue;

                    Vector2 scrPos = ToScrPos(thing.body.Position);

                    Debug.WriteLine("Thing " + thing.id + " has name: " + thing.tex.Name);

                    _spriteBatch.Draw(texture: thing.tex,
                                      position: scrPos,
                                      sourceRectangle: new Rectangle(0, 0, thing.tex.Width, thing.tex.Height),
                                      color: new Color(thing.drawTintRed, thing.drawTintGreen,
                                                       thing.drawTintBlue, thing.drawTintAlpha) /*Color.White*/,
                                      //rotation: 0.0f,
                                      rotation: -thing.body.Rotation,
                                      origin: thing.origin,
                                      scale: thing.scale,
                                      effects: SpriteEffects.None,
                                      layerDepth: 0.0f);

                    // The clients will also want to draw the things, so send draw data to them
                    string thingDrawData = ThingToDrawData(thing);

                    frameDrawData += thingDrawData;
                }
                Debug.WriteLine("frameDrawData: " + frameDrawData);
                PolyNetworking.Networking.SendDrawData(frameDrawData);
            }
            else
            {
                Debug.WriteLine("Client should only draw from received drawData. For now, just split and log drawData.");

                // Get the latest drawData that has been received from the server
                string drawData = PolyNetworking.Networking.GetReceivedDrawData();

                // Split the drawData string into an array of things
                string[] thingStrings = drawData.Split(OBJECT_SEPARATOR);

                // Loop over the thing drawData strings
                foreach (string thingString in thingStrings)
                {
                    if (thingString.Length == 0)
                        continue;

                    // Split thing into an array of values
                    string[] valueStrings = thingString.Split(FIELD_SEPARATOR);

                    Texture2D tex = IndexToTexture(int.Parse(valueStrings[1]));
                    Vector2 physPos = new Vector2(float.Parse(valueStrings[2]), float.Parse(valueStrings[3]));
                    Vector2 scrPos = ToScrPos(physPos);

                    /**/
                    _spriteBatch.Draw(texture: tex,
                                               position: scrPos,
                                               sourceRectangle: new Rectangle(0, 0, tex.Width, tex.Height),
                                               color: Color.White,
                                               rotation: 0.0f,
                                               //rotation: -thing.body.Rotation,
                                               origin: new Vector2(0, 0),
                                               scale: new Vector2(1, 1),
                                               effects: SpriteEffects.None,
                                               layerDepth: 0.0f);
                    /**/

                    // Loop over the drawData strings
                    // For now just log them
                    foreach (string valueString in valueStrings)
                        Debug.WriteLine("Next valueString: " + valueString);
                }
            }
            _spriteBatch.End();

            base.Draw(gameTime);
        }


        protected string ThingToDrawData(Thing thing)
        {
            Vector2 scrPos = ToScrPos(thing.body.Position);

            string str = SPECIFIER_THING + ";" + TextureToIndex(thing.tex) /* "888" */ + ";" +
                         scrPos.X + ";" + scrPos.Y + ";" +
                         thing.drawTintAlpha + ";" + thing.drawTintRed + ";" +
                         thing.drawTintGreen + ";" + thing.drawTintBlue + ";" +
                         thing.body.Rotation + "#";

            Debug.WriteLine("ThingToDrawData: " + str);
            return str;
        }

        /**/
        int TextureToIndex(Texture2D tex)
        {
            int index = -1;
            foreach (var item in testIndexMapping)
            {
                if (item.Value == tex.Name.Replace('\\', '/'))
                {
                    index = item.Key;
                    break;
                }
            }
            return index;
        }
        /**/

        Texture2D IndexToTexture(int index)
        {
            string texName = "Unknown";
            if (testIndexMapping.ContainsKey(index))
                texName = testIndexMapping[index].Replace('\\', '/');
            return textureDict[texName];
        }





        protected void InitializeTextureMap() {
            string rootDirectory = Content.RootDirectory;

            // Beräknar den fullständiga sökvägen till 'Content'-mappen baserat på den aktuella arbetskatalogen.
            string contentFullPath = Path.Combine(System.Environment.CurrentDirectory, rootDirectory);


            var textureMapping = BuildTextureMapping(rootDirectory);

            foreach (var item in textureMapping)
            {
                Debug.WriteLine($"Next key/item: {item.Key}: {item.Value}");
            }
        }

        
        static Dictionary<int, string> BuildTextureMapping(string rootDirectory)
        {
            Debug.WriteLine("BuildTextureMapping called with rootDirectory: " + rootDirectory);
            var textureFiles = Directory.GetFiles(rootDirectory, "*.xnb", SearchOption.AllDirectories);
            Debug.WriteLine("BuildTextureMapping: textureFiles.Length: " + textureFiles.Length);

            var mapping = new Dictionary<int, string>();

            int index = 0;
            foreach (var filePath in textureFiles)
            {
                // Här kan du välja att använda den fullständiga sökvägen eller bara en relativ del av den
                // Beroende på dina behov. Exemplet nedan använder den relativa sökvägen från rootDirectory.
                string relativePath = Path.GetRelativePath(rootDirectory, filePath);

                mapping.Add(index, relativePath);
                index++;
            }

            return mapping;
        }




        /*
        // BAD, static: New version, expects only "Content"
        static Dictionary<int, string> BuildTextureMapping(string contentFolderName)
        {
            // Antag att Content-mappen ligger i samma katalog som den körbara applikationen.
            string rootDirectory = Path.Combine(Environment.CurrentDirectory, contentFolderName);

            // Kontrollera om den uträknade sökvägen existerar
            if (!Directory.Exists(rootDirectory))
            {
                Console.WriteLine($"Kunde inte hitta '{rootDirectory}'. Kontrollera att du har angivit rätt plats för Content-mappen.");
                return new Dictionary<int, string>();
            }

            var textureFiles = Directory.GetFiles(rootDirectory, "*.xnb", SearchOption.AllDirectories);
            var mapping = new Dictionary<int, string>();

            int index = 0;
            foreach (var filePath in textureFiles)
            {
                // Tar bort '.xnb'-ändelsen och använder den relativa sökvägen från 'rootDirectory'
                string relativePath = Path.GetRelativePath(rootDirectory, filePath);
                relativePath = Path.ChangeExtension(relativePath, null); // Tar bort .xnb-filändelsen

                mapping.Add(index, relativePath);
                index++;
            }

            return mapping;
        }
        */


        static Dictionary<int, string> BuildIndexMapping(string rootDirectory)
        {
            var files = Directory.GetFiles(rootDirectory, "*.*", SearchOption.AllDirectories)
                        .Select(filePath => Path.GetRelativePath(rootDirectory, filePath)) // Gör sökvägar relativa
                        .OrderBy(name => name) // Sortera alfabetiskt
                        .ToList();

            var indexMapping = new Dictionary<int, string>();

            for (int i = 0; i < files.Count; i++)
            {
                string nameWithoutExtension = Path.ChangeExtension(files[i], null); // Tar bort .xnb-filändelsen
                indexMapping[i] = nameWithoutExtension.Replace('\\', '/');
            }

            return indexMapping;
        }


        protected void ActOnUpDownLeftRightInput(Thing thing)
        {
            // Inspiration for flexible keys
            //void ActOnInput(Player p, Keys lKey, Keys rKey, Keys jumpKey, PlayerIndex padPlrIx)

            // Walk up
            if (thing.body.LinearVelocity.Y > -MAX_WALK_SPEED &&
                Keyboard.GetState().IsKeyDown(Keys.Up))
                thing.body.ApplyLinearImpulse(new Vector2(0, SIDE_IMPULSE));

            // Walk down
            if (thing.body.LinearVelocity.Y < MAX_WALK_SPEED &&
                Keyboard.GetState().IsKeyDown(Keys.Down))
                thing.body.ApplyLinearImpulse(new Vector2(0, -SIDE_IMPULSE));

            // Walk left
            if (thing.body.LinearVelocity.X > -MAX_WALK_SPEED &&
                Keyboard.GetState().IsKeyDown(Keys.Left))
                thing.body.ApplyLinearImpulse(new Vector2(-SIDE_IMPULSE, 0));

            // Walk right
            if (thing.body.LinearVelocity.X < MAX_WALK_SPEED &&
                Keyboard.GetState().IsKeyDown(Keys.Right))
                thing.body.ApplyLinearImpulse(new Vector2(SIDE_IMPULSE, 0));
        }

        protected void ActOnUpDownInput(Thing thing)
        {
            // Walk up
            if (thing.body.LinearVelocity.Y > -MAX_WALK_SPEED &&
                Keyboard.GetState().IsKeyDown(Keys.Up))
                thing.body.ApplyLinearImpulse(new Vector2(0, SIDE_IMPULSE));

            // Walk down
            if (thing.body.LinearVelocity.Y < MAX_WALK_SPEED &&
                Keyboard.GetState().IsKeyDown(Keys.Down))
                thing.body.ApplyLinearImpulse(new Vector2(0, -SIDE_IMPULSE));
        }

        void ActOnLeftRightInput(Thing thing)
        {
            // Inspiration for flexible keys
            //void ActOnInput(Player p, Keys lKey, Keys rKey, Keys jumpKey, PlayerIndex padPlrIx)

            // Walk left
            if (thing.body.LinearVelocity.X > -MAX_WALK_SPEED &&
                Keyboard.GetState().IsKeyDown(Keys.Left))
                thing.body.ApplyLinearImpulse(new Vector2(-SIDE_IMPULSE, 0));
            
            // Walk right
            if (thing.body.LinearVelocity.X < MAX_WALK_SPEED &&
                Keyboard.GetState().IsKeyDown(Keys.Right))
                thing.body.ApplyLinearImpulse(new Vector2(SIDE_IMPULSE, 0));
        }

        /*
        protected void ActOnUpInput(Thing thing)
        {
            // Walk up
            if (thing.body.LinearVelocity.Y > -MAX_WALK_SPEED &&
                Keyboard.GetState().IsKeyDown(Keys.Up))
                thing.body.ApplyLinearImpulse(new Vector2(0, SIDE_IMPULSE));
        }

        protected void ActOnDownInput(Thing thing)
        {
            // Walk down
            if (thing.body.LinearVelocity.Y < MAX_WALK_SPEED &&
                Keyboard.GetState().IsKeyDown(Keys.Down))
                thing.body.ApplyLinearImpulse(new Vector2(0, -SIDE_IMPULSE));
        }
        */

        protected void LoadLevel(string contentFolder, string levelFolder)
        {
            LevelData loadedLevel = null;
            // This dictionary is needed for a while, until it's time to build the shapes
            var collidersDataDict = new Dictionary<string, Core.CollidersData>();
            var allContentFiles = Directory.GetFiles(contentFolder + "/" + levelFolder, "*.*", SearchOption.AllDirectories);

            // Använd LINQ för att ersätta alla "\" med "/" i varje sökväg.
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
                    
                    //if (file.Contains("/Pictures/") || file.Contains("\\Pictures\\"))
                    if (assetPath.Contains("/Pictures/") || assetPath.Contains("\\Pictures\\"))
                    {
                        var texture = Content.Load<Texture2D>(assetPath);
                        // Save the texture to a collection
                        textureDict[assetPath] = texture;
                    }

                    // Add code for loading sound assets etc here.

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


        // TODO: 
        void BuildColliderShapes(Dictionary<string, Core.CollidersData> collidersDataDict)
        {
            foreach (var objName in collidersDataDict.Keys)
            {
                BuildThingColliderShapes(objName, collidersDataDict[objName]);
            }
        }

        void BuildThingColliderShapes(string objName, Core.CollidersData collidersData)
        {
            // Build shape(s) for this collider data
            //Texture2D tex = textureDict[objName];
            Texture2D tex = PieceNameToTex(objName);
            colliderDict[objName] = Core.ColliderManager.CreateShapesFromCollidersData(collidersData, world, tex.Width, tex.Height, Minigame_Base.MinigameBase.SCALE);
        }

        protected LevelData LoadLevelMap(string assetPath)
        {
            string levelFilePath = Path.Combine(Content.RootDirectory, assetPath + ".json");
            LevelData loadedLevel = LevelMapLoader.LoadLevelMap(levelFilePath);
            return loadedLevel;
        }

        void BuildLevel(LevelData loadedLevel)
        {
            // Loopa över alla GroundTiles i den laddade LevelData
            foreach (var tile in loadedLevel.GroundTiles)
                AddLevelThing(tile.TileName, tile.Position.ToVector2() /**/ + TILE_OFFSET /**/, 0.0f);
        }

        Thing AddLevelThing(string trackPieceName, Vector2 wallPos, float wallRot)
        {
            Texture2D tex = PieceNameToTex(trackPieceName);

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

            // TODO: Create correct fixture inside Thing constructor?
            //HackSwitchFixture(t, trackPieceName);

            AddThing(t);
            return t;
        }

        /*
        void HackSwitchFixture(Thing t, string trackPieceName)
        {
            // The Thing constructor has already created t.body,
            // and even t.body.FixtureList[0].
            // Now try to replace the existing Fixture with the one created from JSON data
            t.body.Remove(t.body.FixtureList[0]);

            List<Shape> shapeList = colliderDict[trackPieceName];

            foreach (Shape shape in shapeList)
                t.body.CreateFixture(shape);

            // Tiles should never be rotated
            t.body.Rotation = 0.0f;
        }
        */

        Texture2D PieceNameToTex(string pieceName)
        {
            Texture2D tex = null;
            if (textureDict.ContainsKey(pieceName))
                tex = textureDict[pieceName];
            return tex;
        }

        //
        // API for Minigame creation
        //

        public void AddThing(Thing thing)
        {
            things.Add(thing.id, thing);
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
            public ControlType controlType;
            public float baseLinearDamping;
            public float extraLinearDamping;
            //public Color drawColor;
            public Vector2 origin;
            public Vector2 scale;
            public int drawTintAlpha;
            public int drawTintRed;
            public int drawTintGreen;
            public int drawTintBlue;

            public Thing(World world,
                         //int id,
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
                         Minigame_Base.ControlType ctrlType = ControlType.None,
                         //Color drawColor = DEFAULT_TINT
                         //Color drawColor = new Color(255, 255, 255)
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
                //b.SetIsSensor(isSensor);
                SetIsSensor(b, isSensor);
                

                this.baseLinearDamping = baseLinearDamping;
                this.extraLinearDamping = extraLinearDamping;
                b.AngularDamping = angularDamping;
                SetFriction(b, friction);
                SetRestitution(b, restitution);
                this.body = b;
                this.SetAdjustedLinearDamping();

                this.origin = new Vector2(originX, originY);
                this.scale = new Vector2(scaleX, scaleY);

                this.controlType = ctrlType;
                //this.drawColor = drawColor;
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

            public void SetAdjustedLinearDamping()
            {
                // Avoid division by zero
                if (body.LinearVelocity.Length() == 0.0f)
                    body.LinearDamping = baseLinearDamping;
                else
                    body.LinearDamping = baseLinearDamping + extraLinearDamping / body.LinearVelocity.Length();
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
        //public Vector2 Position { get; set; }
        public string TileName;
    }

    public class Position
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        // Konverterar Position till MonoGame's Vector2
        //public Vector3 ToVector3() => new Vector3(x, y, z);
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

    public enum ControlType
    {
        None,
        UpDownLeftRight,
        UpDown,
        LeftRight,
        LeftRightJump,
        Custom
    }

    public enum BodyShape
    {
        Rectangle,
        Circle,
        Custom
    }

}