﻿using System;
using System.Linq;

namespace AccountManagement.Domain.Shared
{
    /// <summary>
    /// Note how all the business logic of a secure password is encapsulated and the instance is immutable after being created.
    /// </summary>
    public partial class Password
    {
        public byte[] HashedPassword { get; private set; }
        public byte[] Salt { get; private set; }

        public Password(string password)
        {
            Policy.AssertPasswordMatchesPolicy(password); //Use a nested class to make the policy easily discoverable while keeping this class short and expressive.
            Salt = Guid.NewGuid().ToByteArray();
            HashedPassword = PasswordHasher.HashPassword(salt: Salt, password: password);
        }

        /// <summary>
        /// Returns true if the supplied password parameter is the same string that was used to create this password.
        /// In other words if the user should succeed in logging in using that password.
        /// </summary>
        public bool IsCorrectPassword(string password) => HashedPassword.SequenceEqual(PasswordHasher.HashPassword(Salt, password));

        public void AssertIsCorrectPassword(string attemptedPassword)
        {
            if(!IsCorrectPassword(attemptedPassword))
            {
                throw new WrongPasswordException();
            }
        }
    }
}
