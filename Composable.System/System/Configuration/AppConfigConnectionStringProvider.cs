﻿using System;
using System.Configuration;
using Composable.System.Data.SqlClient;

namespace Composable.System.Configuration
{
    ///<summary>Supplies connection strings from the application configuration file.</summary>
    class AppConfigConnectionStringProvider : IConnectionStringProvider
    {
        ///<summary>Returns the connection string with the given name.</summary>
        public ISqlConnectionProvider GetConnectionProvider(string parameterName)
        {
            return new SqlServerConnectionProvider(new Lazy<string>(() =>
                                                                    {
                                                                        var parameter = ConfigurationManager.ConnectionStrings[parameterName];
                                                                        if(parameter == null)
                                                                        {
                                                                            throw new ConfigurationErrorsException($"ConnectionString with name {parameterName} does not exists");
                                                                        }
                                                                        return parameter.ConnectionString;
                                                                    }));
        }
    }
}
