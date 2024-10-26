using BookLibrary.Storage.Models.Book;
using BookLibrary.Storage.Repositories;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace BookLibrary.UI.ViewModels
{
    public class BookTrackViewModel : INotifyPropertyChanged
    {
        private readonly IBooksRepository booksRepository = new BooksRepository();
        private const int displayBookTrackItemsDefaultCount = 10;
        private string _filter = displayBookTrackItemsDefaultCount.ToString();
        private Visibility _filterVisibility = Visibility.Hidden;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public BookTrackViewModel() { }

        public BookTrackViewModel(BookTrackList book)
        {
            Book = book;
        }

        private ICollectionView bookTracksView { get; set; }

        private BookTrackList _book;

        public BookTrackList Book
        {
            get
            {
                return _book;
            }
            set
            {
                _book = value;
                FilterVisibility = _book.TracksList.Count() >= displayBookTrackItemsDefaultCount ? Visibility.Visible : Visibility.Hidden;
                bookTracksView = CollectionViewSource.GetDefaultView(_book.TracksList);
                bookTracksView.Refresh();
                OnPropertyChanged("Book");
            }
        }
        public string Filter
        {
            get
            {
                return _filter;
            }
            set
            {
                _filter = value;
                OnPropertyChanged("Filter");
            }
        }

        public Visibility FilterVisibility
        {
            get
            {
                return _filterVisibility;
            }
            set
            {
                _filterVisibility = value;
                OnPropertyChanged("FilterVisibility");
            }
        }

        public ICollectionView BookTracks
        {
            get { return bookTracksView; }
        }

        public async Task LoadBookTrack()
        {
            Book = int.TryParse(_filter, out int filter) ? await booksRepository.GetBookTrack(AppUser.GetInstance().AccountId, _book.BookId, filter) :
                await booksRepository.GetBookTrack(AppUser.GetInstance().AccountId, _book.BookId);
        }

        public async Task PutBook()
        {
            await booksRepository.PutBook(AppUser.GetInstance().AccountId, _book.BookId);
            Book.IsBookAvailable = true;
        }

        public async Task TakeBook()
        {
            await booksRepository.TakeBook(AppUser.GetInstance().AccountId, _book.BookId);
            Book.IsBookAvailable = false;
        }
    }
}
