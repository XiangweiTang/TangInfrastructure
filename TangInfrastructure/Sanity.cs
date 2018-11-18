using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangInfrastructure
{
    public static class Sanity
    {
        static Sanity() { }
        public static void Requires(bool valid, string message)
        {
            if (!valid)
                throw new TangInfrastructureException(message);
        }

        public static void Requires(bool valid)
        {
            Requires(valid, "TangInfrastructure Exception.");
        }

        public static void RequiresWave(bool valid, string message)
        {
            Requires(valid, "Wave error:\t" + message);
        }

        public static void RequiresWave(bool valid)
        {
            Requires(valid, "Wave error.");
        }

        public static void RequiresLine(bool valid, string lineType)
        {
            Requires(valid, $"Invalid {lineType} line.");
        }

        public static void RequiresLine(bool valid)
        {
            Requires(valid, "Invalid line.");
        }
    }

    public class TangInfrastructureException : Exception
    {
        public TangInfrastructureException(string message) : base(message) { }
        public TangInfrastructureException() : base() { }
    }
}
