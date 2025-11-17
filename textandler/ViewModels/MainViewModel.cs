using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using textHandlerClass;
using textHandlerApp.Infrastructure;

namespace textHandlerApp.ViewModels
{
	public enum Status
	{
        [Description("Need choose file(s) for process")]
		NeedChooseProcessFile,

        [Description("Need choose file for save")]
        NeedChooseSaveFile,

        [Description("Can process")]
        CanProcess,

        [Description("Processing")]
        Process,

        [Description("Cancelling")]
        Cancel,

        [Description("Done")]
        Done,

    }

	public static class EnumExtensions
	{
		public static string GetDescription(this Enum value)
		{
			var fieldInfo = value.GetType().GetField(value.ToString());
			var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
			return attributes.Length > 0 ? attributes[0].Description : value.ToString();
		}

	}

    public sealed class MainViewModel : INotifyPropertyChanged
	{
		private readonly TextHandler textHandler;

		public MainViewModel()
		{
			textHandler = new TextHandler();

			SelectInputFilesCommand = new RelayCommand(_ => SelectInputFiles());
			SelectSaveFileCommand = new RelayCommand(_ => SelectSaveFile(), _ => inputFiles?.Length > 0);
			ProcessCommand = new RelayCommand(async _ => await ProcessAsync(), _ => CanProcess());
			CancelCommand = new RelayCommand(_ => Cancel(), _ => isProcessing);
		}

		private string[] inputFiles;
		public string[] InputFiles
		{
			get => inputFiles;
			set
			{
				if (inputFiles == value) return;
				inputFiles = value;
				OnPropertyChanged();
				((RelayCommand)SelectSaveFileCommand).RaiseCanExecuteChanged();
				((RelayCommand)ProcessCommand).RaiseCanExecuteChanged();
			}
		}

		private string outputFile;
		public string OutputFile
		{
			get => outputFile;
			set
			{
				if (outputFile == value) return;
				outputFile = value;
				OnPropertyChanged();
				((RelayCommand)ProcessCommand).RaiseCanExecuteChanged();
			}
		}

		private uint minAmountOfSymbols = 3;
		public uint MinAmountOfSymbols
		{
			get => minAmountOfSymbols;
			set
			{
				if (minAmountOfSymbols == value) return;
				minAmountOfSymbols = value;
				textHandler.MinAmountOfSymbols = value;
				OnPropertyChanged();
				((RelayCommand)ProcessCommand).RaiseCanExecuteChanged();
			}
		}

		private bool needDeletePunctuationMarks = false;
		public bool NeedDeletePunctuationMarks
		{
			get => needDeletePunctuationMarks;
			set
			{
				if (needDeletePunctuationMarks == value) return;
				needDeletePunctuationMarks = value;
				textHandler.NeedDeletePunctuationMarks = value;
				OnPropertyChanged();
			}
		}

		private bool isProcessing;
		public bool IsProcessing
		{
			get => isProcessing;
			set
			{
				if (isProcessing == value) return;
				isProcessing = value;
				OnPropertyChanged();
				((RelayCommand)ProcessCommand).RaiseCanExecuteChanged();
				((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
			}
		}

		private int progressValue;
		public int ProgressValue
		{
			get => progressValue;
			set { if (progressValue == value) return; progressValue = value; OnPropertyChanged(); }
		}

		private string statusText = Status.NeedChooseProcessFile.GetDescription();
		public string StatusText
		{
			get => statusText;
			set { if (statusText == value) return; statusText = value; OnPropertyChanged(); }
		}

		public ICommand SelectInputFilesCommand { get; }
		public ICommand SelectSaveFileCommand { get; }
		public ICommand ProcessCommand { get; }
		public ICommand CancelCommand { get; }

		private static string fileFilter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
		private void SelectInputFiles()
		{
			var dialog = new OpenFileDialog
			{
				Filter = fileFilter,
				Multiselect = true
			};
			if (dialog.ShowDialog() == true)
			{
				InputFiles = dialog.FileNames;
				StatusText = Status.NeedChooseSaveFile.GetDescription();
			}
		}

		private void SelectSaveFile()
		{
			var dialog = new SaveFileDialog
			{
				Filter = fileFilter
			};
			if (dialog.ShowDialog() == true)
			{
				OutputFile = dialog.FileName;
				StatusText = Status.CanProcess.GetDescription();
			}
		}

		private bool CanProcess()
		{
			return !IsProcessing && inputFiles != null && inputFiles.Length > 0 && !string.IsNullOrWhiteSpace(outputFile) && MinAmountOfSymbols > 0;
		}

		private void ClearFiles()
		{
			InputFiles = Array.Empty<string>();
			OutputFile = "";
        }

		private CancellationTokenSource cancellationTokenSource;
		private async Task ProcessAsync()
		{
			if (!CanProcess()) return;
			IsProcessing = true;
			cancellationTokenSource = new CancellationTokenSource();
			StatusText = Status.Process.GetDescription();
			ProgressValue = 0;

			textHandler.InputFiles = InputFiles;
			textHandler.OutputFiles = BuildOutputFilesArray(OutputFile, InputFiles?.Length ?? 1);

			var total = InputFiles?.Length ?? 0;
			var progress = new Progress<int>(v =>
			{
				ProgressValue = v;
				StatusText = total > 1 ? $"Processed {v}% of {total} files" : $"Processed {v}%";
			});

			try
			{
				await textHandler.ProcessFiles(progress, cancellationTokenSource.Token);
				ClearFiles();
                StatusText = Status.NeedChooseProcessFile.GetDescription();
            }
			catch (Exception ex)
			{
				StatusText = ex.Message;
			}
			finally
			{
				IsProcessing = false;
			}
		}

		private void Cancel()
		{
			if (IsProcessing && cancellationTokenSource != null)
			{
				cancellationTokenSource.Cancel();
				StatusText = Status.Cancel.GetDescription();
			}
		}

		private static string[] BuildOutputFilesArray(string firstOutputPath, int count)
		{
			if (count == 1) return new[] { firstOutputPath };
			var directory = Path.GetDirectoryName(firstOutputPath);
			var filename = Path.GetFileNameWithoutExtension(firstOutputPath);
			var ext = Path.GetExtension(firstOutputPath);
			var result = new string[count];
			for (int i = 0; i < count; i++)
			{
				result[i] = Path.Combine(directory ?? string.Empty, $"{filename}_{i + 1}{ext}");
			}
			return result;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}


