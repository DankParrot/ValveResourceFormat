using System;
using System.Globalization;
using GUI.Types.Renderer;
using GUI.Utils;
using OpenTK;
using SteamDatabase.ValvePak;
using ValveResourceFormat.ResourceTypes;
using ValveResourceFormat.Serialization;
using Vector3 = OpenTK.Vector3;
using Vector4 = OpenTK.Vector4;

namespace GUI.Types
{
    internal class RenderWorld
    {
        private readonly World world;

        private static int anonymousCameraCount;

        public RenderWorld(World world)
        {
            this.world = world;
        }

        internal void AddObjects(Renderer.Renderer renderer, string path, Package package)
        {
            // Output is World_t we need to iterate m_worldNodes inside it.
            var worldNodes = world.GetWorldNodeNames();
            foreach (var worldNode in worldNodes)
            {
                if (worldNode != null)
                {
                    var newResource = FileExtensions.LoadFileByAnyMeansNecessary(worldNode + ".vwnod_c", path, package);
                    if (newResource == null)
                    {
                        Console.WriteLine("unable to load model " + worldNode + ".vwnod_c");
                        throw new Exception("WTF");
                    }

                    var renderWorldNode = new RenderWorldNode(newResource);
                    renderWorldNode.AddMeshes(renderer, path, package);
                }
            }

            foreach (var lump in world.GetEntityLumpNames())
            {
                LoadEntities(lump, renderer, path, package);
            }
        }

        private void LoadEntities(string entityName, Renderer.Renderer renderer, string path, Package package)
        {
            if (entityName == null)
            {
                return;
            }

            var newResource = FileExtensions.LoadFileByAnyMeansNecessary(entityName + "_c", path, package);
            if (newResource == null)
            {
                Console.WriteLine("unable to load entity lump " + entityName + "_c");

                return;
            }

            var entityLump = new EntityLump(newResource);
            var childEntities = entityLump.GetChildEntityNames();

            foreach (var childEntityName in childEntities)
            {
                // TODO: Should be controlled in UI with world layers
                if (childEntityName.Contains("_destruction"))
                {
                    continue;
                }

                LoadEntities(childEntityName, renderer, path, package);
            }

            var worldEntities = entityLump.GetEntities();

            foreach (var entity in worldEntities)
            {
                var scale = string.Empty;
                var position = string.Empty;
                var angles = string.Empty;
                var model = string.Empty;
                var skin = string.Empty;
                var colour = new byte[0];
                var classname = string.Empty;
                var name = string.Empty;

                foreach (var property in entity.Properties)
                {
                    //metadata
                    switch (property.MiscType)
                    {
                        case 3368008710: //World Model
                            model = property.Data as string;
                            break;
                        case 3827302934: //Position
                            position = property.Data as string;
                            break;
                        case 3130579663: //Angles
                            angles = property.Data as string;
                            break;
                        case 432137260: //Scale
                            scale = property.Data as string;
                            break;
                        case 2020856412: //Skin
                            skin = property.Data as string;
                            break;
                        case 588463423: //Colour
                            colour = property.Data as byte[];
                            break;
                        case 3323665506: //Classname
                            classname = property.Data as string;
                            break;
                        case 1094168427:
                            name = property.Data as string;
                            break;
                    }
                }

                if (scale == string.Empty || position == string.Empty || angles == string.Empty)
                {
                    continue;
                }

                var isCamera =
                    classname == "info_player_start" ||
                    classname == "worldspawn" ||
                    classname == "sky_camera" ||
                    classname == "point_devshot_camera" ||
                    classname == "point_camera";

                if (!isCamera && model == string.Empty)
                {
                    continue;
                }

                var scaleMatrix = Matrix4.CreateScale(ParseCoordinates(scale));

                var positionVector = ParseCoordinates(position);
                var positionMatrix = Matrix4.CreateTranslation(positionVector);

                var pitchYawRoll = ParseCoordinates(angles);
                var rollMatrix = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(pitchYawRoll.Z)); // Roll
                var pitchMatrix = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(pitchYawRoll.X)); // Pitch
                var yawMatrix = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(pitchYawRoll.Y)); // Yaw

                var rotationMatrix = rollMatrix * pitchMatrix * yawMatrix;
                var transformationMatrix = scaleMatrix * rotationMatrix * positionMatrix;

                if (isCamera)
                {
                    if (classname == "worldspawn")
                    {
                        renderer.SetDefaultWorldCamera(positionVector);
                    }
                    else
                    {
                        renderer.AddCamera(name == string.Empty ? $"{classname} #{anonymousCameraCount++}" : name, transformationMatrix);
                    }

                    continue;
                }

                var objColor = Vector4.One;

                // Parse colour if present
                if (colour.Length == 4)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        objColor[i] = colour[i] / 255.0f;
                    }
                }

                var newEntity = FileExtensions.LoadFileByAnyMeansNecessary(model + "_c", path, package);
                if (newEntity == null)
                {
                    Console.WriteLine($"unable to load entity {model}_c");

                    continue;
                }

                var newModel = new Model(newEntity);
                var entityModel = new RenderModel(newModel);
                entityModel.LoadMeshes(renderer, path, transformationMatrix, objColor, package, skin);
            }
        }

        private static Vector3 ParseCoordinates(string input)
        {
            var vector = default(Vector3);
            var split = input.Split(' ');

            for (var i = 0; i < split.Length; i++)
            {
                vector[i] = float.Parse(split[i], CultureInfo.InvariantCulture);
            }

            return vector;
        }
    }
}
