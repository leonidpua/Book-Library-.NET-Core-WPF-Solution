using BookLibrary.Storage.Models.Book;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookLibrary.Storage.Repositories
{
    public interface IBooksRepository
    {
        Task AddBook(Book book);
        Task DeleteBook(Guid bookId);
        Task DoBookAction(BookAction action, Guid accountId, Guid bookId);
        Task<List<Book>> GetAvailableBooks(string searchString = "", int from = 0, int count = 10);
        Task<int> GetAvailableBooksTotalCount(string searchString = "");
        Task<Book> GetBook(Guid bookId);
        Task<List<Book>> GetBooks(string searchString = "", bool onlyAvailable = false, Guid? userId = null, int from = 0, int count = 10);
        Task<List<Book>> GetBooksByUser(Guid userId, string searchString = "", int from = 0, int count = 10);
        Task<int> GetBooksByUserTotalCount(Guid userId, string searchString = "");
        Task<int> GetBooksTotalCount(string searchString = "");
        Task<BookTrackList> GetBookTrack(Guid accountId, Guid bookId, int tracksCount = 0);
        Task PutBook(Guid accountId, Guid bookId);
        Task TakeBook(Guid accountId, Guid bookId);
        Task UpdateBook(Book book);
    }
}
