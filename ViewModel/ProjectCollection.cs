using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ItalicPig.Bootstrap.ViewModel
{
    public class ProjectCollection : ObservableObject
    {
        public ObservableCollection<Project> Projects { get; } = new ObservableCollection<Project>();

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

        public ProjectCollection() : this(LoadSourceTreeTabs()) { }

        public ProjectCollection(IEnumerable<string> projectPaths)
        {
            Projects.AddRange(projectPaths.Select(path => new Project(path)));
            var LastSelectedProject = Properties.Settings.Default.SelectedProject;
            SelectedProject = Projects.FirstOrDefault(p => p.Path == LastSelectedProject);
            if (SelectedProject == null)
            {
                SelectedProject = Projects.FirstOrDefault();
            }
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

        private Project? _SelectedProject;
        #endregion
    }
}
