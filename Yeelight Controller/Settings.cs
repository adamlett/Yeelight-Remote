using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static Yeelight_Controller.MainWindow;

namespace Yeelight_Controller
{

    public static class Settings
    {

        private static string directory = "C:/Yeelight/Settings/";

        public static T Deserialize<T>(this string toDeserialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            StringReader textReader = new StringReader(toDeserialize);
            return (T)xmlSerializer.Deserialize(textReader);
        }

        public static string Serialize<T>(this T toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            StringWriter textWriter = new StringWriter();
            xmlSerializer.Serialize(textWriter, toSerialize);
            return textWriter.ToString();
        }

        public static void WriteShortcutToFile(KBShortcut key)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(@directory + key.name, Serialize(key));
        }

        public static void WriteIPToFile(string IP)
        {
            File.WriteAllText(@directory + "IP", IP);
        }

        public static string ReadIPFromFile()
        {
            if (File.Exists(directory + "IP"))
            {
                return File.ReadAllText(directory + "IP");
            }
            else return null;
        }

        public static T restoreSetting<T>(string name)
        {
            if (File.Exists(directory + name))
            {
                return Deserialize<T>(File.ReadAllText(directory + name));
            }
            else return default(T);
        }


    }

}
