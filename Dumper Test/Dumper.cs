using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Management;

namespace DumperN
{
	public class Dumper
	{
		public string baseLocation { get; set; } = tempPath;

		public string dumpsFolder
		{
			get
			{
				return Path.Combine(baseLocation, "dumps");
			}
		}

		public string customDumpsFolder { get; set; } = null;

		/// <summary>
		/// for the svchosts you must digit the pid
		/// </summary>
		public void addException(string process)
		{
			exclusionList.Add(process.ToUpper());
		}

		public void removeException(string process)
		{
			exclusionList.Remove(process.ToUpper());
		}

		/// <summary>
		/// you can use this property to get the result files directory (C:\...\result.txt) after you have used the customLoad() method.
		/// </summary>
		public string[] resultFiles
        {
            get
            {
				List<string> files = new List<string>();

                List<string>.Enumerator enumerator = resultFileList.GetEnumerator();
				while (enumerator.MoveNext())
                {
					files.Add(enumerator.Current);
                }
				return files.ToArray();
            }
        }

		/// <summary>
		/// you can use this property if you want to dump only few processes using customLoad(). If you want to add a svchost process you must digit the pid. Examples: addProcess("explorer"); addProcess("8192");.
		/// </summary>
		public void addProcess(string process)
		{
			processesList.Add(process.ToUpper());
		}

		public void removeProcess(string process)
		{
			processesList.Remove(process.ToUpper());
		}

		public string filePrefix
		{
			set
			{
				resultFilePrefix = value;
			}
		}

		/// <summary>
		/// this is a message which explains some doubt the user can have.
		/// </summary>
		public string warningMessage
		{
			get
			{
				return "If you can't find a process memory dump it's because the process wasn't found and if the process memory dump txt is empty it's because the memory dump was failed because of the application or user permissions.";

			}
		}

		public long ElapsedMilliseconds = 0;


		/// <summary>
		/// you can use this method if you want to dump only few processes. You can add a process using addProcess property as example: addProcess("myProcess");.
		/// </summary>
		public void customLoad()
		{
			clearFoldersBeforeStart();

			Stopwatch sw = new Stopwatch();
			sw.Start();


			extractResource();
			runCommand($"tasklist /svc | find \"svchost.exe\" > {Path.Combine(assetsPath, "svchost.log")}");
			Thread.Sleep(500);
			customGetSvchost();
			dumpSvchost();
			customDumpProcesses();
			Thread.Sleep(1000);
			Process[] processesByName = Process.GetProcessesByName("s2");
			int num = processesByName.Length;
			foreach (Process process in processesByName)
			{
				while (num != 0)
				{
					if (!getParent(process.Id).Equals(Process.GetCurrentProcess().ProcessName))
					{
						num--;
					}
				}
			}



			sw.Stop();
			ElapsedMilliseconds = sw.ElapsedMilliseconds;
		}

		public void load()
		{
			clearFoldersBeforeStart();

			Stopwatch sw = new Stopwatch();
			sw.Start();


			extractResource();
			runCommand($"tasklist /svc | find \"svchost.exe\" > {Path.Combine(assetsPath, "svchost.log")}");
			Thread.Sleep(500);
			getSvchost();
			dumpSvchost();
			dumpProcesses();
			Thread.Sleep(1000);
			Process[] processesByName = Process.GetProcessesByName("s2");
			int num = processesByName.Length;
			foreach (Process process in processesByName)
			{
				while (num != 0)
				{
					if (!getParent(process.Id).Equals(Process.GetCurrentProcess().ProcessName))
					{
						num--;
					}
				}
			}



			sw.Stop();
			ElapsedMilliseconds = sw.ElapsedMilliseconds;
		}



		void clearFoldersBeforeStart()
		{
			try
			{
				Directory.Delete(dumpsFolder, true);
				Directory.Delete(assetsPath, true);
			}
			catch { }
			finally
			{
				Directory.CreateDirectory(dumpsFolder);
				Directory.CreateDirectory(assetsPath);
			}
		}

		void extractResource()
		{
			byte[] s2 = Dumper_Test.Properties.Resources.s2;
			File.WriteAllBytes(Path.Combine(assetsPath, "s2.exe"), s2);
		}

		string assetsPath
		{
			get
			{
				return Path.Combine(baseLocation, "assets");
			}
		}

