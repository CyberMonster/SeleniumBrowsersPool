using System;

namespace SeleniumBrowsersPool.BrowserPool.Commands
{

    [Serializable]
    public class InvalidCommandException : Exception
    {
        public InvalidCommandException(IBrowserCommand command) : base(GetMessage(command)) { }

        private static string GetMessage(IBrowserCommand command)
            => $"Invocation of {command.GetType().Name} was illegal in current context.";

        protected InvalidCommandException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
