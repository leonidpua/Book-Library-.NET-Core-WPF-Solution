﻿using Book_Library_Repository_EF_Core.Models.Book;
using Book_Library_Repository_EF_Core.Repositories;
using Book_Library_Repository_EF_Core.Servicies;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Book_Library_.NET_Core_WPF_App.Pages
{
    /// <summary>
    /// Interaction logic for EditBookPage.xaml
    /// </summary>
    public partial class EditBookPage : BookLibraryPage
    {
        private IDataStore DataStore => RepositoryService.Get<BookLibraryRepository>();

        private Page _previousPage;

        private BookItem _book;

        public EditBookPage(Page previousPage, BookItem book)
        {
            InitializeComponent();

            _previousPage = previousPage;

            btnBackward.Background = PagesPropertiesProvider.BackwardImage;

            _book = DataStore.Books.GetBook((int)book.ID);

            if (book == null || _book == null)
                NavigationService.Navigate(_previousPage);
            
            tbBookName.Text = _book.Name;
            tbBookAuthors.Text = _book.Authors;
            dpBookDate.SelectedDate = _book.Year;

            btnBackward.Click += btnBackward_Click;
            btnEditBook.Click += btnUpdateBook_Click;
        }

        private void btnBackward_Click(object sender, RoutedEventArgs e)
        {
            TryCatchMessageTask(() =>
            {
                NavigationService.Navigate(_previousPage);
            });
        }

        private void btnUpdateBook_Click(object sender, RoutedEventArgs e)
        {
            TryCatchMessageTask(() =>
            {
                if (tbBookName.Text != string.Empty && tbBookAuthors.Text != string.Empty && dpBookDate.SelectedDate != null)
                {
                    _book.Name = tbBookName.Text;
                    _book.Authors = tbBookAuthors.Text;
                    _book.Year = dpBookDate.SelectedDate.Value;
                    DataStore.Books.UpdateBook(_book);
                    NavigationService.Navigate(_previousPage);
                }
            });
        }
    }
}
