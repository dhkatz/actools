using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using FirstFloor.ModernUI.Helpers;

namespace AcManager.Controls.Helpers {
    public static class AwesomiumResolverService {
        public static bool IsInitialized => _awesomiumPath != null;

        #region Fields
        private const string DllExtension = ".dll";
        private static readonly string[] Dependencies;
        private static readonly string[] Resources;
        private static string _awesomiumPath;
        #endregion

        #region Ctor
        static AwesomiumResolverService() {
            Dependencies = new [] {
                "awesomium.core",
                "awesomium.windows.controls",
                "awesomium.windows.forms"
            };

            Resources = new [] {
                "awesomium.core.Resources",
                "awesomium.windows.controls.Resources"
            };
        }
        #endregion

        #region Methods
        /// <summary>
        /// Initializes and activates the <see cref="AwesomiumResolverService"/>.
        /// </summary>
        /// <param name="directoryPath">
        /// The path to the directory containing the Awesomium.NET assemblies
        /// and its native Awesomium references.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// A null reference or an empty string specified for <paramref name="directoryPath"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The directory specified does not exist or it does not contain the
        /// necessary Awesomium.NET assemblies.
        /// </exception>
        public static void Initialize(string directoryPath) {
            if (string.IsNullOrEmpty(directoryPath)) {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            if (!directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                directoryPath += Path.DirectorySeparatorChar;
            }

            var dllPath = $"{directoryPath}{Dependencies[0]}{DllExtension}";
            if (!File.Exists(dllPath)) {
                throw new ArgumentException("The directory specified does not contain the Awesomium.NET assemblies", nameof(directoryPath));
            }

            _awesomiumPath = directoryPath;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAwesomium;

            Logging.Write("[AwesomiumResolverService] Initialize(): " + directoryPath);
        }

        /// <summary>
        /// Stops monitoring for requested Awesomium.NET assemblies.
        /// </summary>
        public static void Stop() {
            _awesomiumPath = null;
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAwesomium;
        }
        #endregion

        #region Event Handlers
        private static Assembly ResolveAwesomium(object sender, ResolveEventArgs args) {
            var unresolved = args.Name.ToLower();
            var resourcesDll = Resources.SingleOrDefault(item => unresolved.StartsWith(item));

            if (!string.IsNullOrEmpty(resourcesDll)) {
                var resourceId = CultureInfo.CurrentUICulture.IetfLanguageTag;

                if (string.Compare(resourceId, "en-US", StringComparison.OrdinalIgnoreCase) != 0) {
                    string resourcesPath = $"{_awesomiumPath}{resourceId}{Path.DirectorySeparatorChar}{resourcesDll}{DllExtension}";

                    if (File.Exists(resourcesPath))
                        return Assembly.LoadFrom(resourcesPath);
                }
            }

            var dependencyDll = Dependencies.SingleOrDefault(item => unresolved.StartsWith(item));

            if (string.IsNullOrEmpty(dependencyDll)) return null;
            string dependencyPath = $"{_awesomiumPath}{Path.DirectorySeparatorChar}{dependencyDll}{DllExtension}";
            return File.Exists(dependencyPath) ? Assembly.LoadFrom(dependencyPath) : null;
        }
        #endregion
    }
}