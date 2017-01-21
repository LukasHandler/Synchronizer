using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Synchronizer.ApplicationLogic
{
    public static class Serializer
    {
        /// <summary>
        /// Serializes an object. From <see href="http://stackoverflow.com/questions/6115721/how-to-save-restore-serializable-object-to-from-file"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializableObject"></param>
        /// <param name="fileName"></param>
        public static void SerializeObject<T>(T serializableObject, string fileName)
        {
            if (serializableObject == null)
            {
                return;
            }

            string serialized = JsonConvert.SerializeObject(serializableObject);
            File.WriteAllText(fileName, serialized);
        }


        /// <summary>
        /// Deserializes an xml file into an object list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static T DeSerializeObject<T>(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return default(T);
            }

            string serialized = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        public static T CopyObject<T>(T objectToCopy)
        {
            if (objectToCopy == null)
            {
                return default(T);
            }

            string serialized = JsonConvert.SerializeObject(objectToCopy);
            return JsonConvert.DeserializeObject<T>(serialized);
        }
    }
}
