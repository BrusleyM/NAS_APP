#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace NAS.EditorTools
{
    /// <summary>
    /// Unity's PlayerSettings.iOS.appleDeveloperTeamID / appleEnableAutomaticSigning
    /// don't reliably propagate to every target in the generated Xcode project -
    /// only the main app target reliably gets them. The UnityFramework (and any
    /// Tests) target is left without a team, which is why Xcode shows
    /// "No Account for Team" until you manually reselect the team in Signing &
    /// Capabilities. This runs right after Unity exports the Xcode project and
    /// forces DEVELOPMENT_TEAM + automatic signing onto every target, so a fresh
    /// build never needs that manual click.
    /// </summary>
    public static class IOSSigningPostProcessor
    {
        [PostProcessBuild(1)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget != BuildTarget.iOS)
                return;

            var teamId = PlayerSettings.iOS.appleDeveloperTeamID;
            if (string.IsNullOrEmpty(teamId))
                return;

            var projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            // Modern Unity (2019.3+) generates exactly these two targets in a
            // "unified" Xcode project - the app itself and the framework it
            // embeds. There's no generic "all targets" accessor in this
            // Unity version's PBXProject API, so both are named explicitly.
            ApplyTeam(project, project.GetUnityMainTargetGuid(), teamId);
            ApplyTeam(project, project.GetUnityFrameworkTargetGuid(), teamId);

            project.WriteToFile(projectPath);
        }

        private static void ApplyTeam(PBXProject project, string targetGuid, string teamId)
        {
            if (string.IsNullOrEmpty(targetGuid))
                return;

            project.SetBuildProperty(targetGuid, "DEVELOPMENT_TEAM", teamId);
            project.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Automatic");
        }
    }
}
#endif
