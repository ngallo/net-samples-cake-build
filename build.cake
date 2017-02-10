var target = Argument("target", "Default");

/********** GLOBAL VARIABLES **********/

var buildArtifacts  = Directory("./src/packages");
var solutions = GetFiles("./**/*.sln");

/********** TARGETS **********/

Task("Clean")
    .Does(() =>
{
    CleanDirectories(new DirectoryPath[] { buildArtifacts });
});

Task("Restore")
    .Does(() =>
{
    var solutions = GetFiles("./**/*.sln");

    foreach(var solution in solutions)
    {
        Information("Restoring solution:" + solution);
        NuGetRestore(solution);
    }
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var solutions = GetFiles("./**/*.sln");

    foreach(var solution in solutions)
    {
         Information("Building solution:" + solution);
        MSBuild(solution);
    }
});

Task("Output")
    .Does(() =>
{
    CreateDirectory("./output/tests");
});

Task("Tests")
    .Does(() =>
{
    NUnit3("./src/**/bin/DEbug/*.Tests.dll",
        new NUnit3Settings {
            ToolPath = GetFiles("./src/packages/NUnit.ConsoleRunner.3.6.0/tools/nunit3-console.exe").First()
            , Results = MakeAbsolute(File("./output/tests/result.xml"))
    });
    ReportGenerator("./output/tests/result.xml", "./output/tests/html", new ReportGeneratorSettings() {
      ToolPath = "./src/packages/ReportGenerator.2.5.2/Tools/reportgenerator.exe"
    });
});

Task("CI")
  .IsDependentOn("Build")
  .IsDependentOn("Output")
  .IsDependentOn("Tests")
  .Does(() => 
{
  Information("CI Build completed :)");
});


Task("Default")
  .IsDependentOn("CI");

RunTarget(target);