using SharpDX;
using SharpDX.Direct3D9;
using Speedo.Interface;
using System;

namespace Speedo.Hook
{
    internal class Speedometer : IDisposable
    {
        private const float ANGLE_RATIO = (float)Math.PI / 180f;
        private Sprite Dial;
        private readonly Texture BackgroundTexture;
        private readonly Texture CarTexture;
        private readonly Texture BoatTexture;
        private readonly Texture PlaneTexture;
        private readonly Texture NeedleTexture;
        private readonly Texture SpeedFontTexture;
        private readonly Texture BoostLevelFontTexture;
        private readonly Texture VehicleFormFontTexture;
        private readonly Texture GlowTexture;
        private readonly Texture LightTexture;
        DesignConfig Design;
        FontLocation[] SpeedFontLookup;
        FontLocation[] BoostLevelFontLookup;
        FontLocation[] VehicleFormFontLookup;

        private float SpeedoScale;
        private Vector2 SpeedoPos;
        private Color BaseColour = Color.White;
        public string Theme;
        private bool DesignLoaded = false;

        public Speedometer(Device device, SpeedoConfig config)
        {
            UpdateConfig(config);
            try
            {
                Design = DesignLookup.ReadXML(AppContext.BaseDirectory + "\\Themes\\" + Theme + "\\Design.xml");
                DesignLoaded = true;
            }
            catch
            {
                DXHook.DebugMessage("Failed to load asset: Design.xml");
                config.Enabled = false;
            }
            Dial = new Sprite(device);
            if (Design.Dial.Show)
            {
                try
                {
                    CarTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Themes\\" + Theme + "\\Dial_Car.png");
                    BoatTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Themes\\" + Theme + "\\Dial_Boat.png");
                    PlaneTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Themes\\" + Theme + "\\Dial_Plane.png");

                    if (Design.Dial.ShowBackground)
                    {
                        try
                        {
                            BackgroundTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Themes\\" + Theme + "\\Dial_Background.png");
                        }
                        catch
                        {
                            DXHook.DebugMessage("Failed to load asset: Dial_Background.png");
                            Design.Dial.ShowBackground = false;
                        }
                    }
                    if (Design.Dial.ShowGlow)
                    {
                        try
                        {
                            GlowTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Themes\\" + Theme + "\\Glow.png");
                        }
                        catch
                        {
                            DXHook.DebugMessage("Failed to load asset: Glow.png");
                            Design.Dial.ShowGlow = false;
                        }
                    }
                }
                catch
                {
                    DXHook.DebugMessage("Failed to load assets: Dial_Car.png, Dial_Boat.png, Dial_Plane.png");
                    Design.Dial.Show = false;
                }
            }
            if (Design.Needle.Show)
            {
                try
                {
                    NeedleTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Themes\\" + Theme + "\\Needle.png");
                }
                catch
                {
                    DXHook.DebugMessage("Failed to load asset: Needle.png");
                    Design.Needle.Show = false;
                }
            }
            if (Design.Speed.Show)
            {
                try
                {
                    SpeedFontLookup = FontLookup.ReadXML(AppContext.BaseDirectory + "\\Themes\\" + Theme + "\\" + Design.Speed.FontName + ".xml");
                    SpeedFontTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Themes\\" + Theme + "\\" + Design.Speed.FontName + ".png");
                }
                catch
                {
                    DXHook.DebugMessage(string.Format("Failed to load assets: {0}.png, {0}.xml", Design.Speed.FontName));
                    Design.Speed.Show = false;
                }
            }
            if (Design.BoostLevel.Show)
            {
                try
                {
                    BoostLevelFontLookup = FontLookup.ReadXML(AppContext.BaseDirectory + "\\Themes\\" + Theme + "\\" + Design.BoostLevel.FontName + ".xml");
                    BoostLevelFontTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Themes\\" + Theme + "\\" + Design.BoostLevel.FontName + ".png");
                }
                catch
                {
                    DXHook.DebugMessage(string.Format("Failed to load assets: {0}.png, {0}.xml", Design.BoostLevel.FontName));
                    Design.BoostLevel.Show = false;
                }
            }
            if (Design.VehicleForm.Show)
            {
                try
                {
                    VehicleFormFontLookup = FontLookup.ReadXML(AppContext.BaseDirectory + "\\Themes\\" + Theme + "\\" + Design.VehicleForm.FontName + ".xml");
                    VehicleFormFontTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Themes\\" + Theme + "\\" + Design.VehicleForm.FontName + ".png");
                }
                catch
                {
                    DXHook.DebugMessage(string.Format("Failed to load assets: {0}.png, {0}.xml", Design.BoostLevel.FontName));
                    Design.VehicleForm.Show = false;
                }
            }
            if (Design.StuntLight.Show)
            {
                try
                {
                    LightTexture = Texture.FromFile(device, AppContext.BaseDirectory + "\\Themes\\" + config.Theme + "\\Light.png");
                }
                catch
                {
                    DXHook.DebugMessage("Failed to load asset: Light.png");
                    Design.StuntLight.Show = false;
                }
            }
        }

