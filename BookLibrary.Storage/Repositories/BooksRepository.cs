using BookLibrary.Storage.Contexts;
using BookLibrary.Storage.Models.Book;
using BookLibrary.Storage.Models.Records.Book;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookLibrary.Storage.Repositories
{
    public class BooksRepository : IBooksRepository
    {
        public Task<Book> GetBook(Guid bookId)
        {
            using var dbContext = new BookLibraryContext();
            var booksQuery = dbContext.Books.Where(book => book.Id == bookId);
            var books = SelectBooksFromBookRecords(dbContext, booksQuery);
            return Task.FromResult(books.FirstOrDefault());
        }

        public Task<List<Book>> GetBooks(string searchString = "", bool onlyAvailable = false, Guid? userId = null, int from = 0, int count = 10)
        {
            using var dbContext = new BookLibraryContext();
            return Task.FromResult(BuildGetBooksQuery(dbContext, searchString, onlyAvailable, userId, from, count).ToList());
        }

        public Task<List<Book>> GetAvailableBooks(string searchString = "", int from = 0, int count = 10)
        {
            return GetBooks(searchString, true, null, from, count);
        }

        public Task<List<Book>> GetBooksByUser(Guid userId, string searchString = "", int from = 0, int count = 10)
        {
            return GetBooks(searchString, false, userId, from, count);
        }

        public Task<int> GetBooksTotalCount(string searchString = "")
        {
            using var dbContext = new BookLibraryContext();
            return Task.FromResult(BuildGetBooksQuery(dbContext, searchString).Count());
        }

        public Task<int> GetBooksByUserTotalCount(Guid userId, string searchString = "")
        {
            using var dbContext = new BookLibraryContext();
            return Task.FromResult(BuildGetBooksQuery(dbContext, searchString, false, userId).Count());
        }

        public Task<int> GetAvailableBooksTotalCount(string searchString = "")
        {
            using var dbContext = new BookLibraryContext();
            return Task.FromResult(BuildGetBooksQuery(dbContext, searchString, true).Count());
        }


        public Task DeleteBook(Guid bookId)
        {
            using var dbContext = new BookLibraryContext();
            var bookRecord = dbContext.Books.FirstOrDefault(book => book.Id == bookId);
            if (bookRecord != null)
            {
                dbContext.Books.Remove(bookRecord);
                dbContext.SaveChanges();
            }

            return Task.CompletedTask;
        }

        public Task AddBook(Book book)
        {
            using var dbContext = new BookLibraryContext();
            using var transaction = dbContext.Database.BeginTransaction();

            var bookRecord = BookRecord.FromDomain(book);
            dbContext.Books.Add(bookRecord);
            dbContext.SaveChanges();

            SaveBookAuthors(dbContext, book.Authors, bookRecord.Id);

            transaction.Commit();

            return Task.CompletedTask;
        }

        public Task UpdateBook(Book book)
        {
            using var dbContext = new BookLibraryContext();
            using var transaction = dbContext.Database.BeginTransaction();

            var bookRecord = dbContext.Books.FirstOrDefault(record => record.Id == book.Id);
            if (bookRecord is null)
            {
                AddBook(book);
                return Task.CompletedTask;
            }

            bookRecord.Name = book.Name;
            bookRecord.Year = book.Year;
            bookRecord.IsAvailable = book.IsAvailable ?? true;
            dbContext.BooksAuthors.RemoveRange(dbContext.BooksAuthors.Where(record => record.BookId == book.Id));
            dbContext.SaveChanges();

            SaveBookAuthors(dbContext, book.Authors, bookRecord.Id);

            transaction.Commit();

            return Task.CompletedTask;
        }

        public Task<BookTrackList> GetBookTrack(Guid accountId, Guid bookId, int tracksCount = 0)
        {
            var result = new BookTrackList();
            using var dbContext = new BookLibraryContext();
            var bookRecord = dbContext.Books.FirstOrDefault(record => record.Id == bookId);

            result.BookId = bookRecord?.Id ?? Guid.NewGuid();
            result.BookName = bookRecord?.Name;
            result.IsBookAvailable = bookRecord?.IsAvailable;

            var bookTookTracks = dbContext.BookTracking.Where(record => record.BookId == bookId && record.Action == BookAction.Took.ToString());
            var lastBookTookTrack = bookTookTracks.FirstOrDefault(record => record.ActionTime == bookTookTracks.Max(track => track.ActionTime));
            result.CanBePutted = accountId == lastBookTookTrack?.AccountId;

            var accountRecord = dbContext.Accounts.FirstOrDefault(record => record.Id == accountId);
            if (accountRecord != null)
            {
                var profileRecord = dbContext.Profiles.FirstOrDefault(record => record.Id == accountRecord.ProfileId);
                if (profileRecord != null)
                {
                    var tracksQuery = dbContext.BookTracking.Where(record => record.BookId == bookId).OrderByDescending(record => record.ActionTime);
                    if (tracksCount > 0)
                    {
                        tracksQuery = tracksQuery.Take(tracksCount).OrderByDescending(record => record.ActionTime);
                    }

                    var tracksQueryJoinAccounts = tracksQuery.Join(
                        dbContext.Accounts,
                        track => track.AccountId,
                        account => account.Id,
                        (track, account) => new
                        {
                            account.Login,
                            account.ProfileId,
                            track.ActionTime,
                            track.Action
                        }
                        ).OrderByDescending(record => record.ActionTime);

                    var tracksQueryJoinAccountsJoinProfiles = tracksQueryJoinAccounts.Join(
                        dbContext.Profiles,
                        track => track.ProfileId,
                        profile => profile.Id,
                        (track, profile) => new
                        {
                            track.Login,
                            profile.Email,
                            track.ActionTime,
                            track.Action
                        }
                        ).OrderByDescending(record => record.ActionTime);

                    result.TracksList = tracksQueryJoinAccountsJoinProfiles.Select(bookTrack => BookTrack.FromPersistence(
                        bookId,
                        bookRecord.Name,
                        bookTrack.Login,
                        bookTrack.Email,
                        bookTrack.ActionTime,
                        bookTrack.Action
                    )).ToList();
                }
            }

            return Task.FromResult(result);
        }

        public Task DoBookAction(BookAction action, Guid accountId, Guid bookId)
        {
            using var dbContext = new BookLibraryContext();
            using var transaction = dbContext.Database.BeginTransaction();

            var accountRecord = dbContext.Accounts.FirstOrDefault(record => record.Id == accountId);
            if (accountRecord != null)
            {
                var bookRecord = dbContext.Books.FirstOrDefault(record => record.Id == bookId);
                if (bookRecord != null)
                {
                    switch (action)
                    {
                        case BookAction.Took:
                            bookRecord.IsAvailable = false;
                            break;
                        case BookAction.Put:
                            bookRecord.IsAvailable = true;
                            break;
                        default:
                            throw new NotSupportedException($"Action {action} is not supported");
                    }

                    var bookTrackingRecord = new BookTrackingRecord
                    {
                        AccountId = accountId,
                        Action = action.ToString(),
                        ActionTime = DateTime.UtcNow,
                        BookId = bookId
                    };
                    dbContext.BookTracking.Add(bookTrackingRecord);

                    dbContext.SaveChanges();
                    transaction.Commit();
                }
            }
            return Task.CompletedTask;
        }

        public Task TakeBook(Guid accountId, Guid bookId)
        {
            return DoBookAction(BookAction.Took, accountId, bookId);
        }

        public Task PutBook(Guid accountId, Guid bookId)
        {
            return DoBookAction(BookAction.Put, accountId, bookId);
        }

        #region Private

        private IQueryable<Book> BuildGetBooksQuery(BookLibraryContext dbContext, string searchString = "", bool onlyAvailable = false, Guid? userId = null, int from = 0, int count = 10)
        {
            IQueryable<BookRecord> booksQuery = dbContext.Books;
            if (userId is not null)
            {
                var bookTracks = dbContext.BookTracking
                    .Where(bookTrack => bookTrack.AccountId == userId && bookTrack.Action == BookAction.Took.ToString())
                    .GroupBy(bookTrack => bookTrack.BookId).Select(bookTrackGroup => new
                    {
                        BookId = bookTrackGroup.Key,
                        ActionTime = bookTrackGroup.Max(bookTrack => bookTrack.ActionTime)
                    }).OrderBy(bookTrack => bookTrack.ActionTime);
                booksQuery = booksQuery.Where(book => !book.IsAvailable).Join(bookTracks, book => book.Id, bookTrack => bookTrack.BookId, (book, bookTrack) => book);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                var bookIdsByAuthor = dbContext.Authors.Where(author => author.Name.Contains(searchString)).Join(
                        dbContext.BooksAuthors,
                        author => author.Id,
                        bookAuthor => bookAuthor.AuthorId,
                        (author, bookAuthor) => bookAuthor)
                    .Select(bookAuthor => bookAuthor.BookId);
                booksQuery = booksQuery.Where(book => book.Name.Contains(searchString) || book.Year.ToString().Contains(searchString) || bookIdsByAuthor.Contains(book.Id));
            }

            if (onlyAvailable)
            {
                booksQuery = booksQuery.Where(book => book.IsAvailable);
            }

            booksQuery = booksQuery.Skip(from).Take(count);
            var books = SelectBooksFromBookRecords(dbContext, booksQuery);

            return books;
        }

        private IQueryable<Book> SelectBooksFromBookRecords(BookLibraryContext dbContext, IQueryable<BookRecord> bookRecords)
        {
            var books = bookRecords.Select(book => Book.FromPersistence(
                book.Id,
                book.Name,
                dbContext.Authors.Join(
                    dbContext.BooksAuthors.Where(bookAuthor => bookAuthor.BookId == book.Id),
                    author => author.Id,
                    bookAuthor => bookAuthor.AuthorId,
                    (author, bookAuthor) => author)
                .Select(authorRecord => authorRecord.Name).ToList(),
                book.Year,
                book.IsAvailable
            ));
            return books;
        }

        private void SaveBookAuthors(BookLibraryContext dbContext, IEnumerable<string> authors, Guid bookId)
        {
            if (authors is not null)
            {
                foreach (var author in authors)
                {
                    var authorRecord = dbContext.Authors.FirstOrDefault(record => record.Name == author);
                    if (authorRecord is null)
                    {
                        dbContext.Authors.Add(new AuthorRecord { Name = author });
                        dbContext.SaveChanges();
                        authorRecord = dbContext.Authors.FirstOrDefault(record => record.Name == author);
                    }

                    var booksAuthorsRecord = dbContext.BooksAuthors.FirstOrDefault(record => record.AuthorId == authorRecord.Id && record.BookId == bookId);
                    if (booksAuthorsRecord is null)
                    {
                        dbContext.BooksAuthors.Add(new BookAuthorRecord { AuthorId = authorRecord.Id, BookId = bookId });
                        dbContext.SaveChanges();
                    }
                }
            }
        }

        #endregion
    }

}
