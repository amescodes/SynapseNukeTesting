using System;
using System.IO;
using System.Linq;
using NuGet.Versioning;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;

[GitHubActions(
    name:"cicd-nuget",
    GitHubActionsImage.WindowsLatest,
    On = new [] { GitHubActionsTrigger.Push },
    OnPushBranches = new []{"main","develop"},
    InvokedTargets = new []{nameof(Pack)},
    ImportSecrets = new []{nameof(SYNAPSE_NUGET_API_KEY)},
    EnableGitHubToken = true,
    PublishArtifacts = true)
]
class Build : NukeBuild
{
    [Solution(GenerateProjects = true)]
    readonly Solution Solution;

    [Secret] [Parameter] readonly string SYNAPSE_NUGET_API_KEY;

    [GitVersion(UpdateAssemblyInfo = true, UpdateBuildNumber = true)]
    readonly GitVersion GitVersion;

    string Version => GitVersion.NuGetVersionV2;

    //[GitRepository] 
    //readonly GitRepository GitRepository;

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    string OutputDirectory => RootDirectory / "output";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .Executes(() =>
        {
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            Project synapseClient = Solution.Synapse_Client;
            string clientOutputPath = Path.Combine(OutputDirectory, "client");
            DotNetBuild(_ => _
                .SetProjectFile(synapseClient)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(clientOutputPath));


            Project synapseServer = Solution.Synapse_Revit;
            string serverOutputPath = Path.Combine(OutputDirectory, "server");
            DotNetBuild(_ => _
                .SetProjectFile(synapseServer)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(serverOutputPath));
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Project synapseClient = Solution.Synapse_Client;
            DotNetPack(s => s
                .SetProject(synapseClient)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableNoRestore()
                .SetAuthors("ames codes")
                .SetPackageProjectUrl("https://github.com/amescodes/Synapse")
                .SetDescription("Client package for Revit Synapse library.")
                .SetPackageTags("revit grpc client server communication")
                .SetNoDependencies(true)
                .SetVersion(Version)
                .SetOutputDirectory(Path.Combine(OutputDirectory, "nuget")));

            Project synapseServer = Solution.Synapse_Revit;
            DotNetPack(s => s
                .SetProject(synapseServer)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableNoRestore()
                .SetAuthors("ames codes")
                .SetPackageProjectUrl("https://github.com/amescodes/Synapse")
                .SetDescription("Server package for Revit Synapse library. This package should be loaded into the Revit addin.")
                .SetPackageTags("revit grpc client server communication")
                .SetNoDependencies(true)
                .SetVersion(Version)
                .SetOutputDirectory(Path.Combine(OutputDirectory, "nuget")));
        });
}
