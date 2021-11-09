﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Terraria.ModLoader.Properties;

namespace Terraria.ModLoader.Setup
{
	static class Program
	{
		public static readonly string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		public static readonly string logsDir = Path.Combine("setup", "logs");

		public static string SteamDir => Settings.Default.SteamDir;
		public static string TMLDevSteamDir => Settings.Default.TMLDevSteamDir;
		public static string TerrariaPath => Path.Combine(SteamDir, "Terraria.exe");
		public static string TerrariaServerPath => Path.Combine(SteamDir, "TerrariaServer.exe");

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

#if AUTO
			Settings.Default.SteamDir = Path.GetFullPath(args[0]);
			Settings.Default.TMLDevSteamDir = Path.GetFullPath("steam_build");
			if (!Directory.Exists(TMLDevSteamDir))
				Directory.CreateDirectory(TMLDevSteamDir);
#else
			CreateTMLSteamDirIfNecessary();
#endif
			UpdateTargetsFiles();
#if AUTO
			Console.WriteLine("Automatic setup start");
			new AutoSetup().DoAuto();
			Console.WriteLine("Automatic setup finished");
#else
			Application.Run(new MainForm());
#endif
		}

		public static int RunCmd(string dir, string cmd, string args, 
				Action<string> output = null, 
				Action<string> error = null,
				string input = null,
				CancellationToken cancel = default(CancellationToken)) {

			using (var process = new Process()) {
				process.StartInfo = new ProcessStartInfo {
					FileName = cmd,
					Arguments = args,
					WorkingDirectory = dir,
					UseShellExecute = false,
					RedirectStandardInput = input != null,
					CreateNoWindow = true
				};

				if (output != null) {
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
				}

				if (error != null) {
					process.StartInfo.RedirectStandardError = true;
					process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
				}

				if (!process.Start())
					throw new Exception($"Failed to start process: \"{cmd} {args}\"");

				if (input != null) {
					var w = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false));
					w.Write(input);
					w.Close();
				}

				while (!process.HasExited) {
					if (cancel.IsCancellationRequested) {
						process.Kill();
						throw new OperationCanceledException(cancel);
					}
					process.WaitForExit(100);

					output?.Invoke(process.StandardOutput.ReadToEnd());
					error?.Invoke(process.StandardError.ReadToEnd());
				}

				return process.ExitCode;
			}
		}

		 public static bool SelectTerrariaDialog() {
			while (true) {
				var dialog = new OpenFileDialog {
					InitialDirectory = Path.GetFullPath(Directory.Exists(SteamDir) ? SteamDir : "."),
					Filter = "Terraria|Terraria.exe",
					Title = "Select Terraria.exe"
				};

				if (dialog.ShowDialog() != DialogResult.OK)
					return false;

				string err = null;
				if (Path.GetFileName(dialog.FileName) != "Terraria.exe")
					err = "File must be named Terraria.exe";
				else if (!File.Exists(Path.Combine(Path.GetDirectoryName(dialog.FileName), "TerrariaServer.exe")))
					err = "TerrariaServer.exe does not exist in the same directory";

				if (err != null) {
					if (MessageBox.Show(err, "Invalid Selection", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
						return false;
				}
				else {
					Settings.Default.SteamDir = Path.GetDirectoryName(dialog.FileName);
					Settings.Default.TMLDevSteamDir = "";
					Settings.Default.Save();

					CreateTMLSteamDirIfNecessary();
					UpdateTargetsFiles();

					return true;
				}
			}
		}

		public static bool SelectTmlDirectoryDialog() {
			while (true) {
				var dialog = new OpenFileDialog {
					InitialDirectory = Path.GetFullPath(Directory.Exists(SteamDir) ? SteamDir : "."),
					ValidateNames = false,
					CheckFileExists = false,
					CheckPathExists = true,
					FileName = "Folder Selection.",
				};

				if (dialog.ShowDialog() != DialogResult.OK)
					return false;

				Settings.Default.TMLDevSteamDir = Path.GetDirectoryName(dialog.FileName);
				Settings.Default.Save();

				UpdateTargetsFiles();
				
				return true;
			}
		}

		private static void CreateTMLSteamDirIfNecessary() {
			if (Directory.Exists(TMLDevSteamDir))
				return;
			
			Settings.Default.TMLDevSteamDir = Path.GetFullPath(Path.Combine(Settings.Default.SteamDir, "..", "tModLoaderDev"));
			Settings.Default.Save();

			try {
				Directory.CreateDirectory(TMLDevSteamDir);
			}
			catch (Exception e) {
				Console.WriteLine($"{e.GetType().Name}: {e.Message}");
			}
		}

		internal static void UpdateTargetsFiles() {
			UpdateFileText("src/WorkspaceInfo.targets", GetWorkspaceInfoTargetsText());
			UpdateFileText(Path.Combine(TMLDevSteamDir, "tMLMod.targets"), File.ReadAllText("patches/tModLoader/Terraria/release_extras/tMLMod.targets"));
		}

		private static void UpdateFileText(string path, string text) {
			SetupOperation.CreateParentDirectory(path);

			if (!File.Exists(path) || text != File.ReadAllText(path))
				File.WriteAllText(path, text);
		}

		private static string GetWorkspaceInfoTargetsText() {
			string gitsha = "";
			RunCmd("", "git", "rev-parse HEAD", s => gitsha = s.Trim());

			string branch = "";
			RunCmd("", "git", "rev-parse --abbrev-ref HEAD", s => branch = s.Trim());

			string GITHUB_HEAD_REF = Environment.GetEnvironmentVariable("GITHUB_HEAD_REF");
			if (!string.IsNullOrWhiteSpace(GITHUB_HEAD_REF)) {
				Console.WriteLine($"GITHUB_HEAD_REF found: {GITHUB_HEAD_REF}");
				branch = GITHUB_HEAD_REF;
			}
			string HEAD_SHA = Environment.GetEnvironmentVariable("HEAD_SHA");
			if (!string.IsNullOrWhiteSpace(HEAD_SHA)) {
				Console.WriteLine($"HEAD_SHA found: {HEAD_SHA}");
				gitsha = HEAD_SHA;
			}

			return
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <!-- This file will always be overwritten, do not edit it manually. -->
  <PropertyGroup>
	<BranchName>{branch}</BranchName>
	<CommitSHA>{gitsha}</CommitSHA>
	<TerrariaSteamPath>{SteamDir}</TerrariaSteamPath>
    <tModLoaderSteamPath>{TMLDevSteamDir}</tModLoaderSteamPath>
  </PropertyGroup>
</Project>";
		}
	}
}
