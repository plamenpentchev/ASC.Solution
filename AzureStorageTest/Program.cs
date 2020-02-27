using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorageTest
{
    class Program
    {
        static void Main(string[] args)
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
            aNewBook.BookId = 2;
            aNewBook.RowKey = aNewBook.BookId.ToString();
            aNewBook.PartitionKey = aNewBook.Publisher;
            aNewBook.CreatedDate = DateTime.UtcNow;
            aNewBook.UpdatedDate = DateTime.UtcNow;

            //insert and execute operations
            TableOperation insertOperation = TableOperation.Insert(aNewBook);
            cloudTable.ExecuteAsync(insertOperation);

            Console.ReadLine();

        }
    }
}
