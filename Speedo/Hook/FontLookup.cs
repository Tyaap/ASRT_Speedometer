using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Speedo.Hook
{
    public static class FontLookup
    {
        public static FontLocation[] Location = new FontLocation[10];

        public static void ReadXML()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(AppContext.BaseDirectory + "\\Resources\\font.xml");
            XmlNodeList elementsByTagName = xmlDocument.GetElementsByTagName("char");
            for (int index = 0; index < elementsByTagName.Count; ++index)
            {
                Location[index].letter = elementsByTagName[index].Attributes["id"].Value[0].ToString().First();
                Location[index].x = Convert.ToInt32(elementsByTagName[index].ChildNodes.Item(0).InnerText);
                Location[index].y = Convert.ToInt32(elementsByTagName[index].ChildNodes.Item(1).InnerText);
                Location[index].width = Convert.ToInt32(elementsByTagName[index].ChildNodes.Item(2).InnerText);
                Location[index].height = Convert.ToInt32(elementsByTagName[index].ChildNodes.Item(3).InnerText);
            }
        }

        public static FontLocation FindLetterLocation(char letter)
        {
            for (int index = 0; index < ((IEnumerable<FontLocation>)Location).Count(); ++index)
            {
                if (Location[index].letter == letter)
                {
                    return Location[index];
                }
            }
            return Location[1];
        }
    }
}
