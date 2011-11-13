using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeHog.MessageParser
{
	public struct Token
	{
		public enum Type { WSP, COMMENT, ATOM, DOT_ATOM, QUOTED_STRING, LESS, GREAT, AT, DOMAIN_LITERAL, COMMA, SEMICOLON, COLON, LBRACKET, RBRACKET, DTEXT, CRLF, EOS, BODY_TEXT } ;
		public Type type;
		public String repr;

		//public Token(Type _type) { type = _type; }
		public Token(Type _type, String _repr) { this.type = _type; this.repr = _repr; }
	}

	public class LexerException : Exception
	{
		public LexerException(String message) : base(message) { }
	}

	public class Lexer
	{
		public enum Mode { DEFAULT, DTEXT, FIELDNAME, BODY };

		int curPos;
		String message;
		char curChar() { return curPos < message.Length? message[curPos]: Char.MinValue; }
		char curCharAndAdvance() { return curPos < message.Length? message[curPos++]: Char.MinValue; }
		char peekChar() { return (curPos + 1) < message.Length? message[curPos + 1]: Char.MinValue; }
		char nextChar() { curPos++; return curChar(); }
		char requireAndAdvance(char ch, String msgError) { if (curChar() != ch) throw new LexerException(msgError); return nextChar(); }
		void expectCurrentChar(char ch, String msgError) { if (curChar() != ch) throw new LexerException(msgError); }
		void expectNextChar(char ch, String msgError) { if (nextChar() != ch) throw new LexerException(msgError); }

		public Lexer(StreamReader sr) : this(sr.ReadToEnd()) {}

		public Lexer(String s)
		{
			message = s;
			curPos = -1;
		}

		bool isQText(char ch)
		{
			return ch == 33 || 35 <= ch && ch <= 91 || 93 <= ch && ch <= 126; // printable US-ASCII chars excluding "\", '"'
		}

		Token lexQuotedString()
		{
			requireAndAdvance('"', "Opening quote for quoted string is missing");

			var repr = new StringBuilder("\"");
			while(true)
			{
				char ch = curChar();
				if (isFWS(ch))
				{
					Token t = lexFWS();
					repr.Append(t.repr);
					nextChar();
				}
				else if (isQText(ch))
					repr.Append(curCharAndAdvance());
				else if (ch == '\\')
				{
					nextChar();
					repr.Append(curCharAndAdvance());
				}
				else
					break;
			}
			expectCurrentChar('"', "Closing quote for quoted string is missing");
			repr.Append("\"");

			return new Token(Token.Type.QUOTED_STRING, repr.ToString());
		}

		bool isAtomChar(char ch)
		{
			if ('0' <= ch && ch <= '9' || 'a' <= ch && ch <= 'z' || 'A' <= ch && ch <= 'Z')
				return true;

			switch (ch)
			{
				case '!':
				case '#':
				case '$':
				case '%':
				case '&':
				case '\'':
				case '*':
				case '+':
				case '-':
				case '/':
				case '=':
				case '?':
				case '^':
				case '_':
				case '`':
				case '{':
				case '|':
				case '}':
				case '~':
					return true;
			}
			return false;
		}

		Token lexAtom()
		{
			var repr = new StringBuilder("");
			Char ch = curChar();
			if (!isAtomChar(ch))
				throw new LexerException("Atom character is expected instead of '" + ch + "'");

			while(true)
			{
				repr.Append(curChar());
				ch = peekChar();
				if (isAtomChar(ch)) ch = nextChar(); else break;
			}

			return new Token(Token.Type.ATOM, repr.ToString());
		}

		Token lexDotAtom()
		{
			Token tDotAtom = lexAtom();

			while (peekChar() == '.')
			{
				tDotAtom.type = Token.Type.DOT_ATOM;
				nextChar();
				nextChar();
				tDotAtom.repr += "." + lexAtom().repr;
			}

			return tDotAtom;
		}

		internal void expectCurrent(Token.Type type)
		{
			if (curToken.type != type)
				throw new LexerException("Unexpected token " + curToken.type + " while expected " + type);
		}

		private Boolean isDText(Char ch)
		{
			return (33 <= ch && ch <= 90 || 94 <= ch && ch <= 126);
		}

		private Token lexDText()
		{
			var sb = new StringBuilder();
			while (isDText(curChar()))
				sb.Append(curChar());
			return new Token(Token.Type.DTEXT, sb.ToString());
		} // end of lexDText

		Token lexCRLForFWS()
		{
			requireAndAdvance('\r', "CRLF should start with \\r");
			requireAndAdvance('\n', "CRLF should end with \\n");
			if (isWSP(curChar()))
			{
				StringBuilder sb = new StringBuilder("\r\n ");
				//
				// folding WSP
				//
				while(true)
				{
					sb.Append(curChar());
					if (isWSP(peekChar())) nextChar(); else break;
				}
				return new Token(Token.Type.WSP, sb.ToString());
			}
			else
				return new Token(Token.Type.CRLF, "\r\n");
		}

		private Boolean isWSP(Char ch) { return ch == ' ' || ch == '\t'; }

		private Boolean isFWS(Char ch)
		{
			switch (ch)
			{
				case '\r':
				case '\n':
				case '\t':
				case ' ': return true;
				default:
					return false;
			}
		}

		private Token lexFWS()
		{
			StringBuilder sb = new StringBuilder();
			if (isWSP(curChar()))
				while(true)
				{
					sb.Append(curChar());
					if (isWSP(peekChar())) nextChar(); else break;
				}
			if (curChar() == '\r')
			{
				sb.Append(curChar());
				expectNextChar('\n', "CR should come with LF");
				sb.Append(curChar());
				if (!isWSP(nextChar()))
					throw new LexerException("WSP is required after CRLF");
				while(true)
				{
					sb.Append(curChar());
					if (isWSP(peekChar())) nextChar(); else break;
				}
			}
			return new Token(Token.Type.WSP, sb.ToString());
		}

		private Boolean isVCHAR(Char ch)
		{
			return 0x21 <= ch && ch <= 0x7e;
		}

		Token lexComment()
		{
			expectCurrentChar('(', "Comment should start with left parenthesis");
			StringBuilder sb = new StringBuilder();
			Char ch = nextChar();
			if (isFWS(ch))
			{
				Token t = lexFWS();
				sb.Append(t.repr);
			}
			do
			{
				if (33 <= ch && ch <= 39 || 42 <= ch && ch <= 91 || 93 <= ch && ch <= 126)
					sb.Append(ch); // ctext
				else if (ch == '\\')
				{
					//
					// quoted-pair
					//
					ch = nextChar();
					if (isWSP(ch) || isVCHAR(ch))
						sb.Append(ch);
					else
						throw new LexerException("Unrecognized quoted char : " + ch);
				}
				else if (isFWS(ch))
				{
					Token t = lexFWS();
					sb.Append(t.repr);
				}
				else if (ch == '(')
					sb.Append(lexComment().repr);

				ch = nextChar();
			} while (ch != ')');

			return new Token(Token.Type.COMMENT, sb.ToString());
		}

		Token curToken;
		public Token next(Mode mode)
		{
			Char ch = nextChar();
			switch (mode)
			{
				case Mode.DTEXT:
					if (isDText(ch))
						curToken = lexDText();
					else if (isFWS(ch))
						curToken = lexFWS();
					else if (ch == ']')
						curToken = new Token(Token.Type.RBRACKET, "]");
					else
						throw new LexerException("Unexpected character in " + mode + " mode: " + ch);
					break;
				case Mode.DEFAULT:
					switch (ch)
					{
						case Char.MinValue: curToken = new Token(Token.Type.EOS, ""); break;
						case '(': curToken = lexComment(); break;
						case '\r': curToken = lexCRLForFWS(); break;
						case ' ': case '\t': curToken = lexFWS(); break;
						//case '\\': return lexQuotedChar();
						case '"': curToken = lexQuotedString(); break;
						case '<': curToken = new Token(Token.Type.LESS, "<"); break;
						case '>': curToken = new Token(Token.Type.GREAT, ">"); break;
						case '@': curToken = new Token(Token.Type.AT, "@"); break;
						case ',': curToken = new Token(Token.Type.COMMA, ","); break;
						case ';': curToken = new Token(Token.Type.SEMICOLON, ";"); break;
						case ':': curToken = new Token(Token.Type.COLON, ":"); break;
						case '[': curToken = new Token(Token.Type.LBRACKET, "["); break;
						case ']': curToken = new Token(Token.Type.RBRACKET, "]"); break;
						default:
							if (isAtomChar(ch))
							{
								curToken = lexDotAtom();  // dot-atom allowed?
								break;
							}
							else
								throw new LexerException("Unrecognized token at '" + curChar() + "'");
					}
					break;
				//if (0x21 < curChar && curChar < 0x7e) return new Token(Token.Type.VChar);
			}

			return curToken;			
		} // end of next

		public Token nextNonCFWS()
		{
			bool didSkipSomeFWS = false;
			while(true)
			{
				next(Mode.DEFAULT);
				if (curToken.type == Token.Type.WSP || curToken.type == Token.Type.COMMENT)
					if (curToken.type == Token.Type.WSP)
						didSkipSomeFWS = true;
					else { }
				else
					break;
			}
			if (didSkipSomeFWS)
				curToken.repr = " " + curToken.repr;
			return curToken;
		}

		public Token current() { return curToken; }
	}
}
