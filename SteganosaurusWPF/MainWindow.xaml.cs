﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SteganosaurusWPF
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int maxFileSize = 8 * 1024 * 1024;
        
        public List<string> data;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        #region UI Helpers
        private string ShowOpenFileDialog()
        {
            return ShowOpenFileDialog("", "All file (*.*)|*.*");
        }
        
        private string ShowOpenFileDialog(string defaultExt, string filter)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "";
            dlg.DefaultExt = defaultExt;
            dlg.Filter = filter;

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                return dlg.FileName;
            }
            else
                return null;
        }

        private string ShowSaveFileDialog(string fileName, string defaultExt, string filter)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = fileName;
            dlg.DefaultExt = defaultExt;
            dlg.Filter = filter;

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                return dlg.FileName;
            }
            else
            {
                return null;
            }
        }

        private void ShowError(string title, string desciption)
        {
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Error;
            MessageBox.Show(title, desciption, button, icon);
        }

        #endregion

        private void BrowsePictureButtonOnClick(object sender, RoutedEventArgs e)
        {
            string fileName = ShowOpenFileDialog(".bmp", "Bitmap Image (.bmp)|*.bmp");
            if (fileName != null)
                selectPictureTextBox.Text = fileName;
        }

        private void BrowseMessageButtonOnClick(object sender, RoutedEventArgs e)
        {
            string fileName = ShowOpenFileDialog();
            if (fileName != null)
                selectMessageTextBox.Text = fileName;
        }

        private void ProcessInsertionButtonOnClick(object sender, RoutedEventArgs e)
        {
            string filePicturePath = selectPictureTextBox.Text;
            string fileMessagePath = selectMessageTextBox.Text;
            string key = encryptionKeyTextBox.Text;

            if (!File.Exists(filePicturePath))
                ShowError("File Path Error", "Your path for picture file is incorrect, please make sure the file exist");
            else if (!File.Exists(fileMessagePath))
                ShowError("File Path Error", "Your path for message file is incorrect, please make sure the file exist");
            else
            {
                FileInfo fileInfoPicture = new FileInfo(filePicturePath);
                FileInfo fileInfoMessage = new FileInfo(fileMessagePath);

                if (fileInfoPicture.Length > maxFileSize || fileInfoMessage.Length > maxFileSize)
                    ShowError("File Limit Error", "Your file is too big, please select another file");
                else
                {
                    BitmapSource bitmapSourceBefore = new BitmapImage(new Uri(filePicturePath));
                    BitmapSource bitmapSourceAfter = null;

                    switch (algorithmComboBox.SelectedIndex)
                    {
                        case 0:
                            if (Steganography.canDoInsertionWithAlgorithmStandard(filePicturePath, fileMessagePath))
                                bitmapSourceAfter = Steganography.InsertionWithAlgorithmStandard(filePicturePath, fileMessagePath, key);
                            else
                                ShowError("File Limit Error", "Your message file can't fit into the picture");
                            break;
                        case 1:
                            Steganography.InsertionWithAlgorithmLiao(filePicturePath, fileMessagePath, key);
                            break;
                        case 2:
                            Steganography.InsertionWithAlgorithmSwain(filePicturePath, fileMessagePath, key);
                            break;
                        default:
                                ShowError("Form Error", "Please select the algoritm");
                            break;

                    }
                    imageBefore.Source = bitmapSourceBefore;
                    imageAfter.Source = bitmapSourceAfter;

                    string fileName = ShowSaveFileDialog(System.IO.Path.GetFileNameWithoutExtension(filePicturePath) + "-saurus", ".bmp", "Bitmap Image (.bmp)|*.bmp");
                    if (fileName != null)
                    {
                        using (var fileStream = new FileStream(fileName, FileMode.Create))
                        {
                            BitmapEncoder encoder = new BmpBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmapSourceAfter));
                            encoder.Save(fileStream);
                        }
                    }

                }
            }
        }

        private void BrowsePictureExtractOnClick(object sender, RoutedEventArgs e)
        {
            string fileName = ShowOpenFileDialog(".bmp", "Bitmap Image (.bmp)|*.bmp");
            if (fileName != null)
                selectPictureExtractTextBox.Text = fileName;
        }

        private void ProcessExtrationButtonOnClick(object sender, RoutedEventArgs e)
        {
            string filePicturePath = selectPictureExtractTextBox.Text;
            string key = encryptionKeyExtractTextBox.Text;

            if (!File.Exists(filePicturePath))
                ShowError("File Path Error", "Your path for picture file is incorrect, please make sure the file exist");
            else
            {
                FileTemp fileTemp = null;
                switch (algorithmComboBox.SelectedIndex)
                {
                    case 0:
                        Console.WriteLine("check 1");
                        fileTemp = Steganography.ExtractionWithAlgorithmStandard(filePicturePath, key);
                        Console.WriteLine("check 2");
                        break;
                    case 1:
                        //Steganography.InsertionWithAlgorithmLiao(filePicturePath, fileMessagePath, key);
                        break;
                    case 2:
                        //Steganography.InsertionWithAlgorithmSwain(filePicturePath, fileMessagePath, key);
                        break;
                    default:
                        ShowError("Form Error", "Please select the algoritm");
                        break;

                }
                string fileName = ShowSaveFileDialog(fileTemp.Name, "", "All file|*.*");
                if (fileName != null)
                {
                    File.WriteAllBytes(fileName, fileTemp.Data);
                }
            }
        }

        #region Temporary Function/Procedure

        private void PRNG(int seed_, int max_)
        {
            // Manually input the Seed, or you can make it random like my code above.
            int seed = Convert.ToInt32(seed_);

            String display = "";
            int min = 1;
            // Max value is manually input, for how many number will be generated.
            // i need to plus by 1 for the max value because i state the min value is 1.
            int max = Convert.ToInt32(max_) + 1;

            Random rand = new Random(seed);

            int number;
            // this dictionary is for saving the number generated by random, if exist, 
            //do random again.
            Dictionary<int, int> num = new Dictionary<int, int>();

            for (int i = 1; i < max; i++)
            {

                number = rand.Next(min, max);
                if (num.ContainsKey(number))
                {
                    while (true)
                    {
                        number = rand.Next(min, max);
                        if (num.ContainsKey(number))
                        {
                            // if exist do nothing and then random again while true  
                        }
                        else
                        {
                            num.Add(number, 1);
                            break;
                        }

                    }
                }
                else
                {
                    num.Add(number, 1);
                }

                display += "\t" + number;

            }
            // display the random number.
            //rtOutput.Text = display;
        }
        #endregion
    }
}
