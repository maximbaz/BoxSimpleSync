using System;

namespace BoxSimpleSync.API.Exceptions
{
    public class AuthenticationException : Exception
    {
        public AuthenticationException(string information) : base("Cannot get authentication " + information) {}

        public AuthenticationException() : this("information") {}
    }
}