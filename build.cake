/********** ARGUMENTS **********/

var target = Argument("target", "Default");

/********** GLOBAL VARIABLES **********/

var solutionFiles = GetFiles("./**/*.sln");

var packagesArtifactsDir  = Directory("./src/packages");

var nUnit3ToolPath = GetFiles("./src/packages/NUnit.ConsoleRunner.3.6.0/tools/nunit3-console.exe").First();
var nUnit3Results = MakeAbsolute(File("./output/tests/result.xml"));
var nUnit3TestPath = "./src/**/bin/DEbug/*.Tests.dll";

var reportGeneratorToolPath = "./src/packages/ReportGenerator.2.5.2/Tools/reportgenerator.exe";
var openCoverToolPath = "./src/packages/OpenCover.4.6.519/tools/OpenCover.Console.exe";

var outputArtifactsDir  = Directory("./output");
var outputTestsPath = "./output/tests";
var outputTestsResultPath = string.Format("{0}/result.xml", outputTestsPath);
var outputReportGeneratorPath = string.Format("{0}/html", outputTestsPath);

/********** TARGETS **********/

Task("Clean")
    .Does(() =>
{
    CleanDirectories(new DirectoryPath[] { packagesArtifactsDir, outputArtifactsDir });
});

Task("Restore")
    .Does(() =>
{
    foreach(var solutionFile in solutionFiles)
    {
        NuGetRestore(solutionFile);
    }
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
    foreach(var solutionFile in solutionFiles)
    {
        MSBuild(solutionFile);
    }
});

Task("Output")
    .Does(() =>
{
    CreateDirectory(outputTestsPath);
});

Task("Tests")
    .Does(() =>
{
    NUnit3(nUnit3TestPath,
        new NUnit3Settings {
            ToolPath = nUnit3ToolPath
            , Results = nUnit3Results
    });
    ReportGenerator(outputTestsResultPath, outputReportGeneratorPath, new ReportGeneratorSettings() {
      ToolPath = reportGeneratorToolPath
    });
});

Task("CI")
  .IsDependentOn("Build")
  .IsDependentOn("Output")
  .IsDependentOn("Tests")
  .Does(() => 
{
    Information("CI Build completed :-)");
});

Task("Default")
  .IsDependentOn("CI");

RunTarget(target);