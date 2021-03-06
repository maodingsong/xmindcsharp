using NUnit.Framework;
using System.IO;
using Serilog;
using XMindAPI.Configuration;
using FluentAssertions;
using XMindAPI.Extensions;
using XMindAPI.Models;
namespace Tests
{
    [TestFixture]
    public class XMindSheetTest
    {
        private readonly bool _isCleanUpNeeded = false;
        private readonly string _xmindOutputFolderName = "xmind-output";
        [SetUp]
        public void Setup()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("sheet.test.log", retainedFileCountLimit: 3)
                .CreateLogger();
        }

        [Test]
        public void GetParent_DirectAncestor_Success()
        {
            //Arrange
            var book = new XMindConfiguration()
                .WithFileWriter(useDefaultPath: true, zip: false)
                .CreateWorkBook(workbookName: "test");
            //Act
            var sheet = book.CreateSheet();
            book.AddSheet(sheet);
            var parent = sheet.GetParent();
            //Arrange
            parent.Should().Be((XMindWorkBook)book, $"Sheet as direct ancestor");
        }

        [Test]
        public void SetTitle_DefaultFlow_Success()
        {
            //Arrange
            var book = new XMindConfiguration()
                .WithFileWriter(useDefaultPath: true, zip: true)
                .CreateWorkBook(workbookName: "test");
            string title = "Awesome sheet";
            //Act
            var sheet = book
                .CreateSheet();
            sheet.SetTitle(title);
            //Assert
            sheet.GetTitle().Should().Be(title, $"because title for topic is specified");
        }

        [Test]
        public void ReplaceRootTopic_DefaultFlow_Success()
        {
            //Arrange
            var book = new XMindConfiguration()
                .WithFileWriter(useDefaultPath: true, zip: true)
                .CreateWorkBook(workbookName: "test");
            var topic = book.CreateTopic();
            //Act
            book
                .GetPrimarySheet()
                .ReplaceRootTopic(topic);
            //Assert
            book.GetPrimarySheet().GetRootTopic().Should().Be(topic, "because we replace root topic with new one");
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            if (_isCleanUpNeeded)
            {
                var xmindOutput = new DirectoryInfo(_xmindOutputFolderName);
                if (xmindOutput.Exists)
                    xmindOutput.Delete(true);
            }
        }
    }
}
