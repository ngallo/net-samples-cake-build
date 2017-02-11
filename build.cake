/********** ARGUMENTS **********/

var target = Argument("target", "Default");
var configuration = Argument<string>("configuration", "Release");
var buildNumber = Argument<string>("buildNumber", "000");

var appName = "CakeBuildSample";

/********** TOOLS **********/

#tool "nuget:?package=NUnit.ConsoleRunner"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"

/********** GLOBAL VARIABLES **********/

// Parse release notes
var releaseNotes = ParseReleaseNotes("./ReleaseNotes.md");

// Get version.
var version = releaseNotes.Version.ToString();
var semVersion = version + "." + buildNumber;

// Solution
var solutionFilePath = "./src/CakeBuildSample.sln";
var solutionInfoFilePath = "./src/SolutionInfo.cs";

// Define Nuget & MSBuild directories
var nugetPackagesFolderPath = "./src/packages";
var buildBinFoldersPath = "./src/**/bin";
var buildObjFoldersPath = "./src/**/obj";
var buildFolderPath = "./src/CakeBuildSample/bin/" + configuration;

// Define nUnit3 files
var nUnit3TestsFilesPath = "./src/**/bin/" + configuration + "/*.Tests.dll";
var nUnit3ResultFilePath = "./output/tests/result.xml";

// Define ReportGenerator fiters
var reportGeneratorFilter1 = "+[*]*";
var reportGeneratorFilter2 = "-[*Tests*]*"; 

// Define output directories & files
var outputFolderPath = "./output";
var outputTestsFolderPath = "./output/tests";
var outputTestsResultFilePath = "./output/tests/result.xml";
var outputReportGeneratorFolderPath = "./output/tests/html";
var outputBuildFolderPath = "./output/artifacts/build-" + semVersion;
var outputBuildZipFilePath = "./output/artifacts/build-" + semVersion + ".zip";

/********** SETUP / TEARDOWN **********/

Setup(context =>
{
    //Executed BEFORE the first task.
    Information("Building version {0} of {1}.", semVersion, appName);
});

Teardown(context =>
{
    // Executed AFTER the last task.
    Information("Finished building version {0} of {1}.", semVersion, appName);
});

/********** PREPARE **********/

Task("Clean")
    .Does(() =>
{
    Information("Cleaning Nuget Packages");
    CleanDirectories(nugetPackagesFolderPath);

    Information("Cleaning Output directory");
    CleanDirectories(outputFolderPath);

    Information("Cleaning Bin directories");
    CleanDirectories(buildBinFoldersPath);

    Information("Cleaning Obj directories");
    CleanDirectories(buildObjFoldersPath);
});

Task("Restore")
    .Does(() =>
{
    Information("Restoring {0}", solutionFilePath);
    NuGetRestore(MakeAbsolute(File(solutionFilePath)));
});

/********** BUILD **********/

Task("Patch-Assembly-Info")
    .Does(() =>
{
    CreateAssemblyInfo(solutionInfoFilePath, new AssemblyInfoSettings
    {
        Product = appName,
        Version = version,
        FileVersion = version,
        InformationalVersion = semVersion,
        Copyright = "Copyright (c) 2017 - " + DateTime.Now.Year.ToString() + " Nicola Gallo"
    });
});

Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Patch-Assembly-Info")
    .Does(() =>
{
    Information("Building {0}", solutionFilePath);
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

/********** PACKAGE **********/

Task("Create-Artifacts")
  .Does(() => 
    {
    Information("Creating artifacts folder");
    CreateDirectory(outputBuildFolderPath);

    Information("Copying artifacts");
    CopyFiles(buildFolderPath + "/*.dll", outputBuildFolderPath);
    CopyFiles(buildFolderPath + "/*.pdb", outputBuildFolderPath);
    CopyFiles(buildFolderPath + "/*.xml", outputBuildFolderPath);
    CopyFiles(buildFolderPath + "/*.exe", outputBuildFolderPath);

    CopyFiles(new FilePath[] { "LICENSE", "README.md", "ReleaseNotes.md" }, outputBuildFolderPath);
});

Task("Zip-Artifacts")
  .Does(() => 
    {
    Information("Compressing artifacts");
    Zip(outputBuildFolderPath, outputBuildZipFilePath);
});


Task("Package")
    .IsDependentOn("Create-Artifacts")
    .IsDependentOn("Zip-Artifacts");

/********** TASK TARGETS **********/

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
  .IsDependentOn("Package")
  .Does(() => 
{
    Information("RC Build completed");
});

Task("Default")
  .IsDependentOn("CI");


/********** EXECUTION **********/

RunTarget(target);