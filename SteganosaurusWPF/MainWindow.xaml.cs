using System;
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

        // Browse picture in extraction tab
        private void BrowsePictureButtonOnClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Picture"; // Default file name
            dlg.DefaultExt = ".jpg"; // Default file extension
            dlg.Filter = "JPG Image (.jpg)|*.jpg"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                selectPictureTextBox.Text = filename;
            }
        }

        private void BrowseMessageButtonOnClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "File"; // Default file name

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                selectMessageTextBox.Text = filename;
            }
        }

        private void ProcessInsertionButtonOnClick(object sender, RoutedEventArgs e)
        {
            string filePicturePath = selectPictureTextBox.Text;
            string fileMessagePath = selectMessageTextBox.Text;
            string key = encryptionKeyTextBox.Text;
            if (!File.Exists(filePicturePath))
            {
                string messageBoxText = "Your path for picture file is incorrect, please make sure the file exist";
                string caption = "File Path Error";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBox.Show(messageBoxText, caption, button, icon);
            }
            else if (!File.Exists(fileMessagePath))
            {
                string messageBoxText = "Your path for message file is incorrect, please make sure the file exist";
                string caption = "File Path Error";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBox.Show(messageBoxText, caption, button, icon);
            }
            else
            {
                FileInfo fileInfoPicture = new FileInfo(filePicturePath);
                FileInfo fileInfoMessage = new FileInfo(fileMessagePath);
                Console.WriteLine(fileInfoPicture.Length);
                if (fileInfoPicture.Length > maxFileSize || fileInfoMessage.Length > maxFileSize)
                {
                    string messageBoxText = "Your file is too big, please select another file";
                    string caption = "File Size Error";
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Error;
                    MessageBox.Show(messageBoxText, caption, button, icon);
                }
                else
                {
                    Byte[] byteImage = File.ReadAllBytes(filePicturePath);
                    Byte[] byteMessage = File.ReadAllBytes(fileMessagePath);
                    Image icon = new Image();


                    switch (algorithmComboBox.SelectedIndex)
                    {
                        case 0:
                            Steganography.InsertionWithAlgorithmStandard(byteImage, byteMessage, key);
                            break;
                        case 1:
                            Steganography.InsertionWithAlgorithmLiao(byteImage, byteMessage, key);
                            break;
                        case 2:
                            Steganography.InsertionWithAlgorithmSwain(byteImage, byteMessage, key);
                            break;
                        default:
                            break;
                    }

                    BitmapImage bmImage = new BitmapImage();
                    bmImage.BeginInit();
                    bmImage.UriSource = new Uri(filePicturePath, UriKind.Absolute);
                    bmImage.EndInit();
                    imageBefore.Source = bmImage;

                }
            }
        }

        
    }
}
