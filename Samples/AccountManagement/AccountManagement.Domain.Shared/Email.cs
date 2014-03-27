﻿using System;
using System.Text.RegularExpressions;
using Composable.DDD;
using Composable.System;

namespace AccountManagement.Domain.Shared
{
    ///<summary>
    /// A small value object that ensures that it is impossible to create an invalid email.
    /// This frees all users of the class from ever having to validated an email.
    /// As long as it is not null it is guaranteed to be valid.
    /// </summary>
    public class Email : ValueObject<Email>
    {
        private string Value { get; set; }

        override public string ToString()
        {
            return Value;
        }

        private Email(string emailAddress)
        {
            Validate(emailAddress);
            Value = emailAddress;
        }

        public static bool IsValidEmail(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                return false;
            }

            var regex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$");
            var isMatch = regex.IsMatch(emailAddress);

            if (!isMatch)
            {
                return false;
            }

            if (emailAddress.Contains(".."))
            {
                return false;
            }

            if (emailAddress.Contains("@.") || emailAddress.Contains(".@"))
            {
                return false;
            }
            return true;
        }

        public static Email Parse(string emailAddress)
        {
            return new Email(emailAddress);
        }

        //Note how all the exceptions contain the invalid email address. Always make sure that exceptions contain the relevant information.
        private static void Validate(string emailAddress)
        {
            if(!IsValidEmail(emailAddress))
            {
                throw new InvalidEmailException(emailAddress ?? "[null]");
            }           
        }
    }

    public class InvalidEmailException : ArgumentException
    {
        public InvalidEmailException(string message) : base(message) {}
    }
}
