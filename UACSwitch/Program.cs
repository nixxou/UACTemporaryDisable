using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System.Diagnostics;
using System.Security.Principal;
internal class Program
{
	private static void Main(string[] args)
	{
		string current_exe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

		if (!IsAdministrator())
		{
			if (CheckTaskExist("TempDisableUAC"))
			{
				ExecuteTask("TempDisableUAC");
			}
			return;
		}
		else
		{
			if (args.Length == 0)
			{
				if (CheckTaskExist("TempDisableUAC"))
				{
					using (TaskService ts = new TaskService())
					{
						ts.RootFolder.DeleteTask("TempDisableUAC");
					}
				}
				RegisterTask("TempDisableUAC", current_exe, "10");
				Thread.Sleep(1000);
				ExecuteTask("TempDisableUAC");
			}
			else
			{
				if (args[0] != "")
				{
					byte timeToSleep = 0;
					bool canConvert = byte.TryParse(args[0], out timeToSleep);
					if (canConvert == true)
					{
						UACDisable();
						Thread.Sleep(timeToSleep * 1000);
						UACEnable();
					}
				}
			}

		}
	}

	public static void UACEnable()
	{
		RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
		key.SetValue("ConsentPromptBehaviorAdmin", 5);
		key.SetValue("PromptOnSecureDesktop ", 1);
	}
	public static void UACDisable()
	{
		RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
		key.SetValue("ConsentPromptBehaviorAdmin", 0);
		key.SetValue("PromptOnSecureDesktop ", 0);
	}
	public static bool IsAdministrator()
	{
		WindowsIdentity identity = WindowsIdentity.GetCurrent();
		WindowsPrincipal principal = new WindowsPrincipal(identity);
		return principal.IsInRole(WindowsBuiltInRole.Administrator);
	}
	public static bool CheckTaskExist(string taskName)
	{
		using (TaskService taskService = new TaskService())
		{
			var task = taskService.GetTask(taskName);
			if (taskService.GetTask(taskName) == null)
			{
				return false;
			}
			else
			{

				return true;
			}
		}
	}

	public static void ExecuteTask(string taskName, int delay = 2000)
	{
		string new_cmd = $@" /I /run /tn ""{taskName}""";
		ProcessStartInfo startInfo = new ProcessStartInfo();
		startInfo.FileName = "schtasks";
		startInfo.Arguments = new_cmd;
		startInfo.Verb = "runas";
		startInfo.UseShellExecute = false;
		startInfo.CreateNoWindow = true;
		var TaskProcess = System.Threading.Tasks.Task.Run(() => Process.Start(startInfo));
		TaskProcess.Wait();
	}

	public static void RegisterTask(string taskName, string executable, string args)
	{
		using (TaskService taskService = new TaskService())
		{
			if (taskService.GetTask(taskName) == null)
			{
				TaskDefinition td = taskService.NewTask();
				td.RegistrationInfo.Description = "TempDisableUAC";

				td.Principal.RunLevel = TaskRunLevel.Highest;
				td.Principal.LogonType = TaskLogonType.S4U;

				// Create an action that will launch Notepad whenever the trigger fires
				td.Actions.Add(executable, args, null);

				// Register the task in the root folder
				taskService.RootFolder.RegisterTaskDefinition(taskName, td, TaskCreation.CreateOrUpdate, Environment.GetEnvironmentVariable("USERNAME"), null, TaskLogonType.S4U, null);


			}
		}
	}


}