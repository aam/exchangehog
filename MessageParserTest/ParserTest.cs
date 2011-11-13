using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExchangeHog.MessageParser;

namespace ExchangeHog.MessageParserTest
{
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	[TestClass]
	public class ParserTest
	{
		public ParserTest()
		{
		}

		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}

		#region Additional test attributes
		//
		// You can use the following additional attributes as you write your tests:
		//
		// Use ClassInitialize to run code before running the first test in the class
		// [ClassInitialize()]
		// public static void MyClassInitialize(TestContext testContext) { }
		//
		// Use ClassCleanup to run code after all tests in a class have run
		// [ClassCleanup()]
		// public static void MyClassCleanup() { }
		//
		// Use TestInitialize to run code before running each test 
		// [TestInitialize()]
		// public void MyTestInitialize() { }
		//
		// Use TestCleanup to run code after each test has run
		// [TestCleanup()]
		// public void MyTestCleanup() { }
		//
		#endregion

		[TestMethod]
		public void GeneralTest()
		{
			//
			// TODO: Add test logic here
			//
		}

		[TestMethod]
		public void TestMailbox()
		{
			Parser p = new Parser("abc@def.com");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual(theaddr.name, "abc@def.com");
		}

		[TestMethod]
		public void TestMailboxWithSpaces()
		{
			Parser p = new Parser("    abc    @		def.com		 ");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual(" abc @ def.com", theaddr.name);
		}

		[TestMethod]
		public void TestMailboxWithFWS()
		{
			Parser p = new Parser("    abc    @	\r\n	def.com		 ");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual(" abc @ def.com", theaddr.name);
		}

		[TestMethod]
		public void TestMailboxWithComments()
		{
			Parser p = new Parser("abc(Kuka)@(Kuka)def.com ");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual("abc@def.com", theaddr.name);
		}

		[TestMethod]
		public void TestMailboxWithSpacesAndComments()
		{
			Parser p = new Parser("abc (Kuka) @ (Kuka) def.com ");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual("abc @ def.com", theaddr.name);
		}

		[TestMethod]
		public void TestMailboxWithQuotedName()
		{
			Parser p = new Parser("\"Joe Q. Public\" <john.q.public@example.com>");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual("\"Joe Q. Public\" <john.q.public@example.com>", theaddr.name);
		}

		[TestMethod]
		public void TestMailboxWithAtomsName()
		{
			Parser p = new Parser("Mary Smith <mary@example.net>");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual("Mary Smith <mary@example.net>", theaddr.name);
		}

		[TestMethod]
		public void TestMailboxWithQuotedName1()
		{
			Parser p = new Parser("\"Mary Smith: Personal Account\" <smith@home.example>");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual("\"Mary Smith: Personal Account\" <smith@home.example>", theaddr.name);
		}

		[TestMethod]
		[ExpectedException(typeof(LexerException), "Unrecognized token at '.'")]
		public void TestObsoleteMailboxWithCommentsInAddress()
		{
			Parser p = new Parser("John Doe <jdoe@machine(comment).  example>");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual("John Doe <jdoe@machine. example>", theaddr.name);
		}

		[TestMethod]
		[ExpectedException(typeof(LexerException), "Atom character expected instead of ' '")]
		public void TestObsoleteMailboxWithoutQuotesAroundDot()
		{
			Parser p = new Parser("Joe Q. Public <john.q.public@example.com>");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual("Joe Q. Public <john.q.public@example.com>", theaddr.name);
		}

		[TestMethod]
		public void TestMailboxWithCommentsInDisplayNameAndAngleAddr()
		{
			Parser p = new Parser("Pete(A nice \\) chap) <pete(his account)@silly.test(his host)>");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual("Pete <pete@silly.test>", theaddr.name);
		}

		[TestMethod]
		public void TestMailboxWithBigGroup()
		{
			Parser p = new Parser("A Group(Some people)\r\n" +
				"     :Chris Jones <c@(Chris's host.)public.example>,\r\n" +
				"            joe@example.org,\r\n" +
				"     John <jdoe@one.test> (my dear friend); (the end of the group)");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(3, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual("Chris Jones <c@public.example>", theaddr.name);
			addresses.MoveNext();
			theaddr = addresses.Current;
			Assert.AreEqual(" joe@example.org", theaddr.name);
			addresses.MoveNext();
			theaddr = addresses.Current;
			Assert.AreEqual(" John <jdoe@one.test>", theaddr.name);
		}
		 
		[TestMethod]
		public void TestMailboxWithSmallgGroup()
		{
			Parser p = new Parser("(Empty list)(start)Hidden recipients  :(nobody(that I know))  ;");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual(null, theaddr.name);
		}

		[TestMethod]
		public void TestMailboxWithQuestionMarkAddr()
		{
			Parser p = new Parser("Who? <one@y.test>");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual("Who? <one@y.test>", theaddr.name);
		}

		[TestMethod]
		public void TestMailboxWithQuotedStringAddr()
		{
			Parser p = new Parser("\"Giant; \\\"Big\\\" Box\" <sysservices@example.net>");
			Parser.Addresses addrs = p.parseMailboxOrGroup();
			int cnt = 0;
			foreach (Parser.Address addr in addrs.addresses())
				cnt++;
			Assert.AreEqual(1, cnt, "Expected one address");
			IEnumerator<Parser.Address> addresses = addrs.addresses().GetEnumerator();
			addresses.MoveNext();
			Parser.Address theaddr = addresses.Current;
			Assert.AreEqual("\"Giant; \"Big\" Box\" <sysservices@example.net>", theaddr.name);
		}
	}
}
