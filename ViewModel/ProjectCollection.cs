using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
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

        public ICommand RefreshCommand { get; }

        public ProjectCollection() : this(LoadSourceTreeTabs()) { }

        public ProjectCollection(IEnumerable<string> projectPaths)
        {
            Refresh(projectPaths);
            RefreshCommand = new RelayCommand(Refresh, CanRefresh);
        }

        #region Private
        private static string[] LoadSourceTreeTabs()
        {
            var OpenTabsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Atlassian", "SourceTree", "opentabs.xml");
            var Serializer = new XmlSerializer(typeof(string[]));
            using var Reader = new StreamReader(OpenTabsPath);
            var Tabs = (string[]?)Serializer.Deserialize(Reader);
            return Tabs ?? Array.Empty<string>();
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

            SelectedProject = Projects.FirstOrDefault(p => p.Path == LastSelectedProject);
            if (SelectedProject == null)
            {
                SelectedProject = Projects.FirstOrDefault();
            }
        }

        private Project? _SelectedProject;
        #endregion
    }
}
