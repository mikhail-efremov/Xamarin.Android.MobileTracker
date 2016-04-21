using System;
using System.IO;
using SQLite;

namespace Xamarin.Android.MobileTracker.ActivityData
{
    public class Person
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int Process()
        {
            var person = new Person
            {
                Id = 1,
                FirstName = "NoName",
                LastName = "NoLastName"
            };

            var dbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Personal),
        "database.db3");
            
            var db = new SQLiteConnection(dbPath);
            
            db.CreateTable<Person>();
            db.Insert(person); // after creating the newStock object

            var stock = db.Get<Person>(1); // primary key id of 5
            var stockList = db.Table<Person>();

            return 1;
        }

        public override string ToString()
        {
            return $"[Person: ID={Id}, FirstName={FirstName}, LastName={LastName}]";
        }
    }
}