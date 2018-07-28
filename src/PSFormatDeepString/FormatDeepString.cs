using System.Management.Automation;
using JetBrains.Annotations;

namespace PSFormatDeepString
{
    /// <summary>
    ///     A simple Cmdlet that outputs a greeting to the pipeline.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "DeepString")]
    [UsedImplicitly]
    public class FormatDeepStringCmdlet : Cmdlet
    {
        [ValidateNotNullOrEmpty]
        [Parameter(Position = 0, HelpMessage = "The object to format.", ValueFromPipeline = true)]
        public object InputObject { get; set; }

        /// <summary>
        ///     Perform Cmdlet processing.
        /// </summary>
        protected override void ProcessRecord()
        {
            var result = PrettyPrinter.Print(InputObject);
            WriteObject(result);
        }
    }
}
