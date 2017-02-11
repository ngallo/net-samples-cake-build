/********** ARGUMENTS **********/

var target = Argument("target", "Default");

/********** GLOBAL VARIABLES **********/

var solutionPath = "./src/CakeBuildSample.sln";

var packagesArtifactsPath  = "./src/packages";

var nUnit3ToolPathPath = "./src/packages/NUnit.ConsoleRunner.3.6.0/tools/nunit3-console.exe";
var nUnit3ResultsPath = "./output/tests/result.xml";
var nUnit3TestPath = "./src/**/bin/Debug/*.Tests.dll";

var reportGeneratorToolPath = "./src/packages/ReportGenerator.2.5.2/Tools/reportgenerator.exe";
var reportGeneratorFilter1 = "+[*]*";
var reportGeneratorFilter2 = "-[*Tests*]*";
var openCoverToolPath = "./src/packages/OpenCover.4.6.519/tools/OpenCover.Console.exe";

var outputArtifactsPath = "./output";
var outputTestsPath = "./output/tests";
var outputTestsResultPath = "./output/tests/result.xml";
var outputReportGeneratorPath = "./output/tests/html";

/********** TARGETS **********/

Task("Clean")
    .Does(() =>
{
    CleanDirectories(new DirectoryPath[] { Directory(packagesArtifactsPath), Directory(outputArtifactsPath) });
});

Task("Restore")
    .Does(() =>
{
    NuGetRestore(MakeAbsolute(File(solutionPath)));
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
    MSBuild(MakeAbsolute(File(solutionPath)));
});

Task("Output")
    .Does(() =>
{
    CreateDirectory(outputTestsPath);
});

Task("Tests")
    .Does(() =>
{
    OpenCover(tool => {
        tool.NUnit3(nUnit3TestPath,
          new NUnit3Settings {
              ToolPath = GetFiles(nUnit3ToolPathPath).First()
            , Results = MakeAbsolute(File(nUnit3ResultsPath))
        });
      },
      new FilePath(outputTestsResultPath),
      new OpenCoverSettings() {
            ToolPath = openCoverToolPath
        }
        .WithFilter(reportGeneratorFilter1)
        .WithFilter(reportGeneratorFilter2));
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