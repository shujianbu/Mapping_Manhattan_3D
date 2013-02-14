using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using GoblinXNA;
using GoblinXNA.Graphics;
using GoblinXNA.SceneGraph;
using Model = GoblinXNA.Graphics.Model;
using GoblinXNA.Graphics.Geometry;
using GoblinXNA.Device.Capture;
using GoblinXNA.Device.Vision;
using GoblinXNA.Device.Vision.Marker;
using GoblinXNA.Device.Util;
using GoblinXNA.Device.Generic;
using GoblinXNA.Physics;
using GoblinXNA.Physics.Newton1;
using GoblinXNA.Helpers;
using GoblinXNA.Shaders;
using GoblinXNA.UI.UI2D;
using GoblinXNA.UI;

namespace DataVirtulization
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class VisualData : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Scene scene;
        Camera mainCamera, flyingCamera;
        CameraNode mainCameraNode, flyingCameraNode;
        Rectangle mainViewRect, flyingViewRect;
        RenderTarget2D mainViewRenderTarget, flyingViewRenderTarget;
        MarkerNode groundMarkerNode, toolbarMarkerNode;
        TransformNode markerBoardTrans, cameraTrans;
        GeometryNode markerBoardGeom, myPyramid;
        GeometryNode modelEDU10001, modelEMP10001, modelDEM10001, modelHOU10001;
        GeometryNode modelEDU10002, modelEMP10002, modelDEM10002, modelHOU10002;
        GeometryNode modelEDU10003, modelEMP10003, modelDEM10003, modelHOU10003;
        GeometryNode modelEDU10004, modelEMP10004, modelDEM10004, modelHOU10004;
        Matrix manhattanRotation = new Matrix();
        Matrix manhattanInitialRotation = new Matrix();
        G2DPanel objectFrame;
        ButtonGroup group1;
        SpriteFont textFont, textFontLarge;
        
        float markerSize = 32.4f;
        float manhattanSize = 0;
        float cameraX = 0;
        float cameraY = 0;
        float cameraZ = 200;
        Boolean rotationMode = false;
        Boolean selectionMode = false;
        String label = "Nothing is selected";
        String cameraLocation = "None";

        Boolean midtownEast = false;
        Boolean midtownWest = false;
        Boolean downtown = false;
        Boolean uptown = false;
        Boolean cameraFly = false;

        Boolean modelEDUdisplay= false;
        Boolean modelEMPdisplay = false;
        Boolean modelDEMdisplay = false;
        Boolean modelHOUdisplay = false;

        float eduScaleX = (float)0.001;
        float eduScaleY = (float)0.001;
        float eduScaleZ = (float)0.001;
        float empScaleX = (float)0.04;
        float empScaleY = (float)0.04;
        float empScaleZ = (float)0.04;
        float houScaleX = (float)0.0013;
        float houScaleY = (float)0.0013;
        float houScaleZ = (float)0.0013;
        float demScaleX = (float)0.0008;
        float demScaleY = (float)0.0008;
        float demScaleZ = (float)0.0008;

        public VisualData()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Display the mouse cursor
            this.IsMouseVisible = true;

            // Initialize the GoblinXNA framework
            State.InitGoblin(graphics, Content, "");

            // Initialize the scene graph
            scene = new Scene();

            // Use the newton physics engine to perform collision detection
            scene.PhysicsEngine = new NewtonPhysics();

            // For some reason, it sometimes causes memory conflict when it attempts to update the
            // marker transformation in the multi-threaded code, so if you see weird exceptions 
            // thrown in Shaders, then you should not enable the marker tracking thread
            State.ThreadOption = (ushort)ThreadOptions.MarkerTracking;

            // Set up optical marker tracking
            // Note that we don't create our own camera when we use optical marker
            // tracking. It'll be created automatically
            SetupMarkerTracking();

            // Set up cameras for both the AR and VR scenes
            CreateCameras();

            // Setup two viewports, one displasy the AR scene, the other displays the VR scene
            SetupViewport();

            // Set up the lights used in the scene
            CreateLights();

            // Create the ground that represents the physical ground marker array
            CreateMarkerBoard();

            // Create 3D objects
            CreateObjects();

            CreateControlPanel();

            // Add a key click callback function.
            KeyboardInput.Instance.KeyPressEvent += new HandleKeyPress(KeyPressHandler);

            manhattanRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(0));
        }

        private void CreateCameras()
        {
            // Create the main camera
            mainCamera = new Camera();
            mainCamera.Translation = new Vector3(0, -30, -100);
            mainCamera.ZNearPlane = 1;
            mainCamera.ZFarPlane = 2000;

            mainCameraNode = new CameraNode(mainCamera);
            scene.RootNode.AddChild(mainCameraNode);

            // Create the flying camera
            flyingCamera = new Camera();
            flyingCamera.Translation = new Vector3(0, 0, 200);
            flyingCamera.ZNearPlane = 1;
            flyingCamera.ZFarPlane = 2000;

            cameraTrans = new TransformNode();

            flyingCameraNode = new CameraNode(flyingCamera);
            groundMarkerNode.AddChild(cameraTrans);
            cameraTrans.AddChild(flyingCameraNode);

            scene.CameraNode = mainCameraNode;
        }

        private void SetupViewport()
        {
            PresentationParameters pp = GraphicsDevice.PresentationParameters;

            // Create a render target to render the main scene.
            mainViewRenderTarget = new RenderTarget2D(State.Device, State.Width, State.Height, false, SurfaceFormat.Color, pp.DepthStencilFormat);

            // Create a render target to render the flying scene. 
            flyingViewRenderTarget = new RenderTarget2D(State.Device, State.Width * 2 / 7, State.Height * 2 / 6, false, SurfaceFormat.Color, pp.DepthStencilFormat);

            // Set the AR scene to take the full window size
            mainViewRect = new Rectangle(0, 0, State.Width, State.Height);

            // Set the VR scene to take the 2 / 5 of the window size and positioned at the top right corner
            flyingViewRect = new Rectangle(State.Width - flyingViewRenderTarget.Width, 30, flyingViewRenderTarget.Width, flyingViewRenderTarget.Height);
        }

        private void CreateLights()
        {
            // Create a directional light source
            LightSource lightSource = new LightSource();
            lightSource.Direction = new Vector3(1, -1, -1);
            lightSource.Diffuse = Color.White.ToVector4();
            lightSource.Specular = new Vector4(0.5f, 0.5f, 0.5f, 1);

            // Create a light node to hold the light source
            LightNode lightNode = new LightNode();
            lightNode.LightSource = lightSource;

            // Set this light node to cast shadows (by just setting this to true will not cast any shadows,
            // scene.ShadowMap needs to be set to a valid IShadowMap and Model.Shader needs to be set to
            // a proper IShadowShader implementation
            lightNode.CastShadows = true;

            // You should also set the light projection when casting shadow from this light
            lightNode.LightProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, 1f, 500);

            scene.RootNode.AddChild(lightNode);
        }

        private void SetupMarkerTracking()
        {
            DirectShowCapture captureDevice = new DirectShowCapture();
            captureDevice.InitVideoCapture(0, FrameRate._60Hz, Resolution._640x480, ImageFormat.R8G8B8_24, false);

            scene.AddVideoCaptureDevice(captureDevice);

            // Use ALVAR marker tracker
            ALVARMarkerTracker tracker = new ALVARMarkerTracker();
            tracker.MaxMarkerError = 0.02f;
            tracker.InitTracker(captureDevice.Width, captureDevice.Height, "calib.xml", markerSize);

            // Set the marker tracker to use for our scene
            scene.MarkerTracker = tracker;

            // Display the camera image in the background.
            //scene.ShowCameraImage = true;

            // Create a marker node to track a ground marker array.
            groundMarkerNode = new MarkerNode(scene.MarkerTracker, "ALVARGroundArray.xml");
            // Create a marker node to track a toolbar marker array.
            toolbarMarkerNode = new MarkerNode(scene.MarkerTracker, "ALVARToolbar.xml");
            scene.RootNode.AddChild(groundMarkerNode);
            scene.RootNode.AddChild(toolbarMarkerNode);
        }

        private void CreateMarkerBoard()
        {
            markerBoardGeom = new GeometryNode("MarkerBoard")
            {
                Model = new TexturedPlane(200, 250),
                Material =
                {
                    Diffuse = Color.White.ToVector4(),
                    Specular = Color.White.ToVector4(),
                    SpecularPower = 20,
                    Texture = Content.Load<Texture2D>("manhattan")
                }
            };

            // Add this humvee model to the physics engine for collision detection
            markerBoardGeom.AddToPhysicsEngine = true;
            markerBoardGeom.Physics.Shape = ShapeType.ConvexHull;

            Vector3 dimension = Vector3Helper.GetDimensions(markerBoardGeom.Model.MinimumBoundingBox);
            // Scale the model to fit to the size of 5 markers.
            float scale = markerSize * (float)5 / Math.Max(dimension.X, dimension.Z);
            // Transformation node of the humvee model.
            manhattanSize = scale;

            // Rotate the marker board in the VR scene so that it appears Z-up
            markerBoardTrans = new TransformNode();
            manhattanInitialRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.PiOver2);

            groundMarkerNode.AddChild(markerBoardTrans);
            markerBoardTrans.AddChild(markerBoardGeom);
        }

        private void CreateObjects()
        {
            myPyramid = new GeometryNode("Pyramid");
            myPyramid.Model = new Pyramid(markerSize * 4 / 8, markerSize, markerSize);
            Material PyramidMaterial = new Material()
            {
                Diffuse = Color.Orange.ToVector4(),
                Specular = Color.White.ToVector4(),
                SpecularPower = 20
            };
            myPyramid.Material = PyramidMaterial;
            myPyramid.AddToPhysicsEngine = true;
            myPyramid.Physics.Shape = ShapeType.ConvexHull;
            groundMarkerNode.AddChild(myPyramid);
            myPyramid.Material.Diffuse = new Vector4(myPyramid.Material.Diffuse.X, myPyramid.Material.Diffuse.Y, myPyramid.Material.Diffuse.Z, 0);

            ModelLoader loader = new ModelLoader();
            modelEDU10001 = new GeometryNode("Block Education");
            modelEDU10001.Model = (Model)loader.Load("", "education");
            modelEDU10001.AddToPhysicsEngine = true;
            modelEDU10001.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelEDU10001.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelEDU10001);

            modelEMP10001 = new GeometryNode("Block Employment");
            modelEMP10001.Model = (Model)loader.Load("", "employment");
            modelEMP10001.AddToPhysicsEngine = true;
            modelEMP10001.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelEMP10001.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelEMP10001);

            modelDEM10001 = new GeometryNode("Block Demographics");
            modelDEM10001.Model = (Model)loader.Load("", "demographics");
            modelDEM10001.AddToPhysicsEngine = true;
            modelDEM10001.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelDEM10001.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelDEM10001);

            modelHOU10001 = new GeometryNode("Block Housing");
            modelHOU10001.Model = (Model)loader.Load("", "housing");
            modelHOU10001.AddToPhysicsEngine = true;
            modelHOU10001.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelHOU10001.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelHOU10001);

            modelEDU10002 = new GeometryNode("Block Education");
            modelEDU10002.Model = (Model)loader.Load("", "education");
            modelEDU10002.AddToPhysicsEngine = true;
            modelEDU10002.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelEDU10002.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelEDU10002);

            modelEMP10002 = new GeometryNode("Block Employment");
            modelEMP10002.Model = (Model)loader.Load("", "employment");
            modelEMP10002.AddToPhysicsEngine = true;
            modelEMP10002.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelEMP10002.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelEMP10002);

            modelDEM10002 = new GeometryNode("Block Demographics");
            modelDEM10002.Model = (Model)loader.Load("", "demographics");
            modelDEM10002.AddToPhysicsEngine = true;
            modelDEM10002.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelDEM10002.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelDEM10002);

            modelHOU10002 = new GeometryNode("Block Housing");
            modelHOU10002.Model = (Model)loader.Load("", "housing");
            modelHOU10002.AddToPhysicsEngine = true;
            modelHOU10002.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelHOU10002.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelHOU10002);

            modelEDU10003 = new GeometryNode("Block Education");
            modelEDU10003.Model = (Model)loader.Load("", "education");
            modelEDU10003.AddToPhysicsEngine = true;
            modelEDU10003.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelEDU10003.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelEDU10003);

            modelEMP10003 = new GeometryNode("Block Employment");
            modelEMP10003.Model = (Model)loader.Load("", "employment");
            modelEMP10003.AddToPhysicsEngine = true;
            modelEMP10003.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelEMP10003.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelEMP10003);

            modelDEM10003 = new GeometryNode("Block Demographics");
            modelDEM10003.Model = (Model)loader.Load("", "demographics");
            modelDEM10003.AddToPhysicsEngine = true;
            modelDEM10003.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelDEM10003.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelDEM10003);

            modelHOU10003 = new GeometryNode("Block Housing");
            modelHOU10003.Model = (Model)loader.Load("", "housing");
            modelHOU10003.AddToPhysicsEngine = true;
            modelHOU10003.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelHOU10003.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelHOU10003);

            modelEDU10004 = new GeometryNode("Block Education");
            modelEDU10004.Model = (Model)loader.Load("", "education");
            modelEDU10004.AddToPhysicsEngine = true;
            modelEDU10004.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelEDU10004.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelEDU10004);

            modelEMP10004 = new GeometryNode("Block Employment");
            modelEMP10004.Model = (Model)loader.Load("", "employment");
            modelEMP10004.AddToPhysicsEngine = true;
            modelEMP10004.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelEMP10004.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelEMP10004);

            modelDEM10004 = new GeometryNode("Block Demographics");
            modelDEM10004.Model = (Model)loader.Load("", "demographics");
            modelDEM10004.AddToPhysicsEngine = true;
            modelDEM10004.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelDEM10004.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelDEM10004);

            modelHOU10004 = new GeometryNode("Block Housing");
            modelHOU10004.Model = (Model)loader.Load("", "housing");
            modelHOU10004.AddToPhysicsEngine = true;
            modelHOU10004.Physics.Shape = ShapeType.ConvexHull;
            ((Model)modelHOU10004.Model).UseInternalMaterials = true;
            groundMarkerNode.AddChild(modelHOU10004);
        }

        private void CreateControlPanel()
        {
            objectFrame = new G2DPanel();
            objectFrame.Bounds = new Rectangle(700, State.Height - 160, 90, 150);
            objectFrame.Border = GoblinEnums.BorderFactory.LineBorder;
            objectFrame.BorderColor = Color.Gold;
            // Ranges from 0 (fully transparent) to 1 (fully opaque)
            objectFrame.Transparency = 0.5f;

            G2DButton visualizeFile = new G2DButton("Visulize");
            visualizeFile.TextFont = textFont;
            visualizeFile.Bounds = new Rectangle(5, 5, 70, 20);
            visualizeFile.ActionPerformedEvent += new ActionPerformed(HandleVisualizeActionsPerformed);
            objectFrame.AddChild(visualizeFile);

            G2DRadioButton radioDownTown = new G2DRadioButton("Downtown");
            radioDownTown.TextFont = textFont;
            radioDownTown.Bounds = new Rectangle(5, 25, 70, 20);
            radioDownTown.ActionPerformedEvent += new ActionPerformed(HandleActionPerformedSection);
            G2DRadioButton radioMidtownEast = new G2DRadioButton("MidtownEast");
            radioMidtownEast.TextFont = textFont;
            radioMidtownEast.Bounds = new Rectangle(5, 40, 70, 20);
            radioMidtownEast.ActionPerformedEvent += new ActionPerformed(HandleActionPerformedSection);
            G2DRadioButton radioMidtownWest = new G2DRadioButton("MidtownWest");
            radioMidtownWest.TextFont = textFont;
            radioMidtownWest.Bounds = new Rectangle(5, 55, 70, 20);
            radioMidtownWest.ActionPerformedEvent += new ActionPerformed(HandleActionPerformedSection);
            G2DRadioButton radioUptown = new G2DRadioButton("Uptown");
            radioUptown.TextFont = textFont;
            radioUptown.Bounds = new Rectangle(5, 70, 70, 20);
            radioUptown.ActionPerformedEvent += new ActionPerformed(HandleActionPerformedSection);
            group1 = new ButtonGroup();
            group1.Add(radioDownTown);
            group1.Add(radioMidtownEast);
            group1.Add(radioMidtownWest);
            group1.Add(radioUptown);
            objectFrame.AddChild(radioDownTown);
            objectFrame.AddChild(radioMidtownEast);
            objectFrame.AddChild(radioMidtownWest);
            objectFrame.AddChild(radioUptown);

            G2DButton focusButton = new G2DButton("Camera Focus");
            focusButton.TextFont = textFont;
            focusButton.Bounds = new Rectangle(5, 95, 80, 20);
            focusButton.ActionPerformedEvent += new ActionPerformed(HandleCameraActionsPerformed);
            objectFrame.AddChild(focusButton);

            G2DButton resetButton = new G2DButton("Reset");
            resetButton.TextFont = textFont;
            resetButton.Bounds = new Rectangle(5, 120, 80, 20);
            resetButton.ActionPerformedEvent += new ActionPerformed(HandleResetActionsPerformed);
            objectFrame.AddChild(resetButton);

            scene.UIRenderer.Add2DComponent(objectFrame);
        }

        void KeyPressHandler(Keys key, KeyModifier modifier)
        {
            if (key == Keys.Escape)
                this.Exit();
            //Quit button
            if (key == Keys.Q)
            {
                rotationMode = false;
                selectionMode = false;

                myPyramid.Material.Diffuse = new Vector4(myPyramid.Material.Diffuse.X, myPyramid.Material.Diffuse.Y, myPyramid.Material.Diffuse.Z, 0);
                UI2DRenderer.WriteText(Vector2.Zero, "", Color.Red, textFontLarge, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            }
            //Reset button
            if (key == Keys.R)
            {
                rotationMode = false;
                selectionMode = false;
                //Initialize rotation of Manhattan.
                manhattanRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(0));

                myPyramid.Material.Diffuse = new Vector4(myPyramid.Material.Diffuse.X, myPyramid.Material.Diffuse.Y, myPyramid.Material.Diffuse.Z, 0);
                UI2DRenderer.WriteText(Vector2.Zero, "", Color.Red, textFontLarge, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);

                cameraX = 0;
                cameraY = 0;
                cameraZ = 200;

                cameraLocation = "None";
                uptown = false;
                midtownWest = false;
                midtownWest = false;
                downtown = false;
                cameraFly = false;
                cameraTrans.Translation = new Vector3(0, 0, 0);

                modelEDUdisplay= false;
                modelHOUdisplay = false;
                modelDEMdisplay = false;
                modelEMPdisplay = false;
            }
            //Activate rotation mode.
            if (key == Keys.C)
            {
                rotationMode = true;
            }
            //Activate selection mode.
            if (key == Keys.P)
            {
                selectionMode = true;
            }
            //Move the camera up when press the "Up" key.
            if (key == Keys.Up)
                cameraY += (float)5;
            //Move the camera down when press the "Down" key.
            if (key == Keys.Down)
                cameraY -= (float)5;
            //Move the camera left when press the "Left" key.
            if (key == Keys.Left)
                cameraX -= (float)5;
            //Move the camera right when press the "Right" key.
            if (key == Keys.Right)
                cameraX += (float)5;
            //Move the camera in when press the "I" key.
            if (key == Keys.OemPlus)
                cameraZ -= (float)5;
            //Move the camera out when press the "O" key.
            if (key == Keys.OemMinus)
                cameraZ += (float)5;

            //Display Education Block.
            if (key == Keys.D1)
            {
                modelHOUdisplay = false;
                modelDEMdisplay = false;
                modelEMPdisplay = false;

                modelEDUdisplay= true;
            }
            //Display Employment Block.
            if (key == Keys.D2)
            {
                modelEDUdisplay= false;
                modelHOUdisplay = false;
                modelDEMdisplay = false;

                modelEMPdisplay = true;
            }
            //Display Demographic Block.
            if (key == Keys.D3)
            {
                modelEMPdisplay = false;
                modelEDUdisplay= false;
                modelHOUdisplay = false;

                modelDEMdisplay = true;
            }
            //Display Housing Block.
            if (key == Keys.D4)
            {
                modelDEMdisplay = false;
                modelEMPdisplay = false;
                modelEDUdisplay= false;

                modelHOUdisplay = true;
            }
        }

        private void HandleResetActionsPerformed(object source)
        {
            rotationMode = false;
            selectionMode = false;

            //Initialize rotation of Manhattan.
            manhattanRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(0));

            myPyramid.Material.Diffuse = new Vector4(myPyramid.Material.Diffuse.X, myPyramid.Material.Diffuse.Y, myPyramid.Material.Diffuse.Z, 0);
            UI2DRenderer.WriteText(Vector2.Zero, "", Color.Red, textFontLarge, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);

            cameraX = 0;
            cameraY = 0;
            cameraZ = 200;

            cameraLocation = "None";
            uptown = false;
            midtownWest = false;
            midtownWest = false;
            downtown = false;
            cameraFly = false;

            cameraTrans.Translation = new Vector3(0, 0, 0);

            modelEDUdisplay= false;
            modelHOUdisplay = false;
            modelDEMdisplay = false;
            modelEMPdisplay = false;
        }

        private void HandleVisualizeActionsPerformed(object source)
        {
        }

        private void HandleCameraActionsPerformed(object source)
        {
            cameraFly = true;
        }

        private void HandleActionPerformedSection(object source)
        {
            G2DComponent comp = (G2DComponent)source;
            cameraLocation = ((G2DRadioButton)comp).Text;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            
            textFont = Content.Load<SpriteFont>("Arial");
            textFontLarge = Content.Load<SpriteFont>("ArialLarge");
            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            //Rotations to attach the camera to the Earth.
            if (cameraFly == true && cameraLocation == "Downtown")
            {
                if (downtown == false)
                {
                    downtown = true;
                    cameraX = 0;
                    cameraY = 0;
                    cameraZ = 200;
                    cameraTrans.Translation = new Vector3(0, 0, 0);
                }
                cameraTrans.Translation = new Vector3(-10, -50, 20);
                flyingCameraNode.Camera.View = Matrix.CreateLookAt(new Vector3(cameraX, cameraY - 60, cameraZ - 160), Vector3.Zero, Vector3.Up);
            }
            else if (cameraFly == true && cameraLocation == "MidtownWest")
            {
                if (midtownWest == false)
                {
                    midtownWest = true;
                    cameraX = 0;
                    cameraY = 0;
                    cameraZ = 200;
                    cameraTrans.Translation = new Vector3(0, 0, 0);
                }
                cameraTrans.Translation = new Vector3(15, 0, 0);
                flyingCameraNode.Camera.View = Matrix.CreateLookAt(new Vector3(cameraX, cameraY - 60, cameraZ - 160), Vector3.Zero, Vector3.Up);
            }
            else if (cameraFly == true && cameraLocation == "MidtownEast")
            {
                if (midtownEast == false)
                {
                    midtownEast = true;
                    cameraX = 0;
                    cameraY = 0;
                    cameraZ = 200;
                    cameraTrans.Translation = new Vector3(0, 0, 0);
                }
                cameraTrans.Translation = new Vector3(-15, 0, 0);
                flyingCameraNode.Camera.View = Matrix.CreateLookAt(new Vector3(cameraX, cameraY - 60, cameraZ - 160), Vector3.Zero, Vector3.Up);
            }
            else if (cameraFly == true && cameraLocation == "Uptown")
            {
                if (uptown == false)
                {
                    uptown = true;
                    cameraX = 0;
                    cameraY = 0;
                    cameraZ = 200;
                    cameraTrans.Translation = new Vector3(0, 0, 0);
                }
                cameraTrans.Translation = new Vector3(30, 35, 0);
                flyingCameraNode.Camera.View = Matrix.CreateLookAt(new Vector3(cameraX, cameraY - 60, cameraZ - 160), Vector3.Zero, Vector3.Up);
            }
            else
                flyingCameraNode.Camera.View = Matrix.CreateLookAt(new Vector3(cameraX, cameraY, cameraZ), Vector3.Zero, Vector3.Up);
            scene.Update(gameTime.ElapsedGameTime, gameTime.IsRunningSlowly, this.IsActive);
        }

        private void markerHelper()
        {
            if (toolbarMarkerNode.MarkerFound)
            {
                if (rotationMode == true)
                    manhattanRotation = Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.ToRadians(toolbarMarkerNode.WorldTransformation.Translation.X * 6));
            }
            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(markerBoardGeom.Physics, Matrix.CreateScale(manhattanSize) * manhattanRotation * manhattanInitialRotation);
            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10001.Physics, manhattanRotation);
            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10001.Physics, manhattanRotation);
            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10001.Physics, manhattanRotation);

            if (modelEDUdisplay)
            {
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10001.Material.Diffuse = new Vector4(modelEMP10001.Material.Diffuse.X, modelEMP10001.Material.Diffuse.Y, modelEMP10001.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10001.Material.Diffuse = new Vector4(modelHOU10001.Material.Diffuse.X, modelHOU10001.Material.Diffuse.Y, modelHOU10001.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10001.Material.Diffuse = new Vector4(modelDEM10001.Material.Diffuse.X, modelDEM10001.Material.Diffuse.Y, modelDEM10001.Material.Diffuse.Z, 0);
                
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10001.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10001.Physics, Matrix.CreateScale(eduScaleX, eduScaleY, eduScaleZ) * Matrix.CreateTranslation(-20, -27, 0));
                modelEDU10001.Material.Diffuse = new Vector4(modelEDU10001.Material.Diffuse.X, modelEDU10001.Material.Diffuse.Y, modelEDU10001.Material.Diffuse.Z, 1);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10002.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10002.Material.Diffuse = new Vector4(modelEMP10002.Material.Diffuse.X, modelEMP10002.Material.Diffuse.Y, modelEMP10002.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10002.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10002.Material.Diffuse = new Vector4(modelHOU10002.Material.Diffuse.X, modelHOU10002.Material.Diffuse.Y, modelHOU10002.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10002.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10002.Material.Diffuse = new Vector4(modelDEM10002.Material.Diffuse.X, modelDEM10002.Material.Diffuse.Y, modelDEM10002.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10002.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10002.Physics, Matrix.CreateScale(eduScaleX, eduScaleY, eduScaleZ) * Matrix.CreateTranslation(-6, -55, 0));
                modelEDU10002.Material.Diffuse = new Vector4(modelEDU10002.Material.Diffuse.X, modelEDU10002.Material.Diffuse.Y, modelEDU10002.Material.Diffuse.Z, 1);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10003.Material.Diffuse = new Vector4(modelEMP10003.Material.Diffuse.X, modelEMP10003.Material.Diffuse.Y, modelEMP10003.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10003.Material.Diffuse = new Vector4(modelHOU10003.Material.Diffuse.X, modelHOU10003.Material.Diffuse.Y, modelHOU10003.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10003.Material.Diffuse = new Vector4(modelDEM10003.Material.Diffuse.X, modelDEM10003.Material.Diffuse.Y, modelDEM10003.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10003.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10003.Physics, Matrix.CreateScale(eduScaleX, eduScaleY, eduScaleZ) * Matrix.CreateTranslation(-11, -43, 0));
                modelEDU10003.Material.Diffuse = new Vector4(modelEDU10003.Material.Diffuse.X, modelEDU10003.Material.Diffuse.Y, modelEDU10003.Material.Diffuse.Z, 1);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10004.Material.Diffuse = new Vector4(modelEMP10004.Material.Diffuse.X, modelEMP10004.Material.Diffuse.Y, modelEMP10004.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10004.Material.Diffuse = new Vector4(modelHOU10004.Material.Diffuse.X, modelHOU10004.Material.Diffuse.Y, modelHOU10004.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10004.Material.Diffuse = new Vector4(modelDEM10004.Material.Diffuse.X, modelDEM10004.Material.Diffuse.Y, modelDEM10004.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10004.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10004.Physics, Matrix.CreateScale(eduScaleX, eduScaleY, eduScaleZ) * Matrix.CreateTranslation(-33, -67, 0));
                modelEDU10004.Material.Diffuse = new Vector4(modelEDU10004.Material.Diffuse.X, modelEDU10004.Material.Diffuse.Y, modelEDU10004.Material.Diffuse.Z, 1);
            }
            else if (modelEMPdisplay)
            {
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10001.Material.Diffuse = new Vector4(modelEDU10001.Material.Diffuse.X, modelEDU10001.Material.Diffuse.Y, modelEDU10001.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10001.Material.Diffuse = new Vector4(modelHOU10001.Material.Diffuse.X, modelHOU10001.Material.Diffuse.Y, modelHOU10001.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10001.Material.Diffuse = new Vector4(modelDEM10001.Material.Diffuse.X, modelDEM10001.Material.Diffuse.Y, modelDEM10001.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10001.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10001.Physics, Matrix.CreateScale(empScaleX, empScaleY, empScaleZ) * Matrix.CreateTranslation(-20, -27, 0));
                modelEMP10001.Material.Diffuse = new Vector4(modelEMP10001.Material.Diffuse.X, modelEMP10001.Material.Diffuse.Y, modelEMP10001.Material.Diffuse.Z, 1);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10002.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10002.Material.Diffuse = new Vector4(modelEDU10002.Material.Diffuse.X, modelEDU10002.Material.Diffuse.Y, modelEDU10002.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10002.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10002.Material.Diffuse = new Vector4(modelHOU10002.Material.Diffuse.X, modelHOU10002.Material.Diffuse.Y, modelHOU10002.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10002.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10002.Material.Diffuse = new Vector4(modelDEM10002.Material.Diffuse.X, modelDEM10002.Material.Diffuse.Y, modelDEM10002.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10002.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10002.Physics, Matrix.CreateScale(empScaleX, empScaleY, empScaleZ) * Matrix.CreateTranslation(-6, -55, 0));
                modelEMP10002.Material.Diffuse = new Vector4(modelEMP10002.Material.Diffuse.X, modelEMP10002.Material.Diffuse.Y, modelEMP10002.Material.Diffuse.Z, 1);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10003.Material.Diffuse = new Vector4(modelEDU10003.Material.Diffuse.X, modelEDU10003.Material.Diffuse.Y, modelEDU10003.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10003.Material.Diffuse = new Vector4(modelHOU10003.Material.Diffuse.X, modelHOU10003.Material.Diffuse.Y, modelHOU10003.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10003.Material.Diffuse = new Vector4(modelDEM10003.Material.Diffuse.X, modelDEM10003.Material.Diffuse.Y, modelDEM10003.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10003.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10003.Physics, Matrix.CreateScale(empScaleX, empScaleY, empScaleZ) * Matrix.CreateTranslation(-11, -43, 0));
                modelEMP10003.Material.Diffuse = new Vector4(modelEMP10003.Material.Diffuse.X, modelEMP10003.Material.Diffuse.Y, modelEMP10003.Material.Diffuse.Z, 1);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10004.Material.Diffuse = new Vector4(modelEDU10004.Material.Diffuse.X, modelEDU10004.Material.Diffuse.Y, modelEDU10004.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10004.Material.Diffuse = new Vector4(modelHOU10004.Material.Diffuse.X, modelHOU10004.Material.Diffuse.Y, modelHOU10004.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10004.Material.Diffuse = new Vector4(modelDEM10004.Material.Diffuse.X, modelDEM10004.Material.Diffuse.Y, modelDEM10004.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10004.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10004.Physics, Matrix.CreateScale(empScaleX, empScaleY, empScaleZ) * Matrix.CreateTranslation(-33, -67, 0));
                modelEMP10004.Material.Diffuse = new Vector4(modelEMP10004.Material.Diffuse.X, modelEMP10004.Material.Diffuse.Y, modelEMP10004.Material.Diffuse.Z, 1);
            }
            else if (modelDEMdisplay)
            {
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10001.Material.Diffuse = new Vector4(modelEDU10001.Material.Diffuse.X, modelEDU10001.Material.Diffuse.Y, modelEDU10001.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10001.Material.Diffuse = new Vector4(modelHOU10001.Material.Diffuse.X, modelHOU10001.Material.Diffuse.Y, modelHOU10001.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10001.Material.Diffuse = new Vector4(modelEMP10001.Material.Diffuse.X, modelEMP10001.Material.Diffuse.Y, modelEMP10001.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10001.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10001.Physics, Matrix.CreateScale(demScaleX, demScaleY, demScaleZ) * Matrix.CreateTranslation(-20, -27, 0));
                modelDEM10001.Material.Diffuse = new Vector4(modelDEM10001.Material.Diffuse.X, modelDEM10001.Material.Diffuse.Y, modelDEM10001.Material.Diffuse.Z, 1);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10002.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10002.Material.Diffuse = new Vector4(modelEDU10002.Material.Diffuse.X, modelEDU10002.Material.Diffuse.Y, modelEDU10002.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10002.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10002.Material.Diffuse = new Vector4(modelHOU10002.Material.Diffuse.X, modelHOU10002.Material.Diffuse.Y, modelHOU10002.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10002.Material.Diffuse = new Vector4(modelEMP10002.Material.Diffuse.X, modelEMP10002.Material.Diffuse.Y, modelEMP10002.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10002.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10002.Physics, Matrix.CreateScale(demScaleX, demScaleY, demScaleZ) * Matrix.CreateTranslation(-6, -55, 0));
                modelDEM10002.Material.Diffuse = new Vector4(modelDEM10002.Material.Diffuse.X, modelDEM10002.Material.Diffuse.Y, modelDEM10002.Material.Diffuse.Z, 1);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10003.Material.Diffuse = new Vector4(modelEDU10003.Material.Diffuse.X, modelEDU10003.Material.Diffuse.Y, modelEDU10003.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10003.Material.Diffuse = new Vector4(modelHOU10003.Material.Diffuse.X, modelHOU10003.Material.Diffuse.Y, modelHOU10003.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10003.Material.Diffuse = new Vector4(modelEMP10003.Material.Diffuse.X, modelEMP10003.Material.Diffuse.Y, modelEMP10003.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10003.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10003.Physics, Matrix.CreateScale(demScaleX, demScaleY, demScaleZ) * Matrix.CreateTranslation(-11, -43, 0));
                modelDEM10003.Material.Diffuse = new Vector4(modelDEM10003.Material.Diffuse.X, modelDEM10003.Material.Diffuse.Y, modelDEM10003.Material.Diffuse.Z, 1);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10004.Material.Diffuse = new Vector4(modelEDU10004.Material.Diffuse.X, modelEDU10004.Material.Diffuse.Y, modelEDU10004.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10004.Material.Diffuse = new Vector4(modelHOU10004.Material.Diffuse.X, modelHOU10004.Material.Diffuse.Y, modelHOU10004.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10004.Material.Diffuse = new Vector4(modelEMP10004.Material.Diffuse.X, modelEMP10004.Material.Diffuse.Y, modelEMP10004.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10004.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10004.Physics, Matrix.CreateScale(demScaleX, demScaleY, demScaleZ) * Matrix.CreateTranslation(-33, -67, 0));
                modelDEM10004.Material.Diffuse = new Vector4(modelDEM10004.Material.Diffuse.X, modelDEM10004.Material.Diffuse.Y, modelDEM10004.Material.Diffuse.Z, 1);
            }
            else if (modelHOUdisplay)
            {
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10001.Material.Diffuse = new Vector4(modelEDU10001.Material.Diffuse.X, modelEDU10001.Material.Diffuse.Y, modelEDU10001.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10001.Material.Diffuse = new Vector4(modelEMP10001.Material.Diffuse.X, modelEMP10001.Material.Diffuse.Y, modelEMP10001.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10001.Material.Diffuse = new Vector4(modelDEM10001.Material.Diffuse.X, modelDEM10001.Material.Diffuse.Y, modelDEM10001.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10001.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10001.Physics, Matrix.CreateScale(houScaleX, houScaleY, houScaleZ) * Matrix.CreateTranslation(-18, -26, 0));
                modelHOU10001.Material.Diffuse = new Vector4(modelHOU10001.Material.Diffuse.X, modelHOU10001.Material.Diffuse.Y, modelHOU10001.Material.Diffuse.Z, 1);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10002.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10002.Material.Diffuse = new Vector4(modelEDU10002.Material.Diffuse.X, modelEDU10002.Material.Diffuse.Y, modelEDU10002.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10002.Material.Diffuse = new Vector4(modelEMP10002.Material.Diffuse.X, modelEMP10002.Material.Diffuse.Y, modelEMP10002.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10002.Material.Diffuse = new Vector4(modelDEM10002.Material.Diffuse.X, modelDEM10002.Material.Diffuse.Y, modelDEM10002.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10002.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10002.Physics, Matrix.CreateScale(houScaleX, houScaleY, houScaleZ) * Matrix.CreateTranslation(-4, -55, 0));
                modelHOU10002.Material.Diffuse = new Vector4(modelHOU10002.Material.Diffuse.X, modelHOU10002.Material.Diffuse.Y, modelHOU10002.Material.Diffuse.Z, 1);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10003.Material.Diffuse = new Vector4(modelEDU10003.Material.Diffuse.X, modelEDU10003.Material.Diffuse.Y, modelEDU10003.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10003.Material.Diffuse = new Vector4(modelEMP10003.Material.Diffuse.X, modelEMP10003.Material.Diffuse.Y, modelEMP10003.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10003.Material.Diffuse = new Vector4(modelDEM10003.Material.Diffuse.X, modelDEM10003.Material.Diffuse.Y, modelDEM10003.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10003.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10003.Physics, Matrix.CreateScale(houScaleX, houScaleY, houScaleZ) * Matrix.CreateTranslation(-9, -43, 0));
                modelHOU10003.Material.Diffuse = new Vector4(modelHOU10003.Material.Diffuse.X, modelHOU10003.Material.Diffuse.Y, modelHOU10003.Material.Diffuse.Z, 1);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10004.Material.Diffuse = new Vector4(modelEDU10004.Material.Diffuse.X, modelEDU10004.Material.Diffuse.Y, modelEDU10004.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10004.Material.Diffuse = new Vector4(modelEMP10004.Material.Diffuse.X, modelEMP10004.Material.Diffuse.Y, modelEMP10004.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10004.Material.Diffuse = new Vector4(modelDEM10004.Material.Diffuse.X, modelDEM10004.Material.Diffuse.Y, modelDEM10004.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10004.Physics, Matrix.CreateTranslation(0, 0, 0));
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10004.Physics, Matrix.CreateScale(houScaleX, houScaleY, houScaleZ) * Matrix.CreateTranslation(-31, -67, 0));
                modelHOU10004.Material.Diffuse = new Vector4(modelHOU10004.Material.Diffuse.X, modelHOU10004.Material.Diffuse.Y, modelHOU10004.Material.Diffuse.Z, 1);
            }
            else
            {
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10001.Material.Diffuse = new Vector4(modelEDU10001.Material.Diffuse.X, modelEDU10001.Material.Diffuse.Y, modelEDU10001.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10001.Material.Diffuse = new Vector4(modelEMP10001.Material.Diffuse.X, modelEMP10001.Material.Diffuse.Y, modelEMP10001.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10001.Material.Diffuse = new Vector4(modelHOU10001.Material.Diffuse.X, modelHOU10001.Material.Diffuse.Y, modelHOU10001.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10001.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10001.Material.Diffuse = new Vector4(modelDEM10001.Material.Diffuse.X, modelDEM10001.Material.Diffuse.Y, modelDEM10001.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10002.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10002.Material.Diffuse = new Vector4(modelEDU10002.Material.Diffuse.X, modelEDU10002.Material.Diffuse.Y, modelEDU10002.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10002.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10002.Material.Diffuse = new Vector4(modelEMP10002.Material.Diffuse.X, modelEMP10002.Material.Diffuse.Y, modelEMP10002.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10002.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10002.Material.Diffuse = new Vector4(modelHOU10002.Material.Diffuse.X, modelHOU10002.Material.Diffuse.Y, modelHOU10002.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10002.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10002.Material.Diffuse = new Vector4(modelDEM10002.Material.Diffuse.X, modelDEM10002.Material.Diffuse.Y, modelDEM10002.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10003.Material.Diffuse = new Vector4(modelEDU10003.Material.Diffuse.X, modelEDU10003.Material.Diffuse.Y, modelEDU10003.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10003.Material.Diffuse = new Vector4(modelEMP10003.Material.Diffuse.X, modelEMP10003.Material.Diffuse.Y, modelEMP10003.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10003.Material.Diffuse = new Vector4(modelHOU10003.Material.Diffuse.X, modelHOU10003.Material.Diffuse.Y, modelHOU10003.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10003.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10003.Material.Diffuse = new Vector4(modelDEM10003.Material.Diffuse.X, modelDEM10003.Material.Diffuse.Y, modelDEM10003.Material.Diffuse.Z, 0);

                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEDU10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEDU10004.Material.Diffuse = new Vector4(modelEDU10004.Material.Diffuse.X, modelEDU10004.Material.Diffuse.Y, modelEDU10004.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelEMP10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelEMP10004.Material.Diffuse = new Vector4(modelEMP10004.Material.Diffuse.X, modelEMP10004.Material.Diffuse.Y, modelEMP10004.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelHOU10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelHOU10004.Material.Diffuse = new Vector4(modelHOU10004.Material.Diffuse.X, modelHOU10004.Material.Diffuse.Y, modelHOU10004.Material.Diffuse.Z, 0);
                ((NewtonPhysics)scene.PhysicsEngine).SetTransform(modelDEM10004.Physics, Matrix.CreateTranslation(-400, -400, -400));
                modelDEM10004.Material.Diffuse = new Vector4(modelDEM10004.Material.Diffuse.X, modelDEM10004.Material.Diffuse.Y, modelDEM10004.Material.Diffuse.Z, 0);
            }

            if (selectionMode)
            {
                if (toolbarMarkerNode.MarkerFound)
                {
                    myPyramid.Material.Diffuse = new Vector4(myPyramid.Material.Diffuse.X, myPyramid.Material.Diffuse.Y, myPyramid.Material.Diffuse.Z, 1);
                    Matrix mat = toolbarMarkerNode.WorldTransformation * Matrix.Invert(groundMarkerNode.WorldTransformation);
                    ((NewtonPhysics)scene.PhysicsEngine).SetTransform(myPyramid.Physics, mat);
                }
                else
                {
                    myPyramid.Material.Diffuse = new Vector4(myPyramid.Material.Diffuse.X, myPyramid.Material.Diffuse.Y, myPyramid.Material.Diffuse.Z, 0);
                }
            }

        }

        private void displayHelper(GameTime gameTime)
        {
            // Set the render target for rendering the AR scene
            scene.SceneRenderTarget = mainViewRenderTarget;
            // Set the scene background size to be the size of the AR scene viewport
            scene.BackgroundBound = mainViewRect;
            // Set the camera to be the AR camera
            scene.CameraNode = mainCameraNode;
            // Don't render the marker board and camera representation
            markerBoardGeom.Enabled = true;
            // Show the video background
            scene.ShowCameraImage = true;
            // Render the main scene
            scene.Draw(gameTime.ElapsedGameTime, gameTime.IsRunningSlowly);

            // Set the render target for rendering the VR scene
            scene.SceneRenderTarget = flyingViewRenderTarget;
            // Set the scene background size to be the size of the VR scene viewport
            scene.BackgroundBound = flyingViewRect;
            // Set the camera to be the VR camera
            scene.CameraNode = flyingCameraNode;
            // Render the marker board and camera representation in VR scene
            markerBoardGeom.Enabled = true;
            // Do not show the video background
            scene.ShowCameraImage = false;
            // Re-traverse the scene graph since we have modified it, and render the VR scene 
            scene.RenderScene(false, true);

            // Set the render target back to the frame buffer
            State.Device.SetRenderTarget(null);
            // Render the two textures rendered on the render targets
            State.SharedSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            State.SharedSpriteBatch.Draw(mainViewRenderTarget, mainViewRect, Color.White);
            State.SharedSpriteBatch.Draw(flyingViewRenderTarget, flyingViewRect, Color.White);
            State.SharedSpriteBatch.End();
        }

        private void displaySystemPanel()
        {
            //Displaying system control panels.
            if (label == "Nothing is selected")
                UI2DRenderer.WriteText(Vector2.Zero, label, Color.Red, textFontLarge, GoblinEnums.HorizontalAlignment.Center, GoblinEnums.VerticalAlignment.Top);
            else
                UI2DRenderer.WriteText(Vector2.Zero, label + " is selected", Color.Red, textFontLarge, GoblinEnums.HorizontalAlignment.Center, GoblinEnums.VerticalAlignment.Top);
            if (rotationMode)
                UI2DRenderer.WriteText(Vector2.Zero, "Rotation Mode", Color.Red, textFontLarge, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            if (selectionMode)
                UI2DRenderer.WriteText(Vector2.Zero, "Selection Mode", Color.Red, textFontLarge, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            //Control panels for attaching camera to planets and objects
            UI2DRenderer.WriteText(new Vector2(5, 20), "Keys Control Menu", Color.Red, textFontLarge, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 35), "'Up Arrow' - Fly Camera Up", Color.Red, textFontLarge, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 50), "'Down Arrow' - Fly Camera Down", Color.Red, textFontLarge, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 65), "'Left Arrow' - Fly Camera Left", Color.Red, textFontLarge, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 80), "'Right Arrow' - Fly Camera Right", Color.Red, textFontLarge, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 95), "'Plus' - Fly Camera In", Color.Red, textFontLarge, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 110), "'Minus' - Fly Camera Out", Color.Red, textFontLarge, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 125), "'S' - Scale Mode", Color.Red, textFontLarge, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 140), "'C' - Rotation Mode", Color.Red, textFontLarge, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 155), "'P' - Selection Mode", Color.Red, textFontLarge, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 170), "'Q' - Quit Mode", Color.Red, textFontLarge, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 185), "'R' - Reset System", Color.Red, textFontLarge, Vector2.One * 0.8f);
        }
        
        protected override void Draw(GameTime gameTime)
        {
            markerHelper();
            displayHelper(gameTime);
            displaySystemPanel();
            //scene.Draw(gameTime.ElapsedGameTime, gameTime.IsRunningSlowly);
        }
    }
}
