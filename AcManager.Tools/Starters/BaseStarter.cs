using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcManager.Tools.Managers;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Helpers;

namespace AcManager.Tools.Starters {
    public abstract class BaseStarter : IAcsPlatformSpecificStarter {
        public bool Use32Version { get; set; }

        protected Process LauncherProcess, GameProcess;

        protected string AcsName => Use32Version ? "acs_x86.exe" : "acs.exe";

        protected string AcsFilename => Path.Combine(AcRootDirectory.Instance.RequireValue, AcsName);

        public abstract void Run();

        public void WaitUntilGame() {
            if (GameProcess != null) return;

            for (var i = 0; i < 100; i++) {
                GameProcess = Process.GetProcessesByName(AcsName.ApartFromLast(".exe", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (GameProcess != null) break;
                Thread.Sleep(500);
            }

            Thread.Sleep(1000);
            GameProcess = Process.GetProcessesByName(AcsName.ApartFromLast(".exe", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        public virtual void WaitGame() {
            GameProcess?.WaitForExit();
        }

        public virtual void CleanUp() {
            if (LauncherProcess != null) {
                LauncherProcess.Dispose();
                LauncherProcess = null;
            }

            if (GameProcess != null) {
                try {
                    if (!GameProcess.HasExited) {
                        GameProcess.Kill();
                    }
                } catch (Exception e) {
                    Logging.Warning("[BaseStarter] Process killing exception: " + e);
                }

                GameProcess.Dispose();
                GameProcess = null;
            }
        }

        public virtual Task RunAsync(CancellationToken cancellation) {
            return Task.Run(() => Run(), cancellation);
        }

        private static bool IsAny() {
            return Process.GetProcessesByName("acs.exe").Any() || Process.GetProcessesByName("acs_x86.exe").Any() ||
                    Process.GetProcessesByName("AssettoCorsa.exe").Any();
        }

        public async Task WaitUntilGameAsync(CancellationToken cancellation) {
            if (GameProcess != null) return;

            Logging.Warning("[BaseStarter] WaitUntilGameAsync(): first stage");

            var nothing = 0;
            for (var i = 0; i < 999; i++) {
                GameProcess = Process.GetProcessesByName(AcsName.ApartFromLast(".exe", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (GameProcess != null) break;
                
                if (IsAny()) {
                    nothing = 0;
                } else if (++nothing > 50) {
                    Logging.Warning("[BaseStarter] WaitUntilGameAsync(): looks like the game is dead");
                    break;
                }

                await Task.Delay(1000, cancellation);
                if (cancellation.IsCancellationRequested) {
                    Logging.Warning("[BaseStarter] WaitUntilGameAsync(): cancelled");
                    return;
                }
            }
        }

        public virtual async Task WaitGameAsync(CancellationToken cancellation) {
            if (GameProcess == null) return;
            await GameProcess.WaitForExitAsync(cancellation);
        }

        public virtual async Task CleanUpAsync(CancellationToken cancellation) {
            await Task.Run(() => CleanUp(), cancellation);
        }
    }
}