using SharpDX;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Speedo.Hook
{
    public struct ThemeConfig
    {
        public DialConfig Dial;
        public StuntLightConfig StuntLight;
        public NeedleConfig Needle;
        public SpeedConfig Speed;
        public BoostLevelConfig BoostLevel;
        public VehicleFormConfig VehicleForm;
    }
    public struct SpeedConfig
    {
        public bool Show;
        public Vector2 Position;
        public string FontName;
        public float FontScale;
        public float FontSpacing;
        public string TextFormat;
        public bool TextCentred;
        public float CarMaxSpeed;
        public float BoatMaxSpeed;
        public float PlaneMaxSpeed;
    }
    public struct DialConfig
    {
        public bool Show;
        public Vector2 Position;
        public bool ShowBackground;
        public Vector2 BackgroundPosition;
        public bool ShowGlow;
        public Vector2 GlowPosition;
        public Color GlowColour;
        public float GlowStart_FractionOfMaxSpeed;
    }
    public struct NeedleConfig
    {
        public bool Show;
        public Vector2 Position;
        public Vector2 PivotPosition;
        public float MinAngle;
        public float MaxAngle;
        public bool MaxSpeedWobble;
        public float WobbleAngle;
        public float WobblePeriod;
    }
    public struct BoostLevelConfig
    {
        public bool Show;
        public Vector2 Position;
        public string FontName;
        public float FontScale;
        public float FontSpacing;
        public string TextFormat;
        public bool TextCentred;
        public bool HideBoostLevelZero;
    }
    public struct StuntLightConfig
    {
        public bool Show;
        public Vector2 Position;
        public Color Colour;
    }
    public struct VehicleFormConfig
    {
        public bool Show;
        public Vector2 Position;
        public string FontName;
        public float FontScale;
        public float FontSpacing;
        public string TextFormat;
        public bool TextCentred;
    }

    public static class ThemeLookup
    {
        public static ThemeConfig ReadXML(string xmlPath)
        {
            ThemeConfig config = new ThemeConfig();

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlPath);
            XmlNodeList configSections = xmlDocument.SelectNodes("/root/config");

            int length = configSections.Count;
            XmlNode node;
            for (int i = 0; i < length; i++)
            {
                node = configSections[i];
                switch (node.Attributes["id"].Value)
                {
                    case "dial":
                        config.Dial = ReadDialConfig(node);
                        break;
                    case "needle":
                        config.Needle = ReadNeedleConfig(node);
                        break;
                    case "light":
                        config.StuntLight = ReadLightConfig(node);
                        break;
                    case "speed":
                        config.Speed = ReadSpeedConfig(node);
                        break;
                    case "boost":
                        config.BoostLevel = ReadBoostConfig(node);
                        break;
                    case "form":
                        config.VehicleForm = ReadFormConfig(node);
                        break;
                }
            }
            return config;
        }

        private static DialConfig ReadDialConfig(XmlNode node)
        {
            return new DialConfig()
            {
                Show = bool.Parse(node.SelectSingleNode("show").InnerText),
                Position = new Vector2(float.Parse(node.SelectSingleNode("x").InnerText), float.Parse(node.SelectSingleNode("y").InnerText)),
                ShowBackground = bool.Parse(node.SelectSingleNode("show_background").InnerText),
                BackgroundPosition = new Vector2(float.Parse(node.SelectSingleNode("background_x").InnerText), float.Parse(node.SelectSingleNode("background_y").InnerText)),
                ShowGlow = bool.Parse(node.SelectSingleNode("show_glow").InnerText),
                GlowPosition = new Vector2(float.Parse(node.SelectSingleNode("glow_x").InnerText), float.Parse(node.SelectSingleNode("glow_y").InnerText)),
                GlowColour = ParseHTMLColour(node.SelectSingleNode("glow_colour").InnerText),
                GlowStart_FractionOfMaxSpeed = float.Parse(node.SelectSingleNode("glow_start_fraction_of_max_speed").InnerText)
            };
        }

        private static NeedleConfig ReadNeedleConfig(XmlNode node)
        {
            return new NeedleConfig
            {
                Show = bool.Parse(node.SelectSingleNode("show").InnerText),
                Position = new Vector2(float.Parse(node.SelectSingleNode("x").InnerText), float.Parse(node.SelectSingleNode("y").InnerText)),
                PivotPosition = new Vector2(float.Parse(node.SelectSingleNode("pivot_x").InnerText), float.Parse(node.SelectSingleNode("pivot_y").InnerText)),
                MinAngle = float.Parse(node.SelectSingleNode("min_angle").InnerText),
                MaxAngle = float.Parse(node.SelectSingleNode("max_angle").InnerText),
                MaxSpeedWobble = bool.Parse(node.SelectSingleNode("max_speed_wobble").InnerText),
                WobbleAngle = float.Parse(node.SelectSingleNode("wobble_angle").InnerText),
                WobblePeriod = float.Parse(node.SelectSingleNode("wobble_period_ms").InnerText)
            };
        }
        private static StuntLightConfig ReadLightConfig(XmlNode node)
        {
            return new StuntLightConfig
            {
                Show = bool.Parse(node.SelectSingleNode("show").InnerText),
                Position = new Vector2(float.Parse(node.SelectSingleNode("x").InnerText), float.Parse(node.SelectSingleNode("y").InnerText)),
                Colour = ParseHTMLColour(node.SelectSingleNode("colour").InnerText)
            };
        }
        private static SpeedConfig ReadSpeedConfig(XmlNode node)
        {
            return new SpeedConfig()
            {
                Show = bool.Parse(node.SelectSingleNode("show").InnerText),
                Position = new Vector2(float.Parse(node.SelectSingleNode("x").InnerText), float.Parse(node.SelectSingleNode("y").InnerText)),
                FontName = node.SelectSingleNode("font_name").InnerText,
                FontScale = float.Parse(node.SelectSingleNode("font_scale").InnerText),
                FontSpacing = float.Parse(node.SelectSingleNode("font_spacing").InnerText),
                TextFormat = node.SelectSingleNode("text_format").InnerText,
                TextCentred = bool.Parse(node.SelectSingleNode("text_centred").InnerText),
                CarMaxSpeed = float.Parse(node.SelectSingleNode("car_max_speed").InnerText),
                BoatMaxSpeed = float.Parse(node.SelectSingleNode("boat_max_speed").InnerText),
                PlaneMaxSpeed = float.Parse(node.SelectSingleNode("plane_max_speed").InnerText)
            };
        }

        private static BoostLevelConfig ReadBoostConfig(XmlNode node)
        {
            return new BoostLevelConfig()
            {
                Show = bool.Parse(node.SelectSingleNode("show").InnerText),
                Position = new Vector2(float.Parse(node.SelectSingleNode("x").InnerText), float.Parse(node.SelectSingleNode("y").InnerText)),
                FontName = node.SelectSingleNode("font_name").InnerText,
                FontScale = float.Parse(node.SelectSingleNode("font_scale").InnerText),
                FontSpacing = float.Parse(node.SelectSingleNode("font_spacing").InnerText),
                TextFormat = node.SelectSingleNode("text_format").InnerText,
                TextCentred = bool.Parse(node.SelectSingleNode("text_centred").InnerText),
                HideBoostLevelZero = bool.Parse(node.SelectSingleNode("hide_boost_level_zero").InnerText)
            };
        }
        private static VehicleFormConfig ReadFormConfig(XmlNode node)
        {
            return new VehicleFormConfig()
            {
                Show = bool.Parse(node.SelectSingleNode("show").InnerText),
                Position = new Vector2(float.Parse(node.SelectSingleNode("x").InnerText), float.Parse(node.SelectSingleNode("y").InnerText)),
                FontName = node.SelectSingleNode("font_name").InnerText,
                FontScale = float.Parse(node.SelectSingleNode("font_scale").InnerText),
                FontSpacing = float.Parse(node.SelectSingleNode("font_spacing").InnerText),
                TextFormat = node.SelectSingleNode("text_format").InnerText,
                TextCentred = bool.Parse(node.SelectSingleNode("text_centred").InnerText)
            };
        }

        public static Color ParseHTMLColour(string colour)
        {
            System.Drawing.Color tmp = System.Drawing.ColorTranslator.FromHtml(colour);
            return new Color(tmp.R, tmp.G, tmp.B);
        }
    }
}
