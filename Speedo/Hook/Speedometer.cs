using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

using Remoting;

namespace Speedo.Hook
{
    internal class Speedometer : IDisposable
    {
        private Interface speedoInterface;

        private readonly string baseDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private Device device;
        private bool loaded;
        private bool enabled;
        private const float ANGLE_RATIO = (float)Math.PI / 180f;
        private Sprite dial;
        private Texture backgroundTexture;
        private Texture carTexture;
        private Texture boatTexture;
        private Texture planeTexture;
        private Texture needleTexture;
        private Texture speedFontTexture;
        private Texture boostLevelFontTexture;
        private Texture vehicleFormFontTexture;
        private Texture glowTexture;
        private Texture lightTexture;
        ThemeConfig themeConfig;
        FontLocation[] speedFontLookup;
        FontLocation[] boostLevelFontLookup;
        FontLocation[] vehicleFormFontLookup;

        private float speedoScale;
        private Vector2 speedoPos;
        private Color baseColour = Color.White;
        public string theme;
        public byte maxOpacity;

        private Stopwatch stopwatch = new Stopwatch();
        private double oldDrawTime;
        private double newDrawTime;
        private double opacityGrad = 255;
        public double opacity;

        public Speedometer(Interface speedoInterface, Device device)
        {
            this.speedoInterface = speedoInterface;
            this.device = device;
            stopwatch.Start();
        }

        public void Load()
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; // Consistent parsing of numbers in XML
                themeConfig = ThemeLookup.ReadXML(baseDirectory + "\\Themes\\" + theme + "\\Design.xml");
            }
            catch
            {
                speedoInterface.Message(MessageType.Error, "Failed to load asset: Design.xml");
                return;
            }