        public void UpdateConfig(SpeedoConfig config)
        {
            SpeedoScale = config.Scale;
            SpeedoPos = new Vector2(config.PosX, config.PosY);
            BaseColour.A = config.Opacity;
            Theme = config.Theme;
        }

        public void Draw(float speed, VehicleForm form, int boostLevel, bool canStunt, bool dataAvailable)
        {
            if (!DesignLoaded)
            {
                return;
            }

            Dial.Begin(SpriteFlags.AlphaBlend);
            if (Design.Dial.Show)
            {
                if (Design.Dial.ShowBackground)
                {
                    DrawBackground();
                }
                if (Design.Dial.ShowGlow)
                {
                    DrawGlow(form, speed);
                }
                DrawDial(form);
            }
            if (Design.Dial.ShowGlow)
            {
                DrawGlow(form, speed);
            }
            if (Design.Dial.Show)
            {
                DrawDial(form);
            }
            if (Design.Needle.Show)
            {
                DrawNeedle(form, speed);
            }
            if (Design.StuntLight.Show && canStunt)
            {
                DrawLight();
            }
            if (Design.Speed.Show)
            {
                DrawText(
                    SpeedFontLookup, 
                    SpeedFontTexture,
                    SpeedoPos + Design.Speed.Position * SpeedoScale,
                    Design.Speed.FontSpacing,
                    Design.Speed.FontScale,
                    Design.Speed.TextCentred,
                    string.Format(Design.Speed.TextFormat, speed));
            }
            if (Design.BoostLevel.Show && (boostLevel > 0 || !Design.BoostLevel.HideBoostLevelZero) && dataAvailable)
            {
                DrawText(
                    BoostLevelFontLookup,
                    BoostLevelFontTexture,
                    SpeedoPos + Design.BoostLevel.Position * SpeedoScale,
                    Design.BoostLevel.FontSpacing,
                    Design.BoostLevel.FontScale,
                    Design.BoostLevel.TextCentred,
                    string.Format(Design.BoostLevel.TextFormat, boostLevel));
            }
            if (Design.VehicleForm.Show && dataAvailable)
            {
                DrawText(
                    VehicleFormFontLookup,
                    VehicleFormFontTexture,
                    SpeedoPos + Design.VehicleForm.Position * SpeedoScale,
                    Design.VehicleForm.FontSpacing,
                    Design.VehicleForm.FontScale,
                    Design.VehicleForm.TextCentred,
                    string.Format(Design.VehicleForm.TextFormat, form));
            }
            Dial.End();
        }

        public void DrawBackground()
        {
            Dial.Transform = Matrix.Transformation2D(Vector2.Zero, 0f, new Vector2(SpeedoScale, SpeedoScale), Vector2.Zero, 0f, SpeedoPos + Design.Dial.BackgroundPosition * SpeedoScale);
            Dial.Draw(BackgroundTexture, BaseColour);
        }

        public void DrawDial(VehicleForm form)
        {
            Dial.Transform = Matrix.Transformation2D(Vector2.Zero, 0f, new Vector2(SpeedoScale, SpeedoScale), Vector2.Zero, 0f, SpeedoPos + Design.Dial.Position * SpeedoScale);
            switch (form)
            {
                case VehicleForm.CAR:
                    Dial.Draw(CarTexture, BaseColour);
                    break;
                case VehicleForm.BOAT:
                    Dial.Draw(BoatTexture, BaseColour);
                    break;
                case VehicleForm.PLANE:
                    Dial.Draw(PlaneTexture, BaseColour);
                    break;
            }
        }

