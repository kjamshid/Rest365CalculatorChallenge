using System;

namespace Calculator.Common
{
    public class NegativeNumberException : Exception
    {
        public NegativeNumberException()
        {

        }

        public NegativeNumberException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        public NegativeNumberException(string message)
            : base(message)
        {

        }
    }
}
