using Entities;
using Loders;
using System.Xml;

namespace DBTest
{
    public class XMLClassTests
    {
        XmlHandler xmlLoader;
        [SetUp]
        public void Setup()
        {
            xmlLoader = new XmlHandler("test_res/test.xml");
        }

        [Test]
        public void DeserealizeOrder()
        {
            xmlLoader.LoadXml();
            xmlLoader.ConvertToDBFormat();
            Assert.That(xmlLoader.DBOrdersList?.Count, Is.EqualTo(5));
        }
        
        [Test]
        public void DeserealizeWrongXMLFile()
        {
            xmlLoader = new XmlHandler("test_res/wrong_test.xml");
            xmlLoader.LoadXml();
            xmlLoader.ConvertToDBFormat();
            Assert.That(xmlLoader.DBOrdersList?.Count, Is.EqualTo(0));
        }
        [Test]
        public void UniqueValueDrop()
        {
            xmlLoader = new XmlHandler("test_res/test2.xml");
            xmlLoader.LoadXml();
            xmlLoader.ConvertToDBFormat();
            Assert.That(xmlLoader.DBOrdersList?.Count, Is.EqualTo(9));
            Assert.That(xmlLoader.Orders?.Count, Is.EqualTo(4));
            Assert.That(xmlLoader.Products?.Count, Is.EqualTo(4));
            Assert.That(xmlLoader.Users?.Count, Is.EqualTo(2));
        }
        
    }
}