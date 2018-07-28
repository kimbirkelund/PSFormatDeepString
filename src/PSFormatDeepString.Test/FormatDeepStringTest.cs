using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace PSFormatDeepString.Test
{
    public class FormatDeepStringTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public static IEnumerable<object[]> TestCases { get; } = CreateTestCases()
                                                                 .Select(v => new object[] { v })
                                                                 .ToList();

        public FormatDeepStringTest([NotNull] ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TestFormatDeepStringCmdlet(IEnumerable<string> scripts)
        {
            using (var powerShell = CreatePowerShell())
            {
                var result = powerShell.InvokeScripts(scripts)
                                       .ToList();

                _outputHelper.WriteLine(string.Join("\n", result.Select(v => v + "")));

                result.Should()
                      .NotBeEmpty();
            }
        }


        private static PowerShell CreatePowerShell()
        {
            var powerShell = PowerShell.Create();

            powerShell.AddCommand("Import-Module")
                      .AddParameter("Assembly", typeof(FormatDeepStringCmdlet).Assembly)
                      .Invoke()
                      .Should()
                      .BeEmpty();

            return powerShell;
        }

        private static IEnumerable<string> CreateTestCase(params string[] scripts) => scripts;

        private static IEnumerable<IEnumerable<string>> CreateTestCases()
        {
            yield return CreateTestCase("function bar() { throw [exception]::new('bob'); }",
                                        "function foo() { bar; }",
                                        "try { foo; } catch { $_ | Format-DeepString }");

            yield return CreateTestCase("function bar() { throw [exception]::new('bob'); }",
                                        "function foo() { bar; }",
                                        "try { foo; } catch { Format-DeepString $_ }");

            yield return CreateTestCase("'bob' | Format-DeepString");

            yield return CreateTestCase("Format-DeepString 'bob'");

            yield return CreateTestCase("42 | Format-DeepString");
            yield return CreateTestCase("[Guid]::NewGuid() | Format-DeepString");

            yield return CreateTestCase("ls | select -First 3 | Format-DeepString");
        }
    }

    public static class PowerShellExtensions
    {
        public static IEnumerable<PSObject> InvokeScripts(this PowerShell powerShell, params string[] scripts)
        {
            return powerShell.InvokeScripts(scripts.AsEnumerable());
        }

        public static IEnumerable<PSObject> InvokeScripts(this PowerShell powerShell, IEnumerable<string> scripts)
        {
            return scripts.Aggregate(powerShell, (p, s) => p.AddScript(s))
                          .Invoke(new[] { "" },
                                  new PSInvocationSettings
                                  {
                                      ErrorActionPreference = ActionPreference.Stop
                                  });
        }
    }
}
