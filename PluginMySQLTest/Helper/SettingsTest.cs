using System;
using System.Collections.Generic;
using PluginMySQL.Helper;
using Xunit;

namespace PluginMySQLTest.Helper
{
    public class SettingsTest
    {
        [Fact]
        public void ValidateValidTest()
        {
            // setup
            var settings = new Settings
            {
                Hostname = "123.456.789.0",
                Port = "3306",
                Database = "testdb",
                Username = "username",
                Password = "password"
            };

            // act
            settings.Validate();

            // assert
        }

        [Fact]
        public void ValidateNoHostNameTest()
        {
            // setup
            var settings = new Settings
            {
                Hostname = null,
                Database = "testdb",
                Username = "username",
                Password = "password"
            };

            // act
            Exception e = Assert.Throws<Exception>(() => settings.Validate());

            // assert
            Assert.Contains("The Hostname property must be set", e.Message);
        }
        
        [Fact]
        public void ValidateNoDatabaseTest()
        {
            // setup
            var settings = new Settings
            {
                Hostname = "123.456.789.0",
                Database = null,
                Username = "username",
                Password = "password"
            };

            // act
            Exception e = Assert.Throws<Exception>(() => settings.Validate());

            // assert
            Assert.Contains("The Database property must be set", e.Message);
        }
        
        [Fact]
        public void ValidateNoUsernameTest()
        {
            // setup
            var settings = new Settings
            {
                Hostname = "123.456.789.0",
                Database = "testdb",
                Username = null,
                Password = "password"
            };

            // act
            Exception e = Assert.Throws<Exception>(() => settings.Validate());

            // assert
            Assert.Contains("The Username property must be set", e.Message);
        }
        
        [Fact]
        public void ValidateNoPasswordTest()
        {
            // setup
            var settings = new Settings
            {
                Hostname = "123.456.789.0",
                Database = "testdb",
                Username = "username",
                Password = null
            };

            // act
            Exception e = Assert.Throws<Exception>(() => settings.Validate());

            // assert
            Assert.Contains("The Password property must be set", e.Message);
        }
        
        [Fact]
        public void GetConnectionStringTest()
        {
            // setup
            var settings = new Settings
            {
                Hostname = "123.456.789.0",
                Port = "3306",
                Database = "testdb",
                Username = "username",
                Password = "password"
            };

            // act
            var connString = settings.GetConnectionString();
            var connDbString = settings.GetConnectionString("otherdb");

            // assert
            Assert.Equal("Server=123.456.789.0; Port=3306; Database=testdb; User=username; Password=password;", connString);
            Assert.Equal("Server=123.456.789.0; Port=3306; Database=otherdb; User=username; Password=password;", connDbString);
        }
    }
}