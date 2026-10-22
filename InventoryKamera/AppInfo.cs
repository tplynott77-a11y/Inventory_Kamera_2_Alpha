using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace InventoryKamera
{
    /// <summary>
    /// Single source of the human-facing application version string, used for the window title and
    /// every log that records the running version. The numeric core comes from
    /// <see cref="AssemblyVersion"/> (kept numeric because the auto-update check compares it against
    /// GitHub release tags); the prerelease stage is the one knob below.
    /// </summary>
    internal static class AppInfo
    {
        /// <summary>
        /// SemVer prerelease suffix for preview builds, e.g. "-alpha", "-alpha.2", "-preview.1".
        /// Set to "" for a stable release. This is the single line to edit when moving a preview along.
        /// </summary>
        private const string PreReleaseTag = "-alpha";

        /// <summary>e.g. "2.0.0-alpha" for a preview, "2.0.0" once <see cref="PreReleaseTag"/> is "".</summary>
        public static string Version =>
            Assembly.GetExecutingAssembly().GetName().Version.ToString(3) + PreReleaseTag;

        /// <summary>
        /// GitHub tags may contain SemVer suffixes (for example "v2.0.0-alpha.2").
        /// System.Version accepts only the numeric core; stripping letters leaves
        /// an invalid trailing hyphen such as "2.0.0-".
        /// </summary>
        internal static bool TryParseReleaseTagVersion(string tag, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            Match match = Regex.Match(tag.Trim(),
                @"^[vV]?(?<numeric>\d+\.\d+(?:\.\d+){0,2})(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$");

            return match.Success && Version.TryParse(match.Groups["numeric"].Value, out version);
        }
    }
}
