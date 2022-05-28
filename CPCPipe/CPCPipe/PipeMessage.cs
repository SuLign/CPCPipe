using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CPCPipe
{
    public class PipeMessage
    {
        public string MessageName { get; set; }
        public object Value { get; set; }
        public Type ValueType { get; set; }

        public PipeMessage()
        {

        }

        private PipeMessage(string messageName, object value, Type valueType)
        {
            MessageName = messageName;
            Value = value;
            ValueType = valueType;
        }

        public object GetValue(Type type)
        {
            if (type == ValueType)
            {
                if (ValueType != typeof(JObject) && Value is JObject v)
                {
                    return JsonConvert.DeserializeObject((string)v.ToString(), type);
                }
                else if (ValueType != typeof(JArray) && Value is JArray vs)
                {
                    if (type.IsArray)
                    {
                        var arr = Array.CreateInstance(type.GetElementType(), vs.Count);
                        Array.Copy(vs.Select(e => e.ToObject(type.GetElementType())).ToArray(), arr, vs.Count);
                        return arr;
                    }
                }
                return Value;
            }
            else
            {
                return default;
            }
        }



        public T GetValue<T>()
        {
            if (typeof(T) == ValueType)
            {
                if (ValueType != typeof(string) && Value is string v)
                {
                    return JsonConvert.DeserializeObject<T>((string)Value);
                }
                return (T)Value;
            }
            else
            {
                return default;
            }
        }

        public static PipeMessage CreateMessage<T>(string messageName, T value)
        {
            return new PipeMessage(messageName, value, typeof(T));
        }
    }
}