		static string tempPath
		{
			get
			{
				return Path.GetTempPath();
			}
		}

		string resultFilePrefix = "";
		string additionalCommands = "";
		Random r = new Random();

		void dumpProcesses()
		{
			string s2Dir = Path.Combine(assetsPath, "s2.exe");

			Process[] processes = Process.GetProcesses();
			for (int i = 0; i < processes.Length; i++)
			{
				Process p = processes[i];
				new Thread(delegate ()
				{
					try
					{
						if (!exclusionList.Contains(p.ProcessName.ToUpper()))
						{
							runCommand($"{s2Dir} -pid {p.Id} -l 4 -nh {additionalCommands} > {dumpsFolder}\\{p.ProcessName}_{r.Next(1000, 999999)}.txt");
						}
					}
					catch { }
				}).Start();
			}
		}

		void customDumpProcesses()
		{
			string s2Dir = Path.Combine(assetsPath, "s2.exe");

			Process[] processes = Process.GetProcesses();
			for (int i = 0; i < processes.Length; i++)
			{
				Process p = processes[i];
				new Thread(delegate ()
				{
					try
					{
						if (processesList.Contains(p.ProcessName.ToUpper()))
						{
							string txtFileName = $"{p.ProcessName}_{r.Next(1000, 999999)}.txt";
							runCommand($"{s2Dir} -pid {p.Id} -l 4 -nh {additionalCommands} > {(customDumpsFolder is null ? dumpsFolder : customDumpsFolder)}\\{txtFileName}");
							resultFileList.Add(Path.Combine(dumpsFolder, txtFileName));
						}
					}
					catch { }
				}).Start();
			}
		}

		void dumpSvchost()
		{
			string s2Dir = Path.Combine(assetsPath, "s2.exe");

			using (List<int>.Enumerator enumerator = pids.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					int pid = enumerator.Current;
					new Thread(delegate ()
					{
						try
						{
							if (!exclusionList.Contains(Convert.ToString(pid)))
							{
								string txtFileName = $"svchostPid-{pid}_{r.Next(1000, 999999)}.txt";
								runCommand($"{s2Dir} -pid {pid} > {dumpsFolder}\\{txtFileName}");
								resultFileList.Add(Path.Combine(dumpsFolder, txtFileName));
							}
						}
						catch { }
					}).Start();
				}
			}
		}


		void getSvchost()
		{
			string logsPath = Path.Combine(assetsPath, "svchost.log");
			string[] logs = File.ReadAllLines(logsPath);

			foreach (string log in logs)
			{
				pids.Add(getPID(log));
			}
		}

		void customGetSvchost()
		{
			string logsPath = Path.Combine(assetsPath, "svchost.log");
			string[] logs = File.ReadAllLines(logsPath);

			foreach (string log in logs)
			{
				int pid = getPID(log);
				if (processesList.Contains(pid.ToString()))
				{
					pids.Add(pid);
				}
			}
		}

		List<int> pids = new List<int>();
		List<string> exclusionList = new List<string>();
		List<string> processesList = new List<string>();
		List<string> resultFileList = new List<string>();

		Regex regexPid = new Regex(@"[0-9]+");
		int getPID(string pName)
		{
			int pid = Convert.ToInt32(regexPid.Match(NormalizeString(pName)).Value);

			return pid;
		}

		string NormalizeString(string pName)
		{
			string newPname = pName.Replace("svchost.exe", string.Empty);
			string noWhiteSpaces = newPname.Trim();

			return noWhiteSpaces;
		}

		void runCommand(string command)
		{
			Process process = new Process();
			process.StartInfo.FileName = "cmd.exe";
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.Start();
			process.StandardInput.WriteLine(command);
			process.StandardInput.Flush();
			process.StandardInput.Close();
		}

		string getParent(int pid)
		{
			string result;
			try
			{
				string queryString = string.Format("SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {0}", pid);
				ManagementObjectCollection.ManagementObjectEnumerator enumerator = new ManagementObjectSearcher("root\\CIMV2", queryString).Get().GetEnumerator();
				enumerator.MoveNext();
				result = Process.GetProcessById((int)((uint)enumerator.Current["ParentProcessId"])).ProcessName;
			}
			catch
			{
				result = "failed.";
			}
			return result;
		}

	}
}
