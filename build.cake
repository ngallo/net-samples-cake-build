/********** ARGUMENTS **********/

Func<CakeYml> cakeYml = () => { return DeserializeYamlFromFile<CakeYml>("./cake.yml"); };

var target = Argument("target", "Default");

var isAPPVEYOR = (EnvironmentVariable("APPVEYOR") ?? "").ToUpper() == "TRUE";

var buildNumber = Argument<string>("build_number", null);
buildNumber = string.IsNullOrWhiteSpace(buildNumber) == false ? buildNumber : isAPPVEYOR ? EnvironmentVariable("APPVEYOR_BUILD_NUMBER") : cakeYml().LocalBuildNumber;
var buildVersion = isAPPVEYOR ? EnvironmentVariable("APPVEYOR_BUILD_VERSION") : ParseReleaseNotes("./ReleaseNotes.md").Version.ToString() + "." + buildNumber;
var buildConfiguration = Argument<string>("buildConfiguration", "Release");

var artifactsEnableSecurity = true;
var artifactsEnableCompression = true;
var artifactsFolderPath = Argument<string>("artifacts_folder", "./output/artifacts");

var certTimeStampUri = Argument<string>("cert_timestamp_uri", isAPPVEYOR ? EnvironmentVariable("APPVEYOR_BUILD_CERT_TIMESTAMP_URI") :  "http://timestamp.digicert.com");
var certPath = Argument<string>("cert_path", isAPPVEYOR ? EnvironmentVariable("APPVEYOR_BUILD_CERT_PATH") : "./certificates/signing/Dev_TemporaryKey.pfx");
//Note: Pa$$w0rd is provided in clear text only for testing purpose. In a real project you must encrypt it.
var certPassword = Argument<string>("cert_password", isAPPVEYOR ? EnvironmentVariable("APPVEYOR_BUILD_PASSWORD") ?? "pa$$w0rd" : "pa$$w0rd");

/********** TOOLS & ADDINS **********/

#addin "Cake.FileHelpers"

#addin "Cake.Yaml"

#addin "Cake.Powershell"

#tool "nuget:?package=NUnit.ConsoleRunner"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"

#addin nuget:?package=Cake.Paket
#tool nuget:?package=Paket

/********** TYPES **********/

public class CakeYmlResource {
    public string Name { get;set; }
    public string Path { get;set; }
}

public class CakeYml {
    public string Name { get;set; }
    public string LocalBuildNumber { get; set; }
    public List<CakeYmlResource> Artifacts { get;set; }
    public List<CakeYmlResource> SharedLibs { get;set; }
}

/********** GLOBAL VARIABLES **********/

// Solution
var solutionFilePath = "./"+ cakeYml().Name +".sln";
var solutionSharedFolderPath = "./shared/";
var solutionInfoFilePath = "./shared/SolutionInfo.cs";

// Define Nuget & MSBuild directories
var nugetPackagesFolderPath = "./packages";
var buildBinFoldersPath = "./src/**/bin";
var buildObjFoldersPath = "./src/**/obj";
var buildArtifactFolderPathTemplate = "./src/{0}/bin/" + buildConfiguration;

// Define nUnit3 files
var nUnit3TestsFilesPath = "./test/**/bin/" + buildConfiguration + "/*.Test.dll";
var nUnit3ResultFilePath = "./output/test/result.xml";

// Define ReportGenerator fiters
var reportGeneratorFilter1 = "+[*]*";
var reportGeneratorFilter2 = "-[*Test*]*"; 

// Define output directories & files
var outputFolderPath = "./output";
var outputTestsFolderPath = "./output/test";
var outputTestsResultFilePath = "./output/test/result.xml";
var outputReportGeneratorFolderPath = "./output/test/html";
var outputBuildArtifactFolderPathTemplate = "{0}/{1}";
var outputBuildArtifactZipFilePathTemplate ="{0}/{1}" + ".zip";

/********** SETUP / TEARDOWN **********/

Setup(context =>
{
    //Executed BEFORE the first task.
    Information("Building build version {0} of {1}.", buildVersion, cakeYml().Name);
});

Teardown(context =>
{
    // Executed AFTER the last task.
    Information("Finished building version {0} of {1}.", buildVersion, cakeYml().Name);
});

