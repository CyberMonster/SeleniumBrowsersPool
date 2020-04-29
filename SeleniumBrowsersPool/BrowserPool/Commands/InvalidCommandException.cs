using System;

namespace SeleniumBrowsersPool.BrowserPool.Commands
{
    [Serializable]
    public class InvalidCommandException : Exception
    {
        public InvalidCommandException(IBrowserCommand command) : base(GetMessage(command)) { }

        private static string GetMessage(IBrowserCommand command)
            => $"Invocation of {command.GetType().Name} was illegal in current context.";
    }
}
