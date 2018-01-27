﻿using System;
using AccountManagement.API;
using FluentAssertions;
using FluentAssertions.Specialized;

namespace AccountManagement.Scenarios
{
    public abstract class ScenarioBase<TResult>
    {
        protected AccountApi Api => AccountApi.Instance;

        public abstract TResult Execute();


        public ExceptionAssertions<TException> ExecutingShouldThrow<TException>() where TException : Exception => this.Invoking(@this => @this.Execute()).ShouldThrow<TException>();
    }
}
