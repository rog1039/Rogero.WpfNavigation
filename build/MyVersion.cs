using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JetBrains.Annotations;
using Nuke.Common.Tools.GitVersion;

namespace _Build
{
    class MyVersion
    {
        public string GetFileVersion()
        {
            return GetVersionNumber();
        }

        public string GetAssemblyVersion()
        {
            return GetVersionNumber();
        }
        public string GetVersionNumberWithBranch()
        {
            var versionString = GetVersionString();
            var branchVersionAddition = GetBranchVersionAddition();

            return $"{versionString}{branchVersionAddition}";
        }

        public string GetVersionNumber()
        {
            var version = GetVersionString();
            return version;
        }

        public string GetVersionString()
        {
            var major = GitVersion.Major;
            var minor = GitVersion.Minor;
            var build = GetBuildNumberAsDaysSinceEpoch();
            var revision = GetMinutesSinceStartOfDay();

            return $"{major}.{minor}.{build}.{revision}";
        }

        string GetMinutesSinceStartOfDay()
        {
            var minutesSinceStartOfDay = Math.Floor(VersionDate.TimeOfDay.TotalMinutes);
            return minutesSinceStartOfDay.ToString();
        }


        readonly GitVersion GitVersion;
        readonly DateTime EpochDate = new DateTime(2019, 01, 01);
        readonly DateTime VersionDate = DateTime.UtcNow;

        public MyVersion(GitVersion gitVersion)
        {
            GitVersion = gitVersion;

        }

        string GetPatchVersion()
        {
            var timestamp = DateTime.UtcNow;
            var minutesSinceLastCommit = Math.Round((timestamp - EpochDate).TotalMinutes, MidpointRounding.AwayFromZero);
            return minutesSinceLastCommit.ToString(CultureInfo.InvariantCulture);
        }

        [NotNull]
        string GetBranchVersionAddition()
        {
            var branch = GitVersion.BranchName;
            return branch == "master" ? string.Empty : $"-{branch}";
        }

        public string GetBuildNumberAsDaysSinceEpoch()
        {
            var daysSinceEpoch = Math.Floor((VersionDate - EpochDate).TotalDays);
            return daysSinceEpoch.ToString();
        }
    }
}
