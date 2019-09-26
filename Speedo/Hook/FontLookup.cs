using System;
using System.Collections.Generic;
using System.Xml;

namespace Speedo.Hook
{
    public struct FontLocation
    {
        public int x;
        public int y;
        public int width;
        public int height;
        public char letter;
    }

    public static class FontLookup
    {
        public static FontLocation[] ReadXML(string xmlPath)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlPath);
            XmlNodeList charElements = xmlDocument.SelectNodes("/root/char");

            int length = charElements.Count;
            FontLocation[] fontLocations = new FontLocation[length];
            XmlNode element;
            for (int i = 0; i < length; i++)
            {
                element = charElements[i];
                fontLocations[i] = new FontLocation()
                {
                    letter = element.Attributes["id"].Value[0],
                    x = Convert.ToInt32(element.SelectSingleNode("x").InnerText),
                    y = Convert.ToInt32(element.SelectSingleNode("y").InnerText),
                    width = Convert.ToInt32(element.SelectSingleNode("width").InnerText),
                    height = Convert.ToInt32(element.SelectSingleNode("height").InnerText)
                };
            }

            return fontLocations;
        }

        public static FontLocation FindLetterLocation(FontLocation[] FontLocations, char letter)
        {
            return Array.Find(FontLocations, a => a.letter == letter);
        }
    }
}