using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NAS.EditorTools
{
    /// <summary>
    /// Build > Develop / Feature / Release / iOS Build & Run. All four go
    /// through the same pipeline: bump PlayerSettings.bundleVersion (Vx.y.z)
    /// plus the target platform's build number/code, then run a real
    /// BuildPipeline.BuildPlayer (Android or iOS - this project doesn't ship
    /// anywhere else). The version/build bump always happens, regardless of
    /// whether the build was triggered here in the Editor or from a CI/CD
    /// pipeline calling the same tier methods headlessly.
    ///
    /// The one real difference between the two: an in-Editor trigger can also
    /// open Xcode and run the build on a device/simulator automatically
    /// (AutoRunPlayer) - "iOS Build & Run" is exactly "Develop" for iOS with
    /// that flag added. A CI/CD run just builds and stops there, since it has
    /// nowhere to launch the result.
    ///
    /// Version rule:
    ///   Develop  -> z += 1
    ///   Feature  -> y += 1, z reset to 0
    ///   Release  -> x += 1, y and z reset to 0
    ///
    /// Android's bundleVersionCode and iOS's buildNumber increment by 1 on
    /// every build, independent of the Vx.y.z bump above - they're a separate,
    /// monotonically increasing counter each store requires. Because the
    /// version/build number always changes, every build gets its own fresh
    /// output folder - never overwrites a previous one in place.
    /// </summary>
    public static class BuildTools
    {
        private enum BuildTier { Develop, Feature, Release }

        [MenuItem("Build/Develop")]
        private static void BuildDevelop() => RunBuild(BuildTier.Develop, EditorUserBuildSettings.activeBuildTarget, autoRun: false);

        [MenuItem("Build/Feature")]
        private static void BuildFeature() => RunBuild(BuildTier.Feature, EditorUserBuildSettings.activeBuildTarget, autoRun: false);

        [MenuItem("Build/Release")]
        private static void BuildRelease() => RunBuild(BuildTier.Release, EditorUserBuildSettings.activeBuildTarget, autoRun: false);

        // Always targets iOS regardless of whatever the active build target
        // currently is - a dedicated fast-iteration trigger, separate from
        // whichever platform Build Settings is currently pointed at.
        [MenuItem("Build/iOS Build && Run", priority = 100)]
        private static void BuildAndRunIOS() => RunBuild(BuildTier.Develop, BuildTarget.iOS, autoRun: true);

        private static void RunBuild(BuildTier tier, BuildTarget target, bool autoRun)
        {
            if (target != BuildTarget.Android && target != BuildTarget.iOS)
            {
                EditorUtility.DisplayDialog("Build",
                    $"Active build target is {target}, but this project only builds for Android or iOS. Switch platform via File > Build Settings first.",
                    "OK");
                return;
            }

            var newVersion = PeekNextVersion(PlayerSettings.bundleVersion, tier);
            var newBuildNumber = PeekNextPlatformBuildNumber(target);

            var action = autoRun ? "build and run" : "build";
            var proceed = EditorUtility.DisplayDialog("Build",
                $"About to {action} {tier} v{newVersion} (build {newBuildNumber}) for {target}.\n\n" +
                "This saves open scenes, updates Player Settings, and runs a full player build.",
                "Build", "Cancel");
            if (!proceed)
                return;

            ExecuteBuild(tier, target, newVersion, newBuildNumber, autoRun);
        }

        // Split out from RunBuild so the actual build (version bump + BuildPlayer)
        // can be invoked without the confirmation dialog - e.g. from an automated
        // CI/CD entry point calling this directly with autoRun: false. The menu
        // items above always go through RunBuild first, so the interactive
        // confirmation is never skipped for a real click.
        private static void ExecuteBuild(BuildTier tier, BuildTarget target, string newVersion, int newBuildNumber, bool autoRun)
        {
            PlayerSettings.bundleVersion = newVersion;
            ApplyPlatformBuildNumber(target, newBuildNumber);

            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            var outputPath = GetOutputPath(target, tier, newVersion, newBuildNumber);
            var buildOptions = tier == BuildTier.Develop
                ? BuildOptions.Development | BuildOptions.AllowDebugging
                : BuildOptions.None;
            if (autoRun)
                buildOptions |= BuildOptions.AutoRunPlayer;

            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenePaths(),
                locationPathName = outputPath,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = buildOptions
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {tier} v{newVersion} (build {newBuildNumber}) for {target} -> {outputPath}");
                if (!autoRun)
                    EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                Debug.LogError($"Build {summary.result}: {tier} v{newVersion} (build {newBuildNumber}) for {target}. See Console for errors.");
                EditorUtility.DisplayDialog("Build failed", $"Build {summary.result}. See the Console for details.", "OK");
            }
        }

        private static string PeekNextVersion(string current, BuildTier tier)
        {
            var parts = (current ?? "0.0.0").Split('.');
            var x = parts.Length > 0 && int.TryParse(parts[0], out var xv) ? xv : 0;
            var y = parts.Length > 1 && int.TryParse(parts[1], out var yv) ? yv : 0;
            var z = parts.Length > 2 && int.TryParse(parts[2], out var zv) ? zv : 0;

            switch (tier)
            {
                case BuildTier.Develop:
                    z += 1;
                    break;
                case BuildTier.Feature:
                    y += 1;
                    z = 0;
                    break;
                case BuildTier.Release:
                    x += 1;
                    y = 0;
                    z = 0;
                    break;
            }

            return $"{x}.{y}.{z}";
        }

        private static int PeekNextPlatformBuildNumber(BuildTarget target)
        {
            if (target == BuildTarget.Android)
                return PlayerSettings.Android.bundleVersionCode + 1;

            // iOS's buildNumber is a free-form string in PlayerSettings, but this
            // project always keeps it numeric so it can increment the same way
            // Android's bundleVersionCode does.
            int.TryParse(PlayerSettings.iOS.buildNumber, out var current);
            return current + 1;
        }

        private static void ApplyPlatformBuildNumber(BuildTarget target, int nextBuildNumber)
        {
            if (target == BuildTarget.Android)
                PlayerSettings.Android.bundleVersionCode = nextBuildNumber;
            else
                PlayerSettings.iOS.buildNumber = nextBuildNumber.ToString();
        }

        private static string[] GetEnabledScenePaths()
        {
            var paths = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled)
                    paths.Add(scene.path);
            return paths.ToArray();
        }

        private static string GetOutputPath(BuildTarget target, BuildTier tier, string version, int buildNumber)
        {
            var productName = string.IsNullOrEmpty(PlayerSettings.productName) ? "App" : PlayerSettings.productName;
            var folder = Path.Combine("Builds", target.ToString(), tier.ToString(), $"v{version} ({buildNumber})");
            Directory.CreateDirectory(folder);

            // Android builds to a single .apk file; iOS builds to an Xcode
            // project directory, so the folder itself is the output path.
            return target == BuildTarget.Android
                ? Path.Combine(folder, $"{productName}.apk")
                : folder;
        }
    }
}
