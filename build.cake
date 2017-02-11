/********** ARGUMENTS **********/

var target = Argument("target", "Default");

/********** GLOBAL VARIABLES **********/

var solutionPath = "./src/CakeBuildSample.sln";

var packagesArtifactsDir  = Directory("./src/packages");

var nUnit3ToolPath = GetFiles("./src/packages/NUnit.ConsoleRunner.3.6.0/tools/nunit3-console.exe").First();
var nUnit3Results = MakeAbsolute(File("./output/tests/result.xml"));
var nUnit3TestPath = "./src/**/bin/Debug/*.Tests.dll";

var reportGeneratorToolPath = "./src/packages/ReportGenerator.2.5.2/Tools/reportgenerator.exe";
var reportGeneratorFilter1 = "+[*]*";
var reportGeneratorFilter2 = "-[*Tests*]*";
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
              ToolPath = nUnit3ToolPath
            , Results = nUnit3Results
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