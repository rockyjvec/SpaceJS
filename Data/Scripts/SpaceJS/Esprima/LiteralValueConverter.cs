using System;
using System.Text.RegularExpressions;

namespace Esprima
{
/*    public class LiteralValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Regex)
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
            }
            else if (value is double && (value as double) % 1 < double.Epsilon)
            {
                try
                {
                    serializer.Serialize(writer, Convert.ToUInt64(d));
                }
                catch
                {
                    serializer.Serialize(writer, (value as double));
                }
            }
            else
            {
                serializer.Serialize(writer, value);
            }
        }
    }*/
}
