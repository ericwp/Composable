﻿using System;
using AccountManagement.API;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.UI.MVC.Views.Register
{
    public class RegisterController : Controller
    {
        readonly IServiceBus _serviceBus;
        public RegisterController(IServiceBus serviceBus) => _serviceBus = serviceBus;

        public IActionResult Register(AccountResource.Command.Register registrationCommand)
        {
            if(!ModelState.IsValid) return View("RegistrationForm");

            var result = _serviceBus.PostRemote(registrationCommand);
            switch(result)
            {
                case AccountResource.Command.Register.RegistrationAttemptResult.Successful:
                    return View("ValidateYourEmail", _serviceBus.Execute(NavigationSpecification.GetRemote(AccountApi.Start).GetRemote(start => start.Queries.AccountById.WithId(registrationCommand.AccountId))));
                case AccountResource.Command.Register.RegistrationAttemptResult.EmailAlreadyRegistered:
                    ModelState.AddModelError(nameof(registrationCommand.Email), "Email is already registered");
                    return View("RegistrationForm");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IActionResult RegistrationForm() => View("RegistrationForm", (_serviceBus.GetRemote(AccountApi.Start)).Commands.Register);
    }
}
