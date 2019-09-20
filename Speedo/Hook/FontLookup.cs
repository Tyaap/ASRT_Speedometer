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

    public class FontLookup
    {
        public FontLocation[] Location;

        public FontLookup(string xmlPath)
        {
            ReadXML(xmlPath);
        }

        public void ReadXML(string xmlPath)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlPath);
            XmlNodeList charElements = xmlDocument.SelectNodes("/root/char");

            int length = charElements.Count;
            Location = new FontLocation[length];
            XmlNode element;
            for (int i = 0; i < length; i++)
            {
                element = charElements[i];
                Location[i] = new FontLocation()
                {
                    letter = element.Attributes["id"].Value[0],
                    x = Convert.ToInt32(element.SelectSingleNode("x").InnerText),
                    y = Convert.ToInt32(element.SelectSingleNode("y").InnerText),
                    width = Convert.ToInt32(element.SelectSingleNode("width").InnerText),
                    height = Convert.ToInt32(element.SelectSingleNode("height").InnerText)
                };
            }
        }

        public FontLocation FindLetterLocation(char letter)
        {
            return Array.Find(Location, a => a.letter == letter);
        }
    }
}