﻿namespace Composable.CQRS.Windsor.Testing
{
    ///<summary>Component that changes container wiring to enable testing.</summary>
    interface IConfigureWiringForTests
    {
        ///<summary>Changes wiring in the container to be appropriate for testing.</summary>
        void ConfigureWiringForTesting();
    }
}