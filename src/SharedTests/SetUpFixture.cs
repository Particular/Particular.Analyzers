using Particular.AnalyzerTesting;
using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public void OneTimeSetup() => AnalyzerTest.ConfigureAllAnalyzerTests(test =>
    {
        test.WithSource(ExternalTypes, "ExternalTypes.cs")
            .WithCommonUsings(commonUsings);
    });

    const string ExternalTypes = """
                                 namespace NServiceBus
                                 {
                                    interface ICancellableContext { }
                                    class CancellableContext : ICancellableContext { }
                                    interface IMessage { }
                                 }
                                 """;

    static readonly string[] commonUsings = ["System", "System.Threading", "System.Threading.Tasks", "NServiceBus"];
}