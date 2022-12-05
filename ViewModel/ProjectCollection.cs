using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Shell;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ItalicPig.Bootstrap.ViewModel
{
    public class ProjectCollection : ObservableObject
    {
        public ObservableCollection<Project> Projects { get; } = new ObservableCollection<Project>();
        public bool ShowProjects => Projects.Count > 0;

        public Project? SelectedProject
        {
            get => _SelectedProject;
            set
            {
                if (_SelectedProject != value)
                {
                    _SelectedProject = value;
                    Properties.Settings.Default.SelectedProject = _SelectedProject?.Path ?? "";
                    Properties.Settings.Default.Save();
                    OnPropertyChanged();
                }
            }
        }

        public TaskbarItemProgressState TaskbarProgressState => Projects.Any(project => project.IsBusy) ? TaskbarItemProgressState.Indeterminate : TaskbarItemProgressState.None;

        public ICommand RefreshCommand { get; }

        public ProjectCollection() : this(LoadSourceTreeTabs()) { }

        public ProjectCollection(IEnumerable<string> projectPaths)
        {
            Refresh(projectPaths);
            RefreshCommand = new RelayCommand(Refresh, CanRefresh);
        }

        public void Save() => SaveSourceTreeTabs(Projects.Select(p => p.Path).ToArray());

        #region Private
        private static string SourceTreeTabsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Atlassian", "SourceTree", "opentabs.xml");

        private static string[] LoadSourceTreeTabs()
        {
            var Serializer = new XmlSerializer(typeof(string[]));
            try
            {
                using var Reader = new StreamReader(SourceTreeTabsPath);
                var Tabs = (string[]?)Serializer.Deserialize(Reader);
                return Tabs ?? Array.Empty<string>();
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
        }

        private static void SaveSourceTreeTabs(string[] tabs)
        {
            var Serializer = new XmlSerializer(typeof(string[]));
            try
            {
                using var Writer = new StreamWriter(SourceTreeTabsPath);
                var XmlSettings = new System.Xml.XmlWriterSettings() { Indent = true };
                using var XmlWriter = System.Xml.XmlWriter.Create(Writer, XmlSettings);
                Serializer.Serialize(XmlWriter, tabs);
            }
            catch (IOException) { }
        }

        private bool CanRefresh() => Projects.All(p => p.IsIdle);

        private void Refresh() => Refresh(LoadSourceTreeTabs());

        private void Refresh(IEnumerable<string> projectPaths)
        {
            var LastSelectedProject = Properties.Settings.Default.SelectedProject;
            var OldProjectCount = Projects.Count;

            Projects.ResetRange(projectPaths.Select(path => new Project(path)));

            if (Projects.Count != OldProjectCount)
            {
                OnPropertyChanged(nameof(ShowProjects));
            }

            SelectedProject = Projects.FirstOrDefault(p => p.Path == LastSelectedProject) ?? Projects.FirstOrDefault();

            foreach (var Project in Projects)
            {
                Project.PropertyChanged += ProjectPropertyChanged;
            }
        }

        private void ProjectPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Project.IsBusy))
            {
                OnPropertyChanged(nameof(TaskbarProgressState));
            }
        }

        private Project? _SelectedProject;
        #endregion
    }
}
