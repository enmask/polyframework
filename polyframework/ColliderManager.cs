using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Collision.Shapes;
using Microsoft.Xna.Framework;
using System.Diagnostics;


namespace Core
{
    public class ColliderManager
    {
        public static CollidersData CreateColliderFromJson(string json /*, float textureWidth, float textureHeight, float scale*/)
        {
            CollidersData collidersData = JsonConvert.DeserializeObject<CollidersData>(json);
            //List<Shape> shapes = new List<Shape>();

            /*
            foreach (ColliderData colliderData in collidersData.colliders)
            {
                if (colliderData.colliderType == "PolygonCollider2D")
                {
                    shapes.Add(CreatePolygonColliderFromJson(colliderData, textureWidth, textureHeight, scale));
                }
                else
                {
                    throw new Exception(colliderData.colliderType + " is not a valid collider type");
                }

                //else if (colliderData.colliderType == "Circle")
                //{
                //    return CreateCircleColliderFromJson(json, textureWidth, textureHeight, scale);
                //}

            }
            return shapes;
            */

            return collidersData;
        }

        public static List<Shape> CreateShapesFromCollidersData(CollidersData collidersData, World world, float textureWidth, float textureHeight, float scale)
        {
            var shapeList = new List<Shape>();

            foreach (var colliderData in collidersData.colliders)
            {
                if (colliderData.colliderType == "PolygonCollider2D")
                    shapeList.Add(CreatePolygonColliderFromJson(colliderData, textureWidth, textureHeight, scale));
                else
                    throw new Exception(colliderData.colliderType + " is not a valid collider type");
            }
            return shapeList;
        }

        /**/ // Obsolete *here*, but certainly needed later, in order to create Shape:s in a post-step
        public static Shape CreatePolygonColliderFromJson(ColliderData colliderData, float textureWidth, float textureHeight, float scale)
        {

            // Konvertera Points till Vertices för Aether.Physics2D
            Vertices vertices = new Vertices();
            for (int i = 0; i < colliderData.points.Count; i += 2)
            {
                float x = textureWidth * colliderData.points[i] / scale;
                float y = textureHeight * colliderData.points[i + 1] / scale;
                vertices.Add(new Vector2(x, y));
            }

            foreach (Vector2 pt in vertices)
            {
                Debug.WriteLine("Next pt in vertices: " + pt);
            }

            // Skapa och returnera en PolygonShape baserad på de angivna vertices
            return new PolygonShape(vertices, density: 1f);
        }
        /**/

        /* v1 creates a Body
        public void CreateBodyFromJson(string jsonFilePath, World world)
            {
                // Läs in JSON från fil
                string json = File.ReadAllText(jsonFilePath);
                ColliderData colliderData = JsonConvert.DeserializeObject<ColliderData>(json);

                // Skapa en ny Body
                Body body = world.CreateBody();

                Fixture f = new Fixture(body, new Ed

                // Konvertera Points till en lista av Vertices för Aether.Physics2D
                Vertices vertices = new Vertices(colliderData.Points.Length / 2);
                for (int i = 0; i < colliderData.Points.Length; i += 2)
                {
                    float x = colliderData.Points[i];
                    float y = colliderData.Points[i + 1];
                    vertices.Add(new Vector2(x, y));
                }

                // Skapa en polygon shape och lägg till den till body
                var polygonShape = new PolygonShape(vertices, density: 1f); // Ange lämplig densitet
                body.CreateFixture(polygonShape);
            }
        */
        /*
        public void CreateBodyFromJson(string jsonFilePath, World world)
        {
            // Läs in JSON från fil
            string json = File.ReadAllText(jsonFilePath);
            ColliderData colliderData = JsonConvert.DeserializeObject<ColliderData>(json);

            // Skapa en ny Body
            Body body = world.CreateBody();

            // Konvertera Points till en lista av Vector2 för Aether.Physics2D
            Vector2[] vertices = new Vector2[colliderData.Points.Length / 2];
            for (int i = 0; i < colliderData.Points.Length; i += 2)
            {
                float x = colliderData.Points[i];
                float y = colliderData.Points[i + 1];
                vertices[i / 2] = new Vector2(x, y);
            }

            // Skapa en polygon shape och lägg till den till body
            var polygon = new Polygon(vertices);
            body.CreateFixture(polygon);
        }
        */
    }



    /*
    public class ColliderData
        {h
            public float[] Offset { get; set; }
            public float[] Size { get; set; }
            public float[] Points { get; set; }
        }
    */

    public class CollidersData
    {
        public List<ColliderData> colliders = new List<ColliderData>();
    }

    public class ColliderData
    {
        public string colliderType;
        public float[] offset = new float[2];
        public float[] size = new float[2];
        public List<float> points = new List<float>();
    }

}
