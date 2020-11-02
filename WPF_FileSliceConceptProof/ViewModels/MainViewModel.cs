using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism;
using Prism.Mvvm;
using Prism.Commands;
using System.Diagnostics;
using System.IO;

namespace WPF_FileSliceConceptProof
{
    class MainViewModel : BindableBase
    {
        public ObservableCollection<String> SlicesFilesNames { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> SlicesFilesSelected { get; set; } = new ObservableCollection<string>();

        private string _sourceFilePath;
        public string SourceFilePath
        {
            get { return _sourceFilePath; }
            set
            {
                SetProperty(ref _sourceFilePath, value);
                string[] pathArray = value.Split('.');
                _sourceFileExtention = pathArray[pathArray.Length - 1];
            }
        }

        private string _sourceFileExtention;


        private int _sliceFileSize = 100;
        public int SliceFileSize
        {
            get { return _sliceFileSize; }
            set { SetProperty(ref _sliceFileSize, value); }
        }

        private bool _mergeFinish = false;
        public bool MergeFinish
        {
            get { return _mergeFinish; }
            set { SetProperty(ref _mergeFinish, value); }
        }


        public DelegateCommand SelectFileCommand { get; set; }
        public DelegateCommand SliceCommand { get; set; }
        public DelegateCommand SelectSlicesCommand { get; set; }
        public DelegateCommand MergeCommand { get; set; }

        public MainViewModel()
        {
            SelectFileCommand = new DelegateCommand(SelectSourceFile);
            SliceCommand = new DelegateCommand(slice);
            SelectSlicesCommand = new DelegateCommand(SelectSliceFiles);
            MergeCommand = new DelegateCommand(merge);
        }

        private void SelectSourceFile()
        {
            //Debug.WriteLine("start select source file");
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SourceFilePath = dialog.FileName;
            }
        }

        private void SelectSliceFiles()
        {
            SlicesFilesSelected.Clear();
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SlicesFilesSelected.AddRange(dialog.FileNames);
            }
            MergeFinish = false;
        }

        private void slice()
        {
            SlicesFilesNames.Clear();
            FileInfo fileInfo = new FileInfo(SourceFilePath);
            long fileSize = fileInfo.Length;
            //Debug.WriteLine(fileSize);
            //Debug.WriteLine(_sourceFileExtention);
            string foler = fileInfo.DirectoryName;
            string fileName = fileInfo.Name;
            Debug.WriteLine(foler);

            using (FileStream fsRead = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read))
            {
                int end = 0;
                while (true)
                {
                    byte[] buffer = new byte[SliceFileSize * 1024];
                    int n = fsRead.Read(buffer, 0, buffer.Length);
                    Debug.WriteLine("n=" + n);
                    if (n == 0)
                    {
                        break;
                    }
                    string outputFilePath = Path.Combine(foler, "_" + end + "." + _sourceFileExtention);
                    SlicesFilesNames.Add(outputFilePath);
                    Debug.WriteLine("output file path=" + outputFilePath);
                    File.WriteAllBytes(outputFilePath, buffer.Take(n).ToArray());
                    end++;
                    Debug.WriteLine("loop=" + end);
                }

            }
        }

        private void merge()
        {

            string folder = new FileInfo(SlicesFilesSelected[0]).DirectoryName;
            string[] pathArray = new FileInfo(SlicesFilesSelected[0]).FullName.Split('.');
            string fileExten = pathArray[pathArray.Length - 1];
            string mergedFilePath = Path.Combine(folder, "Merged_" + DateTime.Now.ToFileTime() + "." + fileExten);

            using (FileStream fsWrite = new FileStream(mergedFilePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                foreach (string filePath in SlicesFilesSelected)
                {
                    byte[] file = File.ReadAllBytes(filePath);
                    fsWrite.Write(file,0,file.Length);
                }
                
            }



            MergeFinish = true;
        }
    }
}
