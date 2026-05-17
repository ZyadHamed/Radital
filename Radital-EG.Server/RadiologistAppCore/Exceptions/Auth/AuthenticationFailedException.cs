using System;
using System.Collections.Generic;
using System.Text;

namespace RadiologistAppCore.Exceptions.Auth
{
    public class AuthenticationFailedException : Exception
    {
       public AuthenticationFailedException(string message) : base(message) { }
    }
}
