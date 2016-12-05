﻿using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.UploadProgressDialog;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.GcsFileBrowser
{
    public class GcsBrowserViewModel : ViewModelBase
    {
        private readonly static GcsBrowserState s_emptyState =
            new GcsBrowserState(Enumerable.Empty<GcsRow>(), "/");

        private readonly GcsFileBrowserWindow _owner;
        private readonly SelectionUtils _selectionUtils;
        private Bucket _bucket;
        private GcsDataSource _dataSource;
        private bool _isLoading;
        private readonly List<GcsBrowserState> _stateStack = new List<GcsBrowserState>();
        private IList<GcsRow> _selectedItems;

        public Bucket Bucket
        {
            get { return _bucket; }
            set
            {
                SetValueAndRaise(ref _bucket, value);
                InvalidateBucket();
            }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            private set
            {
                SetValueAndRaise(ref _isLoading, value);
                RaisePropertyChanged(nameof(IsReady));
            }
        }

        public GcsBrowserState Top
        {
            get
            {
                if (_stateStack.Count == 0)
                {
                    return s_emptyState;
                }
                return _stateStack.Last();
            }
        }

        public bool IsReady => !IsLoading;

        public IList<GcsRow> SelectedItems
        {
            get { return _selectedItems; }
            private set
            {
                SetValueAndRaise(ref _selectedItems, value);
                InvalidateSelectedItem();
            }
        }

        public GcsRow SelectedItem => SelectedItems.FirstOrDefault();

        public ICommand PopAllCommand { get; }

        public ICommand NavigateToCommand { get; }

        public ICommand ShowDirectoryCommand { get; }

        public ICommand RefreshCommand { get; }

        public GcsBrowserViewModel(GcsFileBrowserWindow owner)
        {
            _owner = owner;
            _selectionUtils = new SelectionUtils(owner);

            PopAllCommand = new ProtectedCommand(OnPopAllCommand);
            NavigateToCommand = new ProtectedCommand<string>(OnNavigateToCommand);
            ShowDirectoryCommand = new ProtectedCommand<GcsRow>(OnShowDirectoryCommand);
            RefreshCommand = new ProtectedCommand(OnRefreshCommand);
        }

        public async void StartFileUpload(string[] files)
        {
            var uploadOperations = CreateUploadOperations(files);

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            foreach (var operation in uploadOperations)
            {
                _dataSource.StartUploadOperation(
                    sourcePath: operation.Source,
                    bucket: operation.Bucket,
                    name: operation.Destination,
                    operation: operation,
                    token: tokenSource.Token);
            }

            UploadProgressDialogWindow.PromptUser(uploadOperations, tokenSource);

            RefreshTopState();
        }

        private List<UploadOperation> CreateUploadOperations(string[] files)
        {
            var result = new List<UploadOperation>();

            foreach (var input in files)
            {
                var info = new FileInfo(input);
                var isDirectory = (info.Attributes & FileAttributes.Directory) != 0;

                if (isDirectory)
                {
                    result.AddRange(CreateUploadOperationsForDirectory(info.FullName, info.Name));
                }
                else
                {
                    result.Add(new UploadOperation(
                        source: info.FullName,
                        bucket: Bucket.Name,
                        destination: $"{Top.CurrentPath}{info.Name}"));
                }
            }

            return result;
        }

        private IEnumerable<UploadOperation> CreateUploadOperationsForDirectory(string dir, string localPath)
        {
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                yield return new UploadOperation(
                    source: file,
                    bucket: Bucket.Name,
                    destination: $"{Top.CurrentPath}{localPath}/{Path.GetFileName(file)}");
            }

            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                foreach (var operation in CreateUploadOperationsForDirectory(subDir, $"{localPath}/{Path.GetFileName(subDir)}"))
                {
                    yield return operation;
                }
            }
        }

        public void InvalidateSelectedItems(IEnumerable<GcsRow> selectedRows)
        {
            SelectedItems = selectedRows.ToList();
        }

        #region Command handlers

        private void OnPopAllCommand()
        {
            PopToRoot();
        }

        private void OnNavigateToCommand(string step)
        {
            PopToState(step);
        }

        private void OnShowDirectoryCommand(GcsRow dir)
        {
            PushToDirectory(dir.Name);
        }

        private void OnRefreshCommand()
        {
            RefreshTopState();
        }

        #endregion

        #region Navigation stack methods

        private async void RefreshTopState()
        {
            GcsBrowserState newState;
            try
            {
                IsLoading = true;

                newState = await LoadStateForDirectoryAsync(Top.Name);
            }
            catch (DataSourceException ex)
            {
                Debug.WriteLine($"Failed to refersh directory {Top.Name}: {ex.Message}");
                newState = CreateErrorState(Top.Name);
            }
            finally
            {
                IsLoading = false;
            }

            _stateStack[_stateStack.Count - 1] = newState;
            RaisePropertyChanged(nameof(Top));
        }


        private void PopToRoot()
        {
            _stateStack.RemoveRange(1, _stateStack.Count - 1);
            RaisePropertyChanged(nameof(Top));
        }

        private void PopState()
        {
            if (_stateStack.Count == 1)
            {
                return;
            }

            _stateStack.RemoveRange(_stateStack.Count - 1, 1);
            RaisePropertyChanged(nameof(Top));
        }

        private void PopToState(string step)
        {
            var idx = _stateStack.FindIndex(x => x.Name == step);
            if (idx == -1)
            {
                Debug.WriteLine($"Could not find {step}");
            }

            _stateStack.RemoveRange(idx + 1, _stateStack.Count - (idx + 1));
            RaisePropertyChanged(nameof(Top));
        }

        private async void PushToDirectory(string name)
        {
            GcsBrowserState state;
            try
            {
                state = await LoadStateForDirectoryAsync(name);
            }
            catch (DataSourceException ex)
            {
                Debug.WriteLine($"Failed to load directory {name}: {ex.Message}");
                state = CreateErrorState(name);
            }

            _stateStack.Add(state);
            RaisePropertyChanged(nameof(Top));
        }

        #endregion

        private void InvalidateBucket()
        {
            _dataSource = new GcsDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            PushToDirectory("");
        }

        private void InvalidateSelectedItem()
        {
            RaisePropertyChanged(nameof(SelectedItem));

            if (SelectedItem != null)
            {
                PropertyWindowItemBase item;
                if (SelectedItem.IsDirectory)
                {
                    item = new GcsDirectoryItem(SelectedItem);
                }
                else
                {
                    item = new GcsFileItem(SelectedItem);
                }
                _selectionUtils.SelectItem(item);
            }
            else
            {
                _selectionUtils.ClearSelection();
            }
        }

        private async Task<GcsBrowserState> LoadStateForDirectoryAsync(string name)
        {
            try
            {
                IsLoading = true;

                var dir = await _dataSource.GetDirectoryListAsync(Bucket.Name, name);
                var items = Enumerable.Concat<GcsRow>(
                    dir.Prefixes.OrderBy(x => x).Select(GcsRow.CreateDirectoryRow),
                    dir.Items.OrderBy(x => x.Name).Select(GcsRow.CreateFileRow));

                return new GcsBrowserState(items, name);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static GcsBrowserState CreateErrorState(string name) =>
            new GcsBrowserState(new List<GcsRow> { GcsRow.CreateErrorRow($"Failed to load directory {name}.") }, name);
    }
}
