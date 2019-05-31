// NUnit 3 tests
// See documentation : https://github.com/nunit/docs/wiki/NUnit-Documentation
using NUnit.Framework;
using System;
using Synapse.Handlers.CommandLine;
using Synapse.Core;
using System.IO;
using Synapse.Core.Utilities;
using System.Collections.Generic;

// BP Machine Windows 7: Make sure the 'Server' service is running

namespace Synapse.Handlers.CommandLine.UnitTests
{
    [TestFixture]
    public class ScriptHandlerTests
    {
        public static string _root = null;
        public static string _plansRoot = null;
        public static string _inputFiles = null;
        public static string _outputFiles = null;
        const string INPUT_DIR__ = "$INPUT_DIR__";

        [OneTimeSetUp]
        public void Init()
        {
            _root = Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location );
            Directory.SetCurrentDirectory( $@"{_root}\..\.." );
            _root = Directory.GetCurrentDirectory();
            _plansRoot = $@"{_root}\Plans";
            _inputFiles = $@"{_plansRoot}\InputFiles";
            _outputFiles = $@"{_plansRoot}\OutputFiles";
        }
        [Test]
        public void WorkingDirectory_DoesNotExist_ReturnFail()
        {
            string planFile = "script-local-invalid-working-dir.yaml";
            Plan plan = Plan.FromYaml( $@"{_plansRoot}\{planFile}" );

            //string parmString = plan.Actions[0].Parameters.GetSerializedValues();
            //string newParmString = parmString.Replace( INPUT_DIR__, _inputFiles );
            //plan.Actions[0].Parameters.Values = YamlHelpers.Deserialize<Dictionary<object, object>>( newParmString );

            plan.Start( null, false, true );

            string status = plan.ResultPlan.Actions[0].Result.Status.ToString();
            Assert.AreEqual( StatusType.Failed.ToString(), status );
            string exitData = plan.ResultPlan.Actions[0].Result.ExitData.ToString();
            Assert.That( exitData, Does.Contain( "ERROR : Working Directory Not Found." ) );
        }
        [Test]
        public void Script_DoesNotExist_ReturnFailed()
        {
            string planFile = "script-local-invalid-file.yaml";
            Plan plan = Plan.FromYaml( $@"{_plansRoot}\{planFile}" );

            plan.Start( null, false, true );

            string status = plan.ResultPlan.Actions[0].Result.Status.ToString();
            Assert.AreEqual( StatusType.Failed.ToString(), status );
            string exitData = plan.ResultPlan.Actions[0].Result.ExitData.ToString();
            Assert.That( exitData, Does.Contain( "ERROR : Script Not Found." ) );
        }
        [Test]
        public void BothScriptAndScriptBlock_NotEmpty_ReturnFailed()
        {
            string planFile = "script-local-with-script-and-scriptblock.yaml";
            Plan plan = Plan.FromYaml( $@"{_plansRoot}\{planFile}" );

            plan.Start( null, false, true );

            string status = plan.ResultPlan.Actions[0].Result.Status.ToString();
            Assert.AreEqual( StatusType.Failed.ToString(), status );
            string exitData = plan.ResultPlan.Actions[0].Result.ExitData.ToString();
            Assert.That( exitData, Does.Contain( "ERROR : Script and ScriptBlock Exist In Same Action." ) );
        }
        [Test]
        [TestCase( "script-localhost.yaml", "simple.out" )]
        [TestCase( "script-local-batch-script-simple.yaml", "simple.out" )]
        [TestCase( "script-local-batch-file-simple.yaml", "simple.out" )]
        [TestCase( "script-local-powershell-script-simple.yaml", "simple.out" )]
        [TestCase( "script-local-powershell-file-simple.yaml", "simple.out" )]
        [TestCase( "script-local-powershell-script-regex-base64.yaml", "regex-base64.out" )]
        [TestCase( "script-local-powershell-script-parms-sub.yaml", "parms-sub.out" )]
        public void TestParameters(string planFile, string outFile)
        {
            Plan plan = Plan.FromYaml( $@"{_plansRoot}\{planFile}" );
            string parmString = plan.Actions[0].Parameters.GetSerializedValues();
            string newParmString = parmString.Replace( INPUT_DIR__, _inputFiles );
            plan.Actions[0].Parameters.Values = YamlHelpers.Deserialize<Dictionary<object, object>>( newParmString );

            plan.Start( null, false, true );

            string status = plan.ResultPlan.Actions[0].Result.Status.ToString();
            Assert.AreEqual( StatusType.Complete.ToString(), status );

            string expectedResult = File.ReadAllText( $@"{_outputFiles}\{outFile}" );
            string actualResult = plan.ResultPlan.Actions[0].Result.ExitData.ToString();
            Console.WriteLine( actualResult );
            Assert.AreEqual( expectedResult, actualResult );
            //string actualOutput = plan.ResultPlan.Actions[0].Result.ExitData.ToString().TrimEnd('\n').TrimEnd('\r');
            //Assert.AreEqual( "aaa bbb ccc", actualOutput );
        }
        [Test]
        public void Timeout_Occurred_ReturnTimeoutStatus()
        {
            string planFile = "script-local-timeout.yaml";
            Plan plan = Plan.FromYaml( $@"{_plansRoot}\{planFile}" );

            string parmString = plan.Actions[0].Parameters.GetSerializedValues();
            string newParmString = parmString.Replace( INPUT_DIR__, _inputFiles );
            plan.Actions[0].Parameters.Values = YamlHelpers.Deserialize<Dictionary<object, object>>( newParmString );

            plan.Start( null, false, true );

            string status = plan.ResultPlan.Actions[0].Result.Status.ToString();
            Assert.AreEqual( StatusType.Cancelled.ToString(), status );
        }
        [Test]
        public void ExitCode_MatchFound_ReturnMatchedStatus()
        {
            string planFile = "script-local-exitcode-matched.yaml";
            Plan plan = Plan.FromYaml( $@"{_plansRoot}\{planFile}" );

            string parmString = plan.Actions[0].Parameters.GetSerializedValues();
            string newParmString = parmString.Replace( INPUT_DIR__, _inputFiles );
            plan.Actions[0].Parameters.Values = YamlHelpers.Deserialize<Dictionary<object, object>>( newParmString );

            plan.Start( null, false, true );

            string status = plan.ResultPlan.Actions[0].Result.Status.ToString();
            Assert.AreEqual( StatusType.CompletedWithErrors.ToString(), status );
        }
        [Test]
        public void ExitCode_MatchNotFound_ReturnFailed()
        {
            string planFile = "script-local-exitcode-notmatched.yaml";
            Plan plan = Plan.FromYaml( $@"{_plansRoot}\{planFile}" );

            string parmString = plan.Actions[0].Parameters.GetSerializedValues();
            string newParmString = parmString.Replace( INPUT_DIR__, _inputFiles );
            plan.Actions[0].Parameters.Values = YamlHelpers.Deserialize<Dictionary<object, object>>( newParmString );

            plan.Start( null, false, true );

            string status = plan.ResultPlan.Actions[0].Result.Status.ToString();
            Assert.AreEqual( StatusType.Failed.ToString(), status );
        }
        [Test]
        public void TestRemote()
        {
            // dummy test to flag no testing on Remote server
            throw new Exception( "Remote servers not tested" );
        }
        [Test]
        public void TestRunAs()
        {
            // dummy test to flag no testing on RunAs
            throw new Exception( "RunAs not tested" );
        }
    }
}
