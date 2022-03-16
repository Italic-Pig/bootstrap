using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ItalicPig.Bootstrap.ViewModel
{
    public class ProjectClone : ObservableObject
    {
        public string Url
        {
            get => _Url;
            set
            {
                if (_Url != value)
                {
                    _Url = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProjectName));
                    OnPropertyChanged(nameof(IsUrlValid));
                    OnPropertyChanged(nameof(ValidationMessage));
                    ((RelayCommand<Window>)CloneCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public string ProjectName => Uri.TryCreate(_Url, UriKind.Absolute, out var ParsedUri) ? Path.GetFileName(ParsedUri.AbsolutePath).Replace(".git", "") : "?";

        public bool IsUrlValid => Uri.TryCreate(_Url, UriKind.Absolute, out _);

        public static string PathSeparator => Path.DirectorySeparatorChar.ToString();

        public string Folder
        {
            get => _Folder;
            set
            {
                if (_Folder != value)
                {
                    _Folder = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsFolderValid));
                    OnPropertyChanged(nameof(ValidationMessage));
                    ((RelayCommand<Window>)CloneCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsFolderValid => Path.IsPathRooted(_Folder);

        public string ValidationMessage
        {
            get
            {
                var Name = ProjectName;
                if (!IsUrlValid || Name == "" || Name == "?")
                {
                    return "That URL is invalid.";
                }
                if (_ProjectCollection.Projects.Any(p => p.Name == Name))
                {
                    return "That project already exists.";
                }
                if (!IsFolderValid)
                {
                    return "That folder is invalid.";
                }
                var FullPath = Path.Combine(_Folder, ProjectName);
                if (Directory.Exists(FullPath) && Directory.EnumerateFileSystemEntries(FullPath).Any())
                {
                    return "That folder already exists and isn't empty.";
                }
                return "";
            }
        }

        public ICommand CloneCommand { get; }

        public ProjectClone(ProjectCollection projectCollection)
        {
            _ProjectCollection = projectCollection;
            CloneCommand = new RelayCommand<Window>(Clone, CanClone);

            // Try to find a better default folder based on the location of other projects
            var MostCommonPath = _ProjectCollection.Projects
                    .Select(project => Path.GetDirectoryName(project.Path) ?? "")
                    .GroupBy(path => path)
                    .MaxBy(group => group.Count())
                    ?.First();
            if (!string.IsNullOrEmpty(MostCommonPath))
            {
                _Folder = MostCommonPath;
            }
        }

        #region Private
        private bool CanClone(Window? cloneWindow) => IsUrlValid && IsFolderValid && ProjectName != "" && ProjectName != "?";

        private void Clone(Window? cloneWindow)
        {
            var FullPath = Path.Combine(_Folder, ProjectName);
            if (CreateFolder(FullPath))
            {
                var NewProject = new Project(FullPath);
                _ProjectCollection.Projects.Add(NewProject);
                _ProjectCollection.SelectedProject = NewProject;
                NewProject.Clone(_Url);
                cloneWindow?.Close();
            }
        }

        private static bool CreateFolder(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception Ex) when (Ex is FileNotFoundException || Ex is UnauthorizedAccessException)
            {
                MessageBox.Show(Application.Current.MainWindow, $"Failed to create folder '{path}'.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        private readonly ProjectCollection _ProjectCollection;
        private string _Url = "https://github.com/Italic-Pig/project-name";
        private string _Folder = "C:\\Work\\Italic Pig";
        #endregion
    }
}
