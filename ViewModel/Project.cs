using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ItalicPig.Bootstrap.ViewModel
{
    public class Project : ObservableObject
    {
        public bool Exists => _ProjectPath.Exists;
        public string Name => _ProjectPath.Name;
        public string Path => _ProjectPath.FullName;

        public bool IsBusy
        {
            get => _IsBusy;
            private set
            {
                if (_IsBusy != value)
                {
                    _IsBusy = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsIdle));
                    OnPropertyChanged(nameof(ShowSparseCheckoutOn));
                    OnPropertyChanged(nameof(ShowSparseCheckoutOff));
                    ((RelayCommand)RefreshCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)EnableSparseCheckoutCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)DisableSparseCheckoutCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)ApplyCommand).NotifyCanExecuteChanged();
                }
            }
        }
        public bool IsIdle => !IsBusy;

        public bool ShowSparseCheckoutOn => IsIdle && _Project.SparseCheckoutEnabled;
        public bool ShowSparseCheckoutOff => IsIdle && !_Project.SparseCheckoutEnabled;

        public ObservableCollection<View> Views { get; } = new ObservableCollection<View>();

        public string Log
        {
            get => _Log;
            set
            {
                if (_Log != value)
                {
                    _Log = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ExploreToCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand EnableSparseCheckoutCommand { get; }
        public ICommand DisableSparseCheckoutCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand CopyLogCommand { get; }
        public ICommand ClearLogCommand { get; }

        public Project(string projectPath)
        {
            _ProjectPath = new DirectoryInfo(projectPath);
            Refresh();
            ExploreToCommand = new RelayCommand(ExploreTo, () => Exists);
            RefreshCommand = new RelayCommand(Refresh, () => IsIdle);
            EnableSparseCheckoutCommand = new RelayCommand(EnableSparseCheckout, () => ShowSparseCheckoutOff);
            DisableSparseCheckoutCommand = new RelayCommand(DisableSparseCheckout, () => ShowSparseCheckoutOn);
            ApplyCommand = new RelayCommand(Apply, CanApply);
            CopyLogCommand = new RelayCommand(() => Clipboard.SetText(Log), () => Log != "");
            ClearLogCommand = new RelayCommand(ClearLog, () => Log != "");
        }

        public void Clone(string url) => Task.Run(() => CloneAsync(url));

        #region Private
        [MemberNotNull(nameof(_Project))]
        private void Refresh()
        {
            if (_Project != null)
            {
                _Project.PropertyChanged -= InnerPropertyChanged;
                _Project.SparseCheckoutPaths.CollectionChanged -= SparseCheckoutPathsChanged;
            }
            _Project = new Model.Project(Path);
            _Project.PropertyChanged += InnerPropertyChanged;
            _Project.SparseCheckoutPaths.CollectionChanged += SparseCheckoutPathsChanged;

            Views.ResetRange(_Project.Config.Views
                .Select(view => new View(_Project, view.Key))
                .OrderBy(view => view.Name));

            OnPropertyChanged(nameof(Exists));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Path));
            OnPropertyChanged(nameof(Views));
        }

        private void InnerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_Project.Config))
            {
                Refresh();
            }
            else if (e.PropertyName == nameof(_Project.SparseCheckoutEnabled))
            {
                OnPropertyChanged(nameof(ShowSparseCheckoutOn));
                OnPropertyChanged(nameof(ShowSparseCheckoutOff));
                ((RelayCommand)EnableSparseCheckoutCommand).NotifyCanExecuteChanged();
                ((RelayCommand)DisableSparseCheckoutCommand).NotifyCanExecuteChanged();
                ((RelayCommand)ApplyCommand).NotifyCanExecuteChanged();
            }
        }

        private void SparseCheckoutPathsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ((RelayCommand)ApplyCommand).NotifyCanExecuteChanged();
        }

        private void ExploreTo()
        {
            Process.Start("explorer.exe", Path);
        }

        private async Task CloneAsync(string url)
        {
            Application.Current.Dispatcher.Invoke(() => IsBusy = true);
            try
            {
                await Model.Git.ShowVersionAsync(AddLogOutput);
                await _Project.CloneAsync(url, AddLogOutput);
                await _Project.InstallLfsAsync(AddLogOutput);
                await _Project.SetConfigAsync("core.autocrlf", "false", Model.GitConfigScope.Local, AddLogOutput);
                Application.Current.Dispatcher.Invoke(Refresh);
            }
            catch (Exception Ex)
            {
                AddLogOutput($"{Ex.GetType().Name}: {Ex.Message}");
            }
            Application.Current.Dispatcher.Invoke(() => IsBusy = false);
        }

        private async void EnableSparseCheckout()
        {
            _Project.SparseCheckoutEnabled = true;
            await ApplyAsync();
        }

        private async void DisableSparseCheckout()
        {
            var Result = MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to disable sparse checkout? This will download any files you don't have already across the entire project.", "Disable Sparse Checkout", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (Result == MessageBoxResult.Yes)
            {
                _Project.SparseCheckoutEnabled = false;
                await ApplyAsync();
            }
        }

        private bool CanApply() => IsIdle && Views.Any(view => view.Dirty);

        private async void Apply() => await ApplyAsync();

        private async Task ApplyAsync()
        {
            Application.Current.Dispatcher.Invoke(() => IsBusy = true);
            try
            {
                await Model.Git.ShowVersionAsync(AddLogOutput);
                await _Project.ApplyAsync(AddLogOutput);
                foreach (var View in Views)
                {
                    View.Dirty = false;
                }
            }
            catch (Exception Ex)
            {
                AddLogOutput($"{Ex.GetType().Name}: {Ex.Message}");
            }
            Application.Current.Dispatcher.Invoke(() => IsBusy = false);
        }

        private void AddLogOutput(string output)
        {
            Application.Current.Dispatcher.Invoke(() =>
                {
                    Log += output + '\n';
                    ((RelayCommand)CopyLogCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)ClearLogCommand).NotifyCanExecuteChanged();
                });
        }

        private void ClearLog()
        {
            Log = "";
            ((RelayCommand)CopyLogCommand).NotifyCanExecuteChanged();
            ((RelayCommand)ClearLogCommand).NotifyCanExecuteChanged();
        }

        private readonly DirectoryInfo _ProjectPath;
        private Model.Project _Project;
        private string _Log = "";
        private bool _IsBusy;
        #endregion
    }
}
