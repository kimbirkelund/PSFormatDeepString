using System;
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

        public FormatDeepStringTest([NotNull] ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        [Fact]
        public void TestFormatDeepStringCmdlet()
        {
            using (var powerShell = PowerShell.Create())
            {
                powerShell.AddCommand("Import-Module")
                          .AddParameter("Assembly", typeof(FormatDeepStringCmdlet).Assembly)
                          .Invoke()
                          .Should()
                          .BeEmpty();

                var result = powerShell.AddScript("function bar() { throw [exception]::new('bob'); }")
                                       .AddScript("function foo() { bar; }")
                                       .AddScript("try { foo; } catch { $_ | Format-DeepString }")
                                       .Invoke<string>()
                                       .Single();
                result.Should()
                      .NotBeNull();

                _outputHelper.WriteLine(result);
            }
        }
    }
}
