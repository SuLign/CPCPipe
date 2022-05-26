using System;
using System.Collections.Generic;
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

        public T GetValue<T>()
        {
            if (typeof(T) == ValueType)
            {
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
