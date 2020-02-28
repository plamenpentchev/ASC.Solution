using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorageTest
{
    class Program
    {
        static  void Main(string[] args)
        {
            string partition = "APress";
            string row = "2";

            TestAddWithUnitOfWork(partition, row);
            //TestFindAndUpdateWithUnitOfWork(partition, row, "Rami Vemula");
            //TestDeleteWithUnitOfWork(partition, row);
            Console.ReadLine();

        }

        static async void TestDeleteWithUnitOfWork(string partition, string row)
        {
            using (var _unitOfWork = new UnitOfWork("UseDevelopmentStorage=true"))
            {
                var rep = _unitOfWork.Repository<BookEntity>();
                await rep.CreateTableAsync();

                var data = await rep.FindAsync(partition, row);
                if(null != data)
                    await rep.DeleteAsync(data);

                _unitOfWork.CommitTransactions();
            }
        }

        static async void TestFindAndUpdateWithUnitOfWork(string partition, string row, string author)
        {
            using (var _unitOfWork = new UnitOfWork("UseDevelopmentStorage=true"))
            {
                var rep = _unitOfWork.Repository<BookEntity>();
                await rep.CreateTableAsync();

                var data = await rep.FindAsync(partition, row);
                if (null != data)
                {
                    Console.WriteLine(data);

                    data.Author = author;

                    var updateData = await rep.UpdateAsync(data);
                    Console.WriteLine(updateData);
                }
                
                _unitOfWork.CommitTransactions();
            }
        }

        static async void TestAddWithUnitOfWork(string partition, string row)
        {
            using (var _unitOfWork = new UnitOfWork("UseDevelopmentStorage=true"))
            {
                var bookRepository = _unitOfWork.Repository<BookEntity>();
                await bookRepository.CreateTableAsync();

                BookEntity book = new BookEntity()
                {
                    BookId = int.Parse(row),
                    Author = "Rami",
                    BookName = "ASP.NET Core with Azure",
                    Publisher = partition

                };
                book.RowKey = book.BookId.ToString();
                book.PartitionKey = book.Publisher;
                var data = await bookRepository.FindAsync(book.PartitionKey, book.RowKey);
                if(null == data)
                {
                     data = await bookRepository.AddAsync(book);
                    Console.WriteLine(data);
                }

                _unitOfWork.CommitTransactions();
            }
        }


        static void TestRun1()
        {
            //Azure Storage account and Table Service Instances
            CloudStorageAccount storageAccount;
            CloudTableClient tableClient;

            //connect to storage account
            storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");

            //create the table 'Book' if it not exists
            tableClient = storageAccount.CreateCloudTableClient();
            CloudTable cloudTable = tableClient.GetTableReference("Book");
            cloudTable.CreateIfNotExistsAsync();

            BookEntity aNewBook = new BookEntity() { Author = "Rami", BookName = "ASP.NET Core with Azure", Publisher = "APress" };
            aNewBook.BookId = 3;
            aNewBook.RowKey = aNewBook.BookId.ToString();
            aNewBook.PartitionKey = aNewBook.Publisher;
            aNewBook.CreatedDate = DateTime.UtcNow;
            aNewBook.UpdatedDate = DateTime.UtcNow;

            //insert and execute operations
            TableOperation insertOperation = TableOperation.Insert(aNewBook);
            cloudTable.ExecuteAsync(insertOperation);

        }
    }
}
