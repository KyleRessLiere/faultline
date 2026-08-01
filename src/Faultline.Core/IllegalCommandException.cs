using System;

namespace Faultline.Core
{
    /// <summary>
    /// Thrown when a command is not legal against the state it was applied to. A correct shell never
    /// sees this: it only ever submits commands drawn from <see cref="StepResult.LegalNext"/>.
    /// </summary>
    public sealed class IllegalCommandException : Exception
    {
        /// <summary>Creates the exception.</summary>
        /// <param name="message">Why the command was rejected.</param>
        public IllegalCommandException(string message)
            : base(message)
        {
        }

        /// <summary>Creates the exception.</summary>
        public IllegalCommandException()
        {
        }

        /// <summary>Creates the exception.</summary>
        /// <param name="message">Why the command was rejected.</param>
        /// <param name="innerException">Underlying cause.</param>
        public IllegalCommandException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