/********** PREPARE **********/

Task("Clean")
    .Does(() =>
    {
        if (DirectoryExists(nugetPackagesFolderPath) == true) 
        {
            Information("Cleaning Nuget Packages {0}", nugetPackagesFolderPath);
            CleanDirectories(nugetPackagesFolderPath);
            DeleteDirectory(nugetPackagesFolderPath, true); 
        }
        
        if (DirectoryExists(artifactsFolderPath) == true) 
        {
            Information("Cleaning Artifacts directory {0}", artifactsFolderPath);
            CleanDirectories(artifactsFolderPath);
            DeleteDirectory(artifactsFolderPath, true); 
        }

        if (DirectoryExists(outputFolderPath) == true) 
        {
            Information("Cleaning Output directory {0}", outputFolderPath);
            CleanDirectories(outputFolderPath);
            DeleteDirectory(outputFolderPath, true); 
        }

        Information("Cleaning Bin directories {0}", buildBinFoldersPath);
        CleanDirectories(buildBinFoldersPath);

        Information("Cleaning Obj directories {0}", buildObjFoldersPath);
        CleanDirectories(buildObjFoldersPath);
    });

Task("Restore")
    .Does(() =>
    {
        Information("Paket is restoring {0}", solutionFilePath);
        PaketPack(nugetPackagesFolderPath);
        PaketRestore();
    });

/********** BUILD **********/

Task("Patch-Assembly-Info")
    .Does(() =>
    {
        CreateDirectory(solutionSharedFolderPath);
        CreateAssemblyInfo(solutionInfoFilePath, new AssemblyInfoSettings
        {
            Product = cakeYml().Name,
            Version = buildVersion,
            FileVersion = buildVersion,
            InformationalVersion = buildVersion,
            Copyright = "Copyright (c) 2017 - " + DateTime.UtcNow.Year.ToString() + " " + cakeYml().Name
        });
    });

Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Patch-Assembly-Info")
    .Does(() =>
    {
        Information("Building {0}", solutionFilePath);
        MSBuild(solutionFilePath, new MSBuildSettings()
            .SetConfiguration(buildConfiguration)
            .WithProperty("Windows", "True")
            .WithProperty("TreatWarningsAsErrors", "False")
            .UseToolVersion(MSBuildToolVersion.VS2015)
            .SetVerbosity(Verbosity.Verbose)
            .SetNodeReuse(false));
    });

Task("Test")
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
        foreach (var artifact in cakeYml().Artifacts)
        {
            var buildArtifactFolderPath = string.Format(buildArtifactFolderPathTemplate, artifact.Path);
            var outputBuildArtifactFolderPath = string.Format(outputBuildArtifactFolderPathTemplate, artifactsFolderPath, artifact.Name);

            Information("Creating {0} artifact folder", artifact.Name);
            CreateDirectory(outputBuildArtifactFolderPath);

            Information("Copying {0} artifact", artifact.Name);
            CopyFiles(buildArtifactFolderPath + "/*.dll", outputBuildArtifactFolderPath);
            CopyFiles(buildArtifactFolderPath + "/*.pdb", outputBuildArtifactFolderPath);
            CopyFiles(buildArtifactFolderPath + "/*.xml", outputBuildArtifactFolderPath);
            CopyFiles(buildArtifactFolderPath + "/*.exe", outputBuildArtifactFolderPath);

            CopyFiles(new FilePath[] { "ReleaseNotes.md" }, outputBuildArtifactFolderPath);

            if (isAPPVEYOR == true) {
                var buildInfoBuilder = new StringBuilder();
                buildInfoBuilder.AppendFormat("Git Commit: {0}", EnvironmentVariable("APPVEYOR_REPO_COMMIT"));
                buildInfoBuilder.AppendLine();
                buildInfoBuilder.AppendFormat("Git Commit Message: {0}", EnvironmentVariable("APPVEYOR_REPO_COMMIT_MESSAGE"));
                buildInfoBuilder.AppendLine();
                buildInfoBuilder.AppendFormat("Git Commit TimeStamp: {0}", EnvironmentVariable("APPVEYOR_REPO_COMMIT_TIMESTAMP"));
                buildInfoBuilder.AppendLine();
                buildInfoBuilder.AppendFormat("Build Version: {0}", buildVersion);
                buildInfoBuilder.AppendLine();
                FileWriteText(outputBuildArtifactFolderPath + "/artifact.info", buildInfoBuilder.ToString());
            }
        }
    });

