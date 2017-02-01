using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Diagnostics;

using Synapse.Core.Utilities;

namespace Synapse.Handlers.CommandLine
{
    public static class HandlerUtils
    {
        public static T Deserialize<T>(String str)
        {
            T obj;
            if (str.Trim().StartsWith("<"))
                try
                {
                    obj = XmlHelpers.Deserialize<T>(new StringReader(str));
                }
                catch (Exception e)
                {
                    // Check Edge Case Of Yaml Document Starting With A "<" Character
                    try
                    {
                        obj = YamlHelpers.Deserialize<T>(new StringReader(str));
                    }
                    catch (Exception)
                    {
                        throw e;
                    }
                }
            else
                obj = YamlHelpers.Deserialize<T>(new StringReader(str));

            return obj;
        }

        public static String Serialize<T>(object obj)
        {
            String str = String.Empty;

            if (obj.GetType() == typeof(XmlNode[]))
            {
                StringBuilder sb = new StringBuilder();
                String type = typeof(T).Name;
                sb.Append("<" + type + ">");
                XmlNode[] nodes = (XmlNode[])obj;
                foreach (XmlNode node in nodes)
                    sb.Append(node.OuterXml);
                sb.Append("</" + type + ">");
                str = sb.ToString();
            }
            else if (obj.GetType() == typeof(Dictionary<object, object>))
                str = YamlHelpers.Serialize(obj);
            else
                str = obj.ToString();

            return str;
        }

        public static double ElapsedSeconds(this Stopwatch stopwatch)
        {
            return TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds).TotalSeconds;
        }

    }
}
