namespace XAMLTools.Tests;

using System.Diagnostics;
using DiffEngine;

[SetUpFixture]
public class AssemblySetup
{
    [OneTimeSetUp]
    public static void SetUp()
    {
        DiffRunner.Disabled = Debugger.IsAttached == false;
    }
}