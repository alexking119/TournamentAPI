using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using TournamentAPI.Tests.DataStoreTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TournamentAPI.Tests.DataStoreTests
{
    [TestClass]
    public class DataStoreTest
    {
        [TestMethod]
        public void ListTemplates()
        {
            DataReaderMock idr = new DataReaderMock("id", "name", "className");

            idr.AddRecord(new List<object> { 1, "Example Template", "ExampleTemplateClass" });
            idr.AddRecord(new List<object> { 2, "Second Template", "SecondTemplateClass" });

            var sqlCM = new SqlConnectionMock(1, idr);
            DataStore ds = new DataStore(sqlCM);

            var templates = ds.ListTemplates();
            var template1 = templates[0];
            var template2 = templates[1];

            var expectedTemplate1 = new Template() {id = 1, name = "Example Template", className = "ExampleTemplateClass"};
            var expectedTemplate2 = new Template() { id = 2, name = "Second Template", className = "SecondTemplateClass" };

            Assert.AreEqual(2, templates.Length);

            Assert.AreEqual(expectedTemplate1.id, template1.id);
            Assert.AreEqual(expectedTemplate1.name, template1.name);
            Assert.AreEqual(expectedTemplate1.className, template1.className);

            Assert.AreEqual(expectedTemplate2.id, template2.id);
            Assert.AreEqual(expectedTemplate2.name, template2.name);
            Assert.AreEqual(expectedTemplate2.className, template2.className);
            ds.Dispose();

            Assert.AreEqual(ConnectionState.Closed, sqlCM.State, "Connection was not closed after completion!");
        }

        [TestMethod]
        public void CreateTournament()
        {
            DataReaderMock idr = new DataReaderMock("TournamentId", "name", "description", "startDate", "endDate", "organiser");

            var sqlCM = new SqlConnectionMock(1, idr);
            DataStore ds = new DataStore(sqlCM);

            Tournament newTournament = new Tournament() { id = 3, name = "Tournament", description = "Example description", startDate = new DateTime(2017, 8, 15, 9, 0, 0), endDate = new DateTime(2018, 8, 15, 9, 0, 0) };

            ds.CreateTournament(newTournament);
            ds.Dispose();
            Assert.AreEqual(ConnectionState.Closed, sqlCM.State, "Connection was not closed after completion!");
        }

        [TestMethod]
        public void GetTemplate()
        {
            DataReaderMock idr = new DataReaderMock("id", "name", "className");

            idr.AddRecord(new List<object>() {0, "Example Template", "ExampleTemplateClass"});

            SqlConnectionMock sqlCM = new SqlConnectionMock(1, idr);
            DataStore ds = new DataStore(sqlCM);

            var expectedTemplate = new Template() {id = 0, name = "Example Template", className = "ExampleTemplateClass"};
            var actualTemplate = ds.GetTemplate(0);

            Assert.AreEqual(expectedTemplate.id, actualTemplate.id);
            Assert.AreEqual(expectedTemplate.name, actualTemplate.name);
            Assert.AreEqual(expectedTemplate.className, actualTemplate.className);
            ds.Dispose();

            Assert.AreEqual(ConnectionState.Closed, sqlCM.State, "Connection was not closed after completion!");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ConnectionToDatabaseFails()
        {
            DataReaderMock idr = new DataReaderMock("id", "name", "className");
            DataStore ds = new DataStore(new SqlConnectionMock(1, idr, true));
            var templates = ds.ListTemplates();

            Assert.AreEqual(0, templates.Length);
        }
    }
}
