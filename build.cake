/********** ARGUMENTS **********/

var target = Argument("target", "Default");
var configuration = Argument<string>("configuration", "Release");

/********** TOOLS **********/

#tool "nuget:?package=NUnit.ConsoleRunner"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"

/********** GLOBAL VARIABLES **********/

var solutionFilePath = "./src/CakeBuildSample.sln";

var artifactsPackagesFolderPath = "./src/packages";
var artifactsBinFoldersPath = "./src/**/bin";
var artifactsObjFoldersPath = "./src/**/obj";

var nUnit3TestsFilesPath = "./src/**/bin/" + configuration + "/*.Tests.dll";
var nUnit3ResultFilePath = "./output/tests/result.xml";

var reportGeneratorFilter1 = "+[*]*";
var reportGeneratorFilter2 = "-[*Tests*]*"; 

var outputFolderPath = "./output";
var outputTestsFolderPath = "./output/tests";
var outputTestsResultFilePath = "./output/tests/result.xml";
var outputReportGeneratorFolderPath = "./output/tests/html";

/********** TARGETS **********/

Task("Clean")
    .Does(() =>
{
    CleanDirectories(new DirectoryPath[] { Directory(artifactsPackagesFolderPath), Directory(outputFolderPath) });
    CleanDirectories(artifactsBinFoldersPath);
    CleanDirectories(artifactsObjFoldersPath);
});

Task("Restore")
    .Does(() =>
{
    NuGetRestore(MakeAbsolute(File(solutionFilePath)));
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    MSBuild(solutionFilePath, new MSBuildSettings()
        .SetConfiguration(configuration)
        .WithProperty("Windows", "True")
        .WithProperty("TreatWarningsAsErrors", "False")
        .UseToolVersion(MSBuildToolVersion.VS2015)
        .SetVerbosity(Verbosity.Verbose)
        .SetNodeReuse(false));
});

Task("Tests")
    .Does(() =>
{
    CreateDirectory(outputTestsFolderPath);
    OpenCover(tool => {
        tool.NUnit3(nUnit3TestsFilesPath
            , new NUnit3Settings { Results = MakeAbsolute(File(nUnit3ResultFilePath)) });
      },
      new FilePath(outputTestsResultFilePath),
      new OpenCoverSettings()
        .WithFilter(reportGeneratorFilter1)
        .WithFilter(reportGeneratorFilter2)
    );
    ReportGenerator(outputTestsResultFilePath, outputReportGeneratorFolderPath);
});

Task("CI")
  .IsDependentOn("Clean")
  .IsDependentOn("Build")
  .IsDependentOn("Tests")
  .Does(() => 
{
    Information("CI Build completed");
});

Task("RC")
  .IsDependentOn("Clean")
  .IsDependentOn("Build")
  .IsDependentOn("Tests")
  .Does(() => 
{
    Information("RC Build completed");
});

Task("Default")
  .IsDependentOn("CI");

RunTarget(target);