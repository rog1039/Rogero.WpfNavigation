using System;
using _Build;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    string PackagesDirectory  => Solution.Directory / "packages";

    [Parameter("Nuget API Key to push Nuget package to nuget.org")]
    string NugetApiKey { get; set; }

    [Parameter("Myget API Key to push Nuget package to myget.org public progero feed.")]
    string MygetApiKey { get; set; }
    
    [Parameter("Perform a full nuget delete and reinstall")] readonly bool ReinstallAllPackages = false;


    string MygetPushUrl { get; set; } = "https://www.myget.org/F/progero/api/v2/package";
    string MygetSymbolPushUrl { get; set; } = "https://www.myget.org/F/progero/symbols/api/v2/package";

    string[] NugetRestoreSources = new[] { "https://www.myget.org/F/progero/api/v2/package", "https://api.nuget.org/v3/index.json" };


    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    private MyVersion GetVersionNumber()
    {
        var myVersion = new MyVersion(GitVersion);
        return myVersion;
    }

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target CleanArtifactsDirectoryOnly => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            MSBuild(s => s
                        .SetTargetPath(Solution)
                        .SetRestoreSources(NugetRestoreSources)
                        .SetTargets("Restore"));

            NuGetTasks
                .NuGetRestore(s => s
                                  .SetSource(NugetRestoreSources)
                                  .SetOutputDirectory(PackagesDirectory)
                                  .SetTargetPath(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            Logger.Warn(GitVersion);
            var myVersion = GetVersionNumber();
            TeamCity.Instance?.SetBuildNumber(myVersion.GetVersionNumber());

            MSBuild(s => s
                        .SetTargetPath(Solution)
                        //.SetTargets("Rebuild")
                        .SetConfiguration(Configuration)
                        .SetAssemblyVersion(myVersion.GetAssemblyVersion())
                        .SetFileVersion(myVersion.GetFileVersion())
                        .SetInformationalVersion(myVersion.GetAssemblyVersion())
                        .SetMaxCpuCount(Environment.ProcessorCount)
                        .SetNodeReuse(IsLocalBuild));
        });

    Target NugetPack => _ => _
        .DependsOn(CleanArtifactsDirectoryOnly)
        .DependsOn(Compile)
        .Executes(() =>
        {
            var version = GetVersionNumber();
            var wpfNavigationProject = Solution.GetProject("Rogero.WpfNavigation");

            NuGetTasks
                .NuGetPack(s => s
                               .SetVersion(version.GetAssemblyVersion())
                               .SetOutputDirectory(ArtifactsDirectory)
                               .EnableSymbols()
                               .SetProperty("Configuration", Configuration.ToString())
                               .SetSymbolPackageFormat(NuGetSymbolPackageFormat.symbols_nupkg)
                               .SetTargetPath(wpfNavigationProject.Path)
                );
        });

    Target NugetDotOrgPush => _ => _
        .DependsOn(NugetPack)
        .Executes(() =>
        {
            //
            //
            NuGetTasks.NuGetPush(s => s
                                     .SetNonInteractive(true)
                                     .SetSymbolApiKey(NugetApiKey)
                                     .DisableNoSymbols());
        });

    Target MygetPush => _ => _
        .DependsOn(NugetPack)
        .Executes(() =>
        {
            var packages = ArtifactsDirectory.GlobFiles("*.nupkg");
            //
            NuGetTasks.NuGetPush(s => s
                                     .SetNonInteractive(true)
                                     .SetSource(MygetPushUrl)
                                     .SetApiKey(MygetApiKey)
                                     .SetSymbolSource(MygetSymbolPushUrl)
                                     .SetSymbolApiKey(MygetApiKey)
                                     .DisableNoSymbols()
                                     .CombineWith(packages, (settings, path) => settings.SetTargetPath(path)));
        });

}
