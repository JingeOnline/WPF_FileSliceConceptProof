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
using System.Windows.Forms;

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


        private long _sliceFileSize = 1024 * 1024;
        public long SliceFileSize
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
            SliceCommand = new DelegateCommand(slice2);
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

        //分隔或切片
        //使用流的方式读取，然后一次性写入。
        private void slice()
        {
            SlicesFilesNames.Clear();
            FileInfo fileInfo = new FileInfo(SourceFilePath);
            long fileSize = fileInfo.Length;
            //Debug.WriteLine(fileSize);
            //Debug.WriteLine(_sourceFileExtention);
            string foler = fileInfo.DirectoryName;
            string fileName = fileInfo.Name;
            //Debug.WriteLine(foler);

            using (FileStream fsRead = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read))
            {
                int index = 1;
                while (true)
                {
                    //byte[] buffer = new byte[SliceFileSize * 1024];
                    byte[] buffer = new byte[1024 * 1024];
                    int n = fsRead.Read(buffer, 0, buffer.Length);
                    //Debug.WriteLine("n=" + n);
                    if (n == 0)
                    {
                        break;
                    }
                    string outputFilePath = Path.Combine(foler, fileName + "_" + index + "." + _sourceFileExtention);
                    SlicesFilesNames.Add(outputFilePath);
                    //Debug.WriteLine("output file path=" + outputFilePath);
                    File.WriteAllBytes(outputFilePath, buffer.Take(n).ToArray());
                    index++;
                    //Debug.WriteLine("loop=" + end);
                }

            }
        }

        //使用流的方式读取和写入
        private void slice2()
        {
            SlicesFilesNames.Clear();
            FileInfo fileInfo = new FileInfo(SourceFilePath);
            string foler = fileInfo.DirectoryName;
            string fileName = fileInfo.Name;
            long sourceFileSize = fileInfo.Length;
            //long currentFileSize = 0;
            long bufferSize = 1024*1024;
            int fileIndex = 0;
            bool isLastSlice = false;
            while (true)
            {
                string sliceFilePath = Path.Combine(foler, fileName + "_" + fileIndex + "." + _sourceFileExtention);
                long startPosition = fileIndex * SliceFileSize;
                isLastSlice = (fileIndex + 1) * SliceFileSize >= sourceFileSize;
                readAndWriteOneSlice(sliceFilePath, bufferSize, startPosition, isLastSlice);
                fileIndex++;
                //currentFileSize += SliceFileSize;
                if (isLastSlice)
                {
                    break;
                }
            }
        }

        private void readAndWriteOneSlice(string sliceFilePath, long bufferSize, long startPosition, bool isLastSlice)
        {
            long left = SliceFileSize;

            using (FileStream fsRead = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read))
            {
                fsRead.Seek(startPosition, SeekOrigin.Begin);
                if (isLastSlice)
                {
                    left = fsRead.Length;
                    Debug.WriteLine("left=" + left);
                }
                using (FileStream fsWrite = new FileStream(sliceFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    while (true)
                    {
                        byte[] buffer = new byte[bufferSize];
                        if (left > bufferSize)
                        {
                            int n = fsRead.Read(buffer, 0, buffer.Length);
                            fsWrite.Write(buffer, 0, n);
                            left -= bufferSize;
                        }
                        else
                        {
                            int n = fsRead.Read(buffer, 0, Convert.ToInt32(left));
                            fsWrite.Write(buffer, 0, n);
                            break;
                        }
                    }
                }
            }
        }

        //合并
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
                    fsWrite.Write(file, 0, file.Length);
                }

            }



            MergeFinish = true;
        }
    }
}
