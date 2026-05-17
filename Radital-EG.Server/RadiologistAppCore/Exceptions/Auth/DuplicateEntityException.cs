using System;
using System.Collections.Generic;
using System.Text;

namespace RadiologistAppCore.Exceptions.Auth
{
    public class DuplicateEntityException : Exception
    {
        public DuplicateEntityException(string message) : base(message) { }
    }
}
