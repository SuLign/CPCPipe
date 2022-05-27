using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

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

        public T GetValue<T>()
        {
            if (typeof(T) == ValueType)
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(Value.ToString());
                }
                catch
                {
                    return default;
                }
            }
            else
            {
                return default;
            }
        }

        public object GetValue(Type type)
        {
            if (type == ValueType)
            {
                try
                {
                    return JsonConvert.DeserializeObject(Value.ToString(), type);
                }
                catch
                {
                    return default;
                }
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
