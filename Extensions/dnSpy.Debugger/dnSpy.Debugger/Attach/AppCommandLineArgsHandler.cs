/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Attach;

namespace dnSpy.Debugger.Attach {
	[Export(typeof(IAppCommandLineArgsHandler))]
	sealed class AppCommandLineArgsHandler : IAppCommandLineArgsHandler {
		readonly Lazy<AttachableProcessesService> attachableProcessesService;
		readonly Lazy<DbgManager> dbgManager;

		[ImportingConstructor]
		AppCommandLineArgsHandler(Lazy<AttachableProcessesService> attachableProcessesService, Lazy<DbgManager> dbgManager) {
			this.attachableProcessesService = attachableProcessesService;
			this.dbgManager = dbgManager;
		}

		public double Order => 0;

		[DllImport("kernel32.dll")]
		private static extern bool SetEvent(IntPtr hEvent);
		[DllImport("kernel32.dll")]
		private static extern bool CloseHandle(IntPtr hObject);
		private async Task BreakOnAttach(AttachableProcess process) {
			TaskCompletionSource<bool> isDebuggingChangedSrc = new();
			TaskCompletionSource<bool> isRunningChangedSrc = new();

			EventHandler? IsDebuggingHandler=null;
			EventHandler? IsRunningHandler = null;
			var mgr = dbgManager.Value;


			IsDebuggingHandler = ( _, _) => { if (mgr.IsDebugging == true) isDebuggingChangedSrc.SetResult(true); };
			IsRunningHandler = ( _, _) => { if (mgr.IsRunning == true) isRunningChangedSrc.SetResult(true); };
			mgr.IsDebuggingChanged += IsDebuggingHandler;
			mgr.IsRunningChanged += IsRunningHandler;
			process.Attach();
			if (mgr.IsRunning != true || mgr.IsDebugging != true)
				await Task.WhenAny(Task.WhenAll(isDebuggingChangedSrc.Task, isRunningChangedSrc.Task), Task.Delay(TimeSpan.FromSeconds(10)));
			mgr.IsDebuggingChanged -= IsDebuggingHandler;
			mgr.IsRunningChanged -= IsRunningHandler;
			if (mgr.IsRunning == true && mgr.IsDebugging == true)
				mgr.BreakAll();
		}
		public async void OnNewArgs(IAppCommandLineArgs args) {
			AttachableProcess? process=null;
			if (args.DebugAttachPid is int pid && pid != 0) {
				var processes = await attachableProcessesService.Value.GetAttachableProcessesAsync(null, new[] { pid }, null, CancellationToken.None).ConfigureAwait(false);
				process = processes.FirstOrDefault(p => p.ProcessId == pid);
				if (args.DebugEvent != 0) {
					var evt = new IntPtr(args.DebugEvent);
					SetEvent(evt);
					CloseHandle(evt);
				}
			}
			else if (args.DebugAttachProcess is string processName && !string.IsNullOrEmpty(processName)) {
				var processes = await attachableProcessesService.Value.GetAttachableProcessesAsync(processName, CancellationToken.None).ConfigureAwait(false);
				process = processes.FirstOrDefault();
			}
			if (args.DebugBreakOnAttach && process != null)
					await BreakOnAttach(process);
				else
					process?.Attach();
		}

	}
}
