using System;
using System.Collections.Generic;
using System.Text;

namespace AzureStorageTest
{
    /// <summary>
    /// a book entity that will be used to perform
    /// sample data operations against the Storage emulator
    /// </summary>
    class BookEntity:BaseEntity
    {
        public BookEntity()
        {

        }
        public BookEntity(int bookId, string publisher)
        {
            this.RowKey = bookId.ToString();
            this.PartitionKey = publisher;
        }

        public int BookId { get; set; }
        public string BookName { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }

        public override string ToString()
            => $"[{PartitionKey}:{RowKey}] - {BookId.ToString()} : {BookName} by {Author}, Publ.:{Publisher}";
    }
}
