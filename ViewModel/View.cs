using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ItalicPig.Bootstrap.ViewModel
{
    public class View : ObservableObject
    {
        public string Name { get; }
        public string Tooltip
        {
            get
            {
                if (_Project.Config.Views.TryGetValue(Name, out var Paths))
                {
                    return string.Join('\n', Paths);
                }
                return "";
            }
        }

        public bool IsActive
        {
            get => _Project.IsViewActive(Name);
            set
            {
                Dirty = true;
                _Project.SetViewActive(Name, value);
            }
        }

        public bool Dirty
        {
            get => _Dirty;
            set
            {
                if (_Dirty != value)
                {
                    _Dirty = value;
                    OnPropertyChanged();
                }
            }
        }

        public View(Model.Project project, string name)
        {
            Name = name;
            _Project = project;
            _Project.PropertyChanged += InnerPropertyChanged;
            _Project.SparseCheckoutPaths.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsActive));
        }

        #region Private
        private void InnerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_Project.Config))
            {
                OnPropertyChanged(nameof(Tooltip));
                OnPropertyChanged(nameof(IsActive));
            }
        }

        private readonly Model.Project _Project;
        private bool _Dirty;
        #endregion
    }
}
