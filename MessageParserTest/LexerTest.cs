using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExchangeHog.MessageParser;
using System.IO;

namespace TestProject1
{
	[TestClass]
	public class LexerTest
	{
		[TestMethod]
		public void TestAtom()
		{
			Lexer p = new Lexer("abc");
			Token t = p.next(Lexer.Mode.DEFAULT);
			Assert.IsTrue(t.type == Token.Type.ATOM);
			Assert.IsTrue(t.repr.CompareTo("abc") == 0);
		}

		[TestMethod]
		public void TestDotAtom()
		{
			Lexer p = new Lexer("abc.def");
			Token t = p.next(Lexer.Mode.DEFAULT);
			Assert.IsTrue(t.type == Token.Type.DOT_ATOM);
			Assert.IsTrue(t.repr.CompareTo("abc.def") == 0);
		}

		//[TestMethod]
		//[ExpectedException(typeof(LexerException), "Unrecognized token at @")]
		//public void TestAtomNegative()
		//{
		//	Lexer p = new Lexer("@abc");
		//	p.next(Lexer.Mode.DEFAULT);
		//}

		[TestMethod]
		public void TestQuotedString()
		{
			Lexer l = new Lexer("\"abc\"");
			Token t = l.next(Lexer.Mode.DEFAULT);
			Assert.IsTrue(t.type == Token.Type.QUOTED_STRING);
			Assert.IsTrue(t.repr.CompareTo("\"abc\"") == 0);
		}

		[TestMethod]
		public void TestQuotedStringWithSpaces()
		{
			Lexer l = new Lexer("\" abc def\"");
			Token t = l.next(Lexer.Mode.DEFAULT);
			Assert.IsTrue(t.type == Token.Type.QUOTED_STRING);
			Assert.IsTrue(t.repr.CompareTo("\" abc def\"") == 0);
		}

		[TestMethod]
		public void TestQuotedStringWithQuotedChars()
		{
			Lexer l = new Lexer("\"abc\\\"def\"");
			Token t = l.next(Lexer.Mode.DEFAULT);
			Assert.IsTrue(t.type == Token.Type.QUOTED_STRING);
			Assert.IsTrue(t.repr.CompareTo("\"abc\"def\"") == 0);
		}
	}
}