            dial = new Sprite(device);
            if (themeConfig.Dial.Show)
            {
                try
                {
                    carTexture = Texture.FromFile(device, baseDirectory + "\\Themes\\" + theme + "\\Dial_Car.png");
                    boatTexture = Texture.FromFile(device, baseDirectory + "\\Themes\\" + theme + "\\Dial_Boat.png");
                    planeTexture = Texture.FromFile(device, baseDirectory + "\\Themes\\" + theme + "\\Dial_Plane.png");

                    if (themeConfig.Dial.ShowBackground)
                    {
                        try
                        {
                            backgroundTexture = Texture.FromFile(device, baseDirectory + "\\Themes\\" + theme + "\\Dial_Background.png");
                        }
                        catch
                        {
                            speedoInterface.Message(MessageType.Warning, "Failed to load asset: Dial_Background.png");
                            themeConfig.Dial.ShowBackground = false;
                        }
                    }
                    if (themeConfig.Dial.ShowGlow)
                    {
                        try
                        {
                            glowTexture = Texture.FromFile(device, baseDirectory + "\\Themes\\" + theme + "\\Glow.png");
                        }
                        catch
                        {
                            speedoInterface.Message(MessageType.Warning, "Failed to load asset: Glow.png");
                            themeConfig.Dial.ShowGlow = false;
                        }
                    }
                }
                catch
                {
                    speedoInterface.Message(MessageType.Warning, "Failed to load assets: Dial_Car.png, Dial_Boat.png, Dial_Plane.png");
                    themeConfig.Dial.Show = false;
                }
            }
            if (themeConfig.Needle.Show)
            {
                try
                {
                    needleTexture = Texture.FromFile(device, baseDirectory + "\\Themes\\" + theme + "\\Needle.png");
                }
                catch
                {
                    speedoInterface.Message(MessageType.Warning, "Failed to load asset: Needle.png");
                    themeConfig.Needle.Show = false;
                }
            }
            if (themeConfig.Speed.Show)
            {
                try
                {
                    speedFontLookup = FontLookup.ReadXML(baseDirectory + "\\Themes\\" + theme + "\\" + themeConfig.Speed.FontName + ".xml");
                    speedFontTexture = Texture.FromFile(device, baseDirectory + "\\Themes\\" + theme + "\\" + themeConfig.Speed.FontName + ".png");
                }
                catch
                {
                    speedoInterface.Message(MessageType.Warning, "Failed to load assets: {0}.png, {0}.xml", themeConfig.Speed.FontName);
                    themeConfig.Speed.Show = false;
                }
            }
            if (themeConfig.BoostLevel.Show)
            {
                try
                {
                    boostLevelFontLookup = FontLookup.ReadXML(baseDirectory + "\\Themes\\" + theme + "\\" + themeConfig.BoostLevel.FontName + ".xml");
                    boostLevelFontTexture = Texture.FromFile(device, baseDirectory + "\\Themes\\" + theme + "\\" + themeConfig.BoostLevel.FontName + ".png");
                }
                catch
                {

                    speedoInterface.Message(MessageType.Warning, "Failed to load assets: {0}.png, {0}.xml", themeConfig.BoostLevel.FontName);
                    themeConfig.BoostLevel.Show = false;
                }
            }
            if (themeConfig.VehicleForm.Show)
            {
                try
                {
                    vehicleFormFontLookup = FontLookup.ReadXML(baseDirectory + "\\Themes\\" + theme + "\\" + themeConfig.VehicleForm.FontName + ".xml");
                    vehicleFormFontTexture = Texture.FromFile(device, baseDirectory + "\\Themes\\" + theme + "\\" + themeConfig.VehicleForm.FontName + ".png");
                }
                catch
                {
                    speedoInterface.Message(MessageType.Warning, "Failed to load assets: {0}.png, {0}.xml", themeConfig.BoostLevel.FontName);
                    themeConfig.VehicleForm.Show = false;
                }
            }
            if (themeConfig.StuntLight.Show)
            {
                try
                {
                    lightTexture = Texture.FromFile(device, baseDirectory + "\\Themes\\" + theme + "\\Light.png");
                }
                catch
                {
                    speedoInterface.Message(MessageType.Warning, "Failed to load asset: Light.png");
                    themeConfig.StuntLight.Show = false;
                }
            }
            loaded = true;
        }

        public void UpdateConfig(SpeedoConfig speedoConfig)
        {
            enabled = speedoConfig.Enabled;
            speedoScale = speedoConfig.Scale;
            speedoPos = new Vector2(speedoConfig.PosX, speedoConfig.PosY);
            maxOpacity = speedoConfig.Opacity;
            if (theme != speedoConfig.Theme)
            {
                theme = speedoConfig.Theme;
                this.Dispose();
            }
            if (!loaded)
            {
                this.Load();
            }
        }

        public void Draw(bool showSpeedo, bool dataAvailable, float speed, VehicleForm form, int boostLevel, bool canStunt)
        {
            // Fade effect for when showing/hiding the speedometer.
            newDrawTime = stopwatch.Elapsed.TotalSeconds;
            double targetOpacity = enabled && showSpeedo ? maxOpacity : 0;
            if (oldDrawTime != 0 && opacity != targetOpacity)
            {
                double opacityStep = opacityGrad * (newDrawTime - oldDrawTime);
                if (opacity > targetOpacity)
                {
                    opacity = Math.Max(0, opacity - opacityStep);
                }
                else
                {
                    opacity = Math.Min(maxOpacity, opacity + opacityStep);
                }
                baseColour.A = (byte)opacity;
            }
            oldDrawTime = newDrawTime;

            if (!loaded || opacity == 0)
            {
                return;
            }

            dial.Begin(SpriteFlags.AlphaBlend);
            if (themeConfig.Dial.Show)
            {
                if (themeConfig.Dial.ShowBackground)
                {
                    DrawBackground();
                }
                if (themeConfig.Dial.ShowGlow)
                {
                    DrawGlow(form, speed);
                }
                DrawDial(form);
            }
            if (themeConfig.Dial.ShowGlow)
            {
                DrawGlow(form, speed);
            }
            if (themeConfig.Dial.Show)
            {
                DrawDial(form);
            }
            if (themeConfig.Needle.Show)
            {
                DrawNeedle(form, speed);
            }
            if (themeConfig.StuntLight.Show && dataAvailable && canStunt)
            {
                DrawLight();
            }
            if (themeConfig.Speed.Show)
            {
                DrawText(
                    speedFontLookup, 
                    speedFontTexture,
                    speedoPos + themeConfig.Speed.Position * speedoScale,
                    themeConfig.Speed.FontSpacing,
                    themeConfig.Speed.FontScale,
                    themeConfig.Speed.TextCentred,
                    string.Format(themeConfig.Speed.TextFormat, speed));
            }
            if (themeConfig.BoostLevel.Show && dataAvailable && (boostLevel > 0 || !themeConfig.BoostLevel.HideBoostLevelZero))
            {
                DrawText(
                    boostLevelFontLookup,
                    boostLevelFontTexture,
                    speedoPos + themeConfig.BoostLevel.Position * speedoScale,
                    themeConfig.BoostLevel.FontSpacing,
                    themeConfig.BoostLevel.FontScale,
                    themeConfig.BoostLevel.TextCentred,
                    string.Format(themeConfig.BoostLevel.TextFormat, boostLevel));
            }
            if (themeConfig.VehicleForm.Show && dataAvailable)
            {
                DrawText(
                    vehicleFormFontLookup,
                    vehicleFormFontTexture,
                    speedoPos + themeConfig.VehicleForm.Position * speedoScale,
                    themeConfig.VehicleForm.FontSpacing,
                    themeConfig.VehicleForm.FontScale,
                    themeConfig.VehicleForm.TextCentred,
                    string.Format(themeConfig.VehicleForm.TextFormat, form.ToString().ToUpper()));
            }
            dial.End();
        }

        public void DrawBackground()
        {
            dial.Transform = Matrix.Transformation2D(Vector2.Zero, 0f, new Vector2(speedoScale, speedoScale), Vector2.Zero, 0f, speedoPos + themeConfig.Dial.BackgroundPosition * speedoScale);
            dial.Draw(backgroundTexture, baseColour);
        }

        public void DrawDial(VehicleForm form)
        {
            dial.Transform = Matrix.Transformation2D(Vector2.Zero, 0f, new Vector2(speedoScale, speedoScale), Vector2.Zero, 0f, speedoPos + themeConfig.Dial.Position * speedoScale);
            switch (form)
            {
                case VehicleForm.Car:
                    dial.Draw(carTexture, baseColour);
                    break;
                case VehicleForm.Boat:
                    dial.Draw(boatTexture, baseColour);
                    break;
                case VehicleForm.Plane:
                    dial.Draw(planeTexture, baseColour);
                    break;
            }
        }

        public void DrawNeedle(VehicleForm form, float speed)
        {
            float angleScale = (themeConfig.Needle.MaxAngle - themeConfig.Needle.MinAngle) / GetMaxSpeed(form);
            float rotation = (themeConfig.Needle.MinAngle + speed * angleScale) * ANGLE_RATIO;

            if (rotation > themeConfig.Needle.MaxAngle * ANGLE_RATIO)
            {
                rotation = themeConfig.Needle.MaxAngle * ANGLE_RATIO;
                if (themeConfig.Needle.MaxSpeedWobble)
                {
                    rotation += themeConfig.Needle.WobbleAngle * (float)Math.Sin(Environment.TickCount / themeConfig.Needle.WobblePeriod * Math.PI * 2f) * ANGLE_RATIO;
                }
            }
            dial.Transform = Matrix.Transformation2D(Vector2.Zero, 0f, new Vector2(speedoScale, speedoScale),
                themeConfig.Needle.PivotPosition * speedoScale, rotation, speedoPos + (themeConfig.Needle.Position - themeConfig.Needle.PivotPosition) * speedoScale);
            dial.Draw(needleTexture, baseColour);
        }

        private void DrawGlow(VehicleForm form, float speed)
        {
            dial.Transform = Matrix.Transformation2D(Vector2.Zero, 0, new Vector2(speedoScale, speedoScale), Vector2.Zero, 0f, speedoPos + themeConfig.Dial.GlowPosition * speedoScale);
            float tmp = Math.Max(0f, speed - GetMaxSpeed(form) * themeConfig.Dial.GlowStart_FractionOfMaxSpeed);
            themeConfig.Dial.GlowColour.A = (byte)(opacity * Math.Min(1, tmp / GetMaxSpeed(form) / (1f - themeConfig.Dial.GlowStart_FractionOfMaxSpeed)));
            dial.Draw(glowTexture, themeConfig.Dial.GlowColour);
        }

        private void DrawLight()
        {
            dial.Transform = Matrix.Transformation2D(Vector2.Zero, 0f, new Vector2(speedoScale, speedoScale), Vector2.Zero, 0f, speedoPos + themeConfig.StuntLight.Position * speedoScale);
            themeConfig.StuntLight.Colour.A = (byte)opacity;
            dial.Draw(lightTexture, themeConfig.StuntLight.Colour);
        }

        private void DrawText(FontLocation[] font, Texture fontTexture, Vector2 startPos, float spacing, float scaling, bool centred, string text)
        {
            char[] charArray = text.ToCharArray();
            int length = charArray.Length;

            FontLocation[] cache = null;
            if (centred)
            {
                cache = new FontLocation[length];
                float textLength = 0;
                for (int i = 0; i < length; i++)
                {
                    cache[i] = FontLookup.FindLetterLocation(font,charArray[i]);
                    textLength += cache[i].width;
                }
                startPos.X -= (textLength + spacing * (length - 1)) * speedoScale * scaling / 2f;
            }

            for (int i = 0; i < length; i++)
            {
                FontLocation letterLocation;
                if (cache != null)
                {
                    letterLocation = cache[i];
                }
                else
                {
                    letterLocation = FontLookup.FindLetterLocation(font, charArray[i]);
                }
                dial.Transform = Matrix.Transformation2D(Vector2.Zero, 0f, new Vector2(speedoScale, speedoScale) * scaling, Vector2.Zero, 0f, startPos);
                Rectangle? rectangle = new Rectangle(letterLocation.x, letterLocation.y, letterLocation.width, letterLocation.height);
                dial.Draw(fontTexture, baseColour, rectangle, new Vector3?(), new Vector3?());
                startPos.X += (letterLocation.width + spacing) * speedoScale * scaling;
            }
        }

        private float GetMaxSpeed(VehicleForm form)
        {
            switch (form)
            {
                case VehicleForm.Car:
                    return themeConfig.Speed.CarMaxSpeed;
                case VehicleForm.Boat:
                    return themeConfig.Speed.BoatMaxSpeed;
                case VehicleForm.Plane:
                    return themeConfig.Speed.PlaneMaxSpeed;
                default:
                    return 0f;
            }
        }

        public void OnLostDevice()
        {
            if (dial != null) dial.OnLostDevice();
        }

        public void OnResetDevice()
        {
            if (dial != null) dial.OnResetDevice();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            if (backgroundTexture != null && !backgroundTexture.IsDisposed) backgroundTexture.Dispose();
            if (carTexture != null && !carTexture.IsDisposed) carTexture.Dispose();
            if (boatTexture != null && !boatTexture.IsDisposed) boatTexture.Dispose();
            if (planeTexture != null && !planeTexture.IsDisposed) planeTexture.Dispose();
            if (glowTexture != null && !glowTexture.IsDisposed) glowTexture.Dispose();
            if (needleTexture != null && !needleTexture.IsDisposed) needleTexture.Dispose();
            if (speedFontTexture != null && !speedFontTexture.IsDisposed) speedFontTexture.Dispose();
            if (boostLevelFontTexture != null && !boostLevelFontTexture.IsDisposed) boostLevelFontTexture.Dispose();
            if (vehicleFormFontTexture != null && !vehicleFormFontTexture.IsDisposed) vehicleFormFontTexture.Dispose();
            if (lightTexture != null && !lightTexture.IsDisposed) lightTexture.Dispose();
            if (dial != null && !dial.IsDisposed) dial.Dispose();
            loaded = false;
        }
    }
}
