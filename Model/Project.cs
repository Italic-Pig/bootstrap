using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ItalicPig.Bootstrap.Model
{
    public class Project : ObservableObject
    {
        public string Path { get; }

        public BootstrapConfig Config
        {
            get => _Config;
            private set
            {
                if (_Config != value)
                {
                    _Config = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool SparseCheckoutEnabled
        {
            get => _SparseCheckoutEnabled;
            set
            {
                if (_SparseCheckoutEnabled != value)
                {
                    _SparseCheckoutEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> SparseCheckoutPaths { get; } = new ObservableCollection<string>();

        public Project(string projectPath)
        {
            Path = projectPath;
            _Config = BootstrapConfig.Read(Path);
            _SparseCheckoutEnabled = Git.ReadSparseCheckoutConfig(Path);

            // Get Git's sparse checkout paths without all the ! and * paths (just the fully included paths)
            SparseCheckoutPaths.AddRange(Git.ReadSparseCheckoutPaths(Path));
            var ActivePaths = SparseCheckoutPaths
                .Where(p => !p.Contains('*') && !p.Contains('!') && IsPathIncludedInSparseCheckout(p))
                .ToList();
            SparseCheckoutPaths.ResetRange(ActivePaths);
        }

        public bool IsViewActive(string viewName)
        {
            return _Config.Views[viewName].TrueForAll(IsPathIncludedInSparseCheckout);
        }

        public void SetViewActive(string viewName, bool active)
        {
            var AllActivePaths = _Config.Views
                .Where(view => (view.Key == viewName) ? active : IsViewActive(view.Key))
                .SelectMany(view => view.Value)
                .Distinct()
                .Select(MakeGitPath)
                .ToList();
            SparseCheckoutPaths.ResetRange(AllActivePaths);
        }

        public Task<CliWrap.CommandResult> CloneAsync(string url, Action<string>? outputCallback = null)
        {
            return Git.CloneAsync(Path, url, outputCallback);
        }

        public Task<CliWrap.CommandResult> ApplyAsync(Action<string>? outputCallback = null)
        {
            if (_SparseCheckoutEnabled)
            {
                return Git.EnableSparseCheckoutAsync(Path, SparseCheckoutPaths.Select(MakeBarePath), outputCallback);
            }
            else
            {
                return Git.DisableSparseCheckoutAsync(Path, outputCallback);
            }
        }

        #region Private
        private bool IsPathIncludedInSparseCheckout(string path)
        {
            if (path == "")
            {
                return false;
            }

            // This path must be in the sparse checkout list, including its subdirectories
            // For example, imagine the folder structure /a/b/ - if the sparse checkout is just b, SparseCheckoutPaths will look like this:
            /// /*
            /// !/*/
            /// /a/
            /// !/a/*/
            /// /a/b/
            path = MakeGitPath(path);
            if (SparseCheckoutPaths.Contains(path) && !SparseCheckoutPaths.Contains("!" + path + "*/"))
            {
                return true;
            }

            // This path isn't in the list, but one of its ancestors may be
            var LastSlash = path.LastIndexOf('/', path.Length - 2);
            if (LastSlash == -1)
            {
                return false;
            }
            else
            {
                return IsPathIncludedInSparseCheckout(path[..LastSlash]);
            }
        }

        private static string MakeGitPath(string path)
        {
            if (!path.StartsWith('/'))
            {
                path = '/' + path;
            }
            if (!path.EndsWith('/'))
            {
                path += '/';
            }
            return path;
        }

        private static string MakeBarePath(string path) => path.Trim('/');

        private BootstrapConfig _Config;
        private bool _SparseCheckoutEnabled;
        #endregion
    }
}
