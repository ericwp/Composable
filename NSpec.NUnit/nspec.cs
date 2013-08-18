﻿using System;
using System.Collections.Generic;
using System.Linq;
using NSpec.Domain;
using NSpec.Domain.Extensions;
using NSpec.Domain.Formatters;
using NUnit.Framework;

namespace NSpec.NUnit
{
    /// <summary>
    /// This class acts as as shim between NSpec and NUnit. If you inherit it instead of <see cref="NSpec.nspec"/> you can work as usual with nspec and nunit will execute your tests for you.
    /// </summary>
    [TestFixture]
    // ReSharper disable InconsistentNaming
    public abstract class nspec : NSpec.nspec
        // ReSharper restore InconsistentNaming
    {
        [Test]
        public void ValidateSpec()
        {
            var finder = new SpecFinder(new[] {GetType()});
            var builder = new ContextBuilder(finder, new DefaultConventions());

            ContextCollection result = new ContextRunner(builder, new MyFormatter(), false).Run(builder.Contexts().Build());

            if(result.Failures() == null)
            {
                Assert.Fail("*****   Failed to execute some tests   *****");
            }

            var crashes = result.AllContexts().Where(context => context.Exception != null).ToList();
            if(crashes.Any())
            {
                throw new SpecificationException("unknown", crashes.First().Exception);
            }

            if(result.Failures().Any())
            {
                throw new SpecificationException("unknown", result.First().Exception);
            }
        }

        public class MyFormatter : ConsoleFormatter, IFormatter, ILiveFormatter
        {
            private static void WriteNoticably(string message, params object[] formatwith)
            {
                message = String.Format("#################################    {0}    #################################", message);
                Console.WriteLine(message, formatwith);
            }

            void IFormatter.Write(ContextCollection contexts)
            {
                //Not calling base here lets us get rid of its noisy stack trace output so it does not obscure our thrown exceptions stacktrace.
                Console.WriteLine();
                if(contexts.Failures().Any())
                {
                    WriteNoticably("SUMMARY");                    
                    Console.WriteLine(base.Summary(contexts));

                    int currentFailure = 0;
                    foreach (var failure in contexts.Failures())
                    {
                        Console.WriteLine();
                        Console.Write("#################################  FAILURE {0} #################################", ++currentFailure);

                        var current = failure.Context;
                        var relatedContexts = new List<Context>() { current };
                        while (null != (current = current.Parent))
                        {
                            relatedContexts.Add(current);
                        }

                        var levels = relatedContexts.Select(me => me.Name)
                                                    .Reverse()
                                                    .Skip(1)
                                                    .Concat(new string[] { failure.Spec + " - " + failure.Exception.Message });

                        var message = levels
                            .Select((name, level) => "\t".Times(level) + name)
                            .Aggregate(Environment.NewLine + "at: ", (agg, curr) => agg + curr + Environment.NewLine);

                        Console.WriteLine(message);

                        Console.WriteLine(base.WriteFailure(failure));                        
                    }
                }

                

                Console.WriteLine();
                WriteNoticably("END OF NSPEC RESULTS");                
                Console.WriteLine();
            }

            void ILiveFormatter.Write(Context context)
            {
                base.Write(context);
            }

            void ILiveFormatter.Write(Example e, int level)
            {
                base.Write(e, level);
            }
        }
    }

    public class SpecificationException : Exception
    {
        public SpecificationException(string position, Exception exception) : base(position, exception) {}
    }
}