        public void DrawNeedle(VehicleForm form, float speed)
        {
            float angleScale = (Design.Needle.MaxAngle - Design.Needle.MinAngle) / GetMaxSpeed(form);
            float rotation = (Design.Needle.MinAngle + speed * angleScale) * ANGLE_RATIO;

            if (rotation > Design.Needle.MaxAngle * ANGLE_RATIO)
            {
                rotation = Design.Needle.MaxAngle * ANGLE_RATIO;
                if (Design.Needle.MaxSpeedWobble)
                {
                    rotation += Design.Needle.WobbleAngle * (float)Math.Sin(Environment.TickCount / Design.Needle.WobblePeriod * Math.PI * 2f) * ANGLE_RATIO;
                }
            }
            Dial.Transform = Matrix.Transformation2D(Vector2.Zero, 0f, new Vector2(SpeedoScale, SpeedoScale),
                Design.Needle.PivotPosition * SpeedoScale, rotation, SpeedoPos + (Design.Needle.Position - Design.Needle.PivotPosition) * SpeedoScale);
            Dial.Draw(NeedleTexture, BaseColour);
        }

        private void DrawGlow(VehicleForm form, float speed)
        {
            Dial.Transform = Matrix.Transformation2D(Vector2.Zero, 0, new Vector2(SpeedoScale, SpeedoScale), Vector2.Zero, 0f, SpeedoPos + Design.Dial.GlowPosition * SpeedoScale);
            float tmp = Math.Max(0f, speed - GetMaxSpeed(form) * Design.Dial.GlowStart_FractionOfMaxSpeed);
            Design.Dial.GlowColour.A = (byte)(255f * Math.Min(1, tmp / GetMaxSpeed(form) / (1f - Design.Dial.GlowStart_FractionOfMaxSpeed)));
            Dial.Draw(GlowTexture, Design.Dial.GlowColour);
        }

        private void DrawLight()
        {
            Dial.Transform = Matrix.Transformation2D(Vector2.Zero, 0f, new Vector2(SpeedoScale, SpeedoScale), Vector2.Zero, 0f, SpeedoPos + Design.StuntLight.Position * SpeedoScale);
            Dial.Draw(LightTexture, Design.StuntLight.Colour);
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
                startPos.X -= (textLength + spacing * (length - 1)) * SpeedoScale * scaling / 2f;
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
                Dial.Transform = Matrix.Transformation2D(Vector2.Zero, 0f, new Vector2(SpeedoScale, SpeedoScale) * scaling, Vector2.Zero, 0f, startPos);
                Rectangle? rectangle = new Rectangle(letterLocation.x, letterLocation.y, letterLocation.width, letterLocation.height);
                Dial.Draw(fontTexture, BaseColour, rectangle, new Vector3?(), new Vector3?());
                startPos.X += (letterLocation.width + spacing) * SpeedoScale * scaling;
            }
        }

        private float GetMaxSpeed(VehicleForm form)
        {
            switch (form)
            {
                case VehicleForm.CAR:
                    return Design.Speed.CarMaxSpeed;
                case VehicleForm.BOAT:
                    return Design.Speed.BoatMaxSpeed;
                case VehicleForm.PLANE:
                    return Design.Speed.PlaneMaxSpeed;
                default:
                    return 0f;
            }
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

            if (Design.Dial.Show)
            {
                CarTexture.Dispose();
                BoatTexture.Dispose();
                PlaneTexture.Dispose();
                if (Design.Dial.ShowGlow)
                {
                    GlowTexture.Dispose();
                }
                if (Design.Dial.ShowBackground)
                {
                    BackgroundTexture.Dispose();
                }
            }
            if (Design.Needle.Show)
            {
                NeedleTexture.Dispose();
            }
            if (Design.Speed.Show)
            {
                SpeedFontTexture.Dispose();
            }
            if (Design.BoostLevel.Show)
            {
                BoostLevelFontTexture.Dispose();
            }
            if (Design.VehicleForm.Show)
            {
                VehicleFormFontTexture.Dispose();
            }
            if (Design.StuntLight.Show)
            {
                LightTexture.Dispose();
            }
            Dial.Dispose();
        }
    }
}