Task("Signing-Artifacts")
    .Does(() => 
    {
        if (artifactsEnableSecurity == false) 
        {
            return;
        }
        foreach (var artifact in cakeYml().Artifacts)
        {
            var outputBuildArtifactFolderPath = string.Format(outputBuildArtifactFolderPathTemplate, artifactsFolderPath, artifact.Name);

            var dllFiles = GetFiles(outputBuildArtifactFolderPath + "/**/*.dll");
            Sign(dllFiles, new SignToolSignSettings {
                    TimeStampUri = new Uri(certTimeStampUri),
                    CertPath = certPath,
                    Password = certPassword
            });
            var exeFiles = GetFiles(outputBuildArtifactFolderPath + "/**/*.exe");
            Sign(exeFiles, new SignToolSignSettings {
                    TimeStampUri = new Uri(certTimeStampUri),
                    CertPath = certPath,
                    Password = certPassword
            });      
        }
    });

Task("Zip-Artifacts")
    .Does(() => 
    {
        if (artifactsEnableCompression == false) 
        {
            return;
        }
        foreach (var artifact in cakeYml().Artifacts)
        {
            var outputBuildArtifactFolderPath = string.Format(outputBuildArtifactFolderPathTemplate, artifactsFolderPath, artifact.Name);
            var outputBuildArtifactZipFilePath = string.Format(outputBuildArtifactZipFilePathTemplate, artifactsFolderPath, artifact.Name);

            Information("Compressing {0} artifact", artifact.Name);
            Zip(outputBuildArtifactFolderPath, outputBuildArtifactZipFilePath);

            if (artifactsEnableSecurity == true) {
                Information("Creating SHA256 hash for {0} artifact", outputBuildArtifactZipFilePath);
                var zipFileHashPath = MakeAbsolute(File(outputBuildArtifactZipFilePath + ".hash"));
                FileWriteText(zipFileHashPath, CalculateFileHash(outputBuildArtifactZipFilePath).ToHex());      
            }
        }
    });

Task("Package")
    .IsDependentOn("Create-Artifacts")
    .IsDependentOn("Signing-Artifacts")
    .IsDependentOn("Zip-Artifacts");

/********** LIB TARGETS **********/

Task("Lib")
    .Does(() => 
    {
        return;
        artifactsEnableSecurity = false;
        artifactsEnableCompression = false;
        var path = MakeAbsolute(Directory(artifactsFolderPath)).ToString();
        Information("Creating {0} artifact folder", path);
        CreateDirectory(path);
        RunTarget("RC");
        Information("Lib target completed");
    });

/********** GIT SUBTREE TARGETS **********/

Task("Prepare-SubTree")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Patch-Assembly-Info")
    .Does(() => 
    {
        Information("Prepare-SubTree target completed");
    });

Task("Init-SubTree")
    .Does(() => 
    {
        if ((isAPPVEYOR == true) || (cakeYml().SharedLibs == null))
        {
            return;
        }
        foreach (var sharedLib in cakeYml().SharedLibs) 
        {
            var path = MakeAbsolute(File(sharedLib.Path + "/build.ps1"));
            Information("Initializing " + sharedLib.Name + " with script: " + path);
            StartPowershellFile(path, new PowershellSettings()
                .WithArguments(args => { args.Append("target", "Prepare-SubTree").Append("buildnumber", buildNumber); }));
        }
        Information("Init-SubTree target completed");
    });

/********** TASK TARGETS **********/

Task("CI")
    .IsDependentOn("Init-SubTree")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .Does(() => 
    {
        Information("CI target completed");
    });

Task("RC")
    .IsDependentOn("Init-SubTree")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package")
    .Does(() => 
    {
        Information("RC target completed");
    });

Task("Default")
    .IsDependentOn("CI");

/********** EXECUTION **********/

RunTarget(target);