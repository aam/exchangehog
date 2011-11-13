using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeHog.MessageParser
{
	public class ParseException: Exception
	{
		internal ParseException(String message) : base(message) { }
	}
	public class Parser
	{
		Lexer l;
		public Parser(StreamReader sr) { l = new Lexer(sr); }

		public Parser(String s) { l = new Lexer(s); }

		void parseDateTime()
		{
			String dayofweek = null;
			//
			//	[ day-of-week "," ]
			//
			Token t = l.next(Lexer.Mode.DEFAULT);
			if (t.type == Token.Type.ATOM)
				switch (t.repr)
				{
					case "Mon":
					case "Tue":
					case "Wed":
					case "Thu":
					case "Fri":
					case "Sat":
					case "Sun":
						dayofweek = t.repr;
//						l.expectAndAdvance(Token.Type.COMMA);
						break;
				}
			//
			//	date = day month year
			//
			//
			//	time = hour ":" min [":" sec] zone
			//
		}

		//
		//	address
		//		= mailbox/group
		//
		//	where
		//		mailbox 
		//			= name-addr/addr-spec
		//
		//	=
		//		[display-name] angle-addr /
		//		addr-spec /
		//		group
		//	=
		//		[phrase] [CFWS] "<" addr-spec ">" [CFWS] /
		//		addr-spec /
		//		display-name ":" [group-list] ";" [CFWS]
		//	=
		//		[1*word] [CFWS] "<" addr-spec ">" [CFWS] /
		//		addr-spec /
		//		1*word ":" [mailbox-list/CFWS] ";" [CFWS]
		//	=
		//		[1*word] [CFWS] "<" addr-spec ">" [CFWS] /
		//		addr-spec /
		//		1*word ":" [mailbox-list / CFWS] ";" [CFWS]
		//	=
		//		[1*word] [CFWS] "<" addr-spec ">" [CFWS] /
		//		addr-spec /
		//		1*word ":" [(mailbox *("," mailbox)) / CFWS] ";" [CFWS]
		//		
		//	where	
		//
		//	addr-spec	=	(dot-atom / quoted-string ) "@" (dot-atom / [CFWS] "[" *([FWS] dtext) [FWS] "]" [CFWS]
		//
		//	where
		//		dtext	= %d33-90 / %d94-126 Printable US-ASCII chars not including "[", "]", "\"

		//
		//	parse phrase/dot-atom/quoted-string prefix that is common to
		//		name-addr
		//		addr-spec
		//		group
		//
		String parseLocalPartOrDisplayName()
		{
			StringBuilder sbName = new StringBuilder();
			Token t = l.nextNonCFWS();
			while (t.type == Token.Type.ATOM || t.type == Token.Type.DOT_ATOM || t.type == Token.Type.QUOTED_STRING)
			{
				sbName.Append(t.repr);
				t = l.nextNonCFWS();
			}
			return sbName.ToString();
		}

		public interface Addresses
		{
			IEnumerable<Address> addresses();
		}

		public Addresses parseMailboxOrGroup()
		{
			String localPartOrDisplayName = parseLocalPartOrDisplayName();
			Token t = l.current();
			switch (t.type)
			{
				case Token.Type.AT:
				case Token.Type.LESS: // name-addr
					return new SingleAddress(parseMailbox(localPartOrDisplayName));
				case Token.Type.COLON: // group
					return parseGroup(localPartOrDisplayName);
			}
			throw new ParseException("Unexpected token while expecting mailbox or group");
		}

		public class Address
		{
			public String name { get; set; }
			public Address(String name) { this.name = name; }
		}

		class SingleAddress : Addresses
		{
			Address a;
			public SingleAddress(Address a) { this.a = a; }
			public IEnumerable<Address> addresses() { yield return a; }
		}

		String parseAngleAddr(String sDisplayName)
		{
			var sb = new StringBuilder(sDisplayName);
			l.expectCurrent(Token.Type.LESS);
			sb.Append(l.current().repr);
			String angleAddr = parseAddrSpec(parseLocalPartOrDisplayName());
			sb.Append(angleAddr);
			l.nextNonCFWS();
			l.expectCurrent(Token.Type.GREAT);
			sb.Append(l.current().repr);
			return sb.ToString();
		} // end of parseAngleAddr

		String parseAddrSpec(String sLocalPart)
		{
			StringBuilder sbAddr = new StringBuilder(sLocalPart); 
			l.expectCurrent(Token.Type.AT);
			sbAddr.Append(l.current().repr);

			//
			//	parse domain
			//
			Token t = l.nextNonCFWS();
			switch (t.type)
			{
				case Token.Type.LBRACKET:
					do
					{
						sbAddr.Append(t.repr);
						t = l.next(Lexer.Mode.DTEXT);
						switch (t.type)
						{
							case Token.Type.WSP: sbAddr.Append(" "); break;
							case Token.Type.DTEXT: sbAddr.Append(t.repr); break;
						}
					} while (t.type != Token.Type.RBRACKET);
					sbAddr.Append(t.repr);
					break;
				case Token.Type.ATOM:
				case Token.Type.DOT_ATOM:
					sbAddr.Append(t.repr);
					break;
			}
			return sbAddr.ToString();
		} // end of parseAddrSpec


		class AddressGroup : Addresses
		{
			string displayName;
			public LinkedList<Address> listAddresses;

			public AddressGroup(string displayName) { this.displayName = displayName; listAddresses = new LinkedList<Address>(); }

			public IEnumerable<Address> addresses() { return listAddresses; }
		}

		AddressGroup parseGroup(String displayName)
		{
			var ag = new AddressGroup(displayName);
			l.expectCurrent(Token.Type.COLON);
			do
			{
				ag.listAddresses.AddLast(parseMailbox());
				if (l.current().type != Token.Type.SEMICOLON)
					l.nextNonCFWS();
				else
					break;
			} while (l.current().type == Token.Type.COMMA);
			l.expectCurrent(Token.Type.SEMICOLON);
			return ag;
		} // end of parseGroup

		Address parseMailbox() { return parseMailbox(parseLocalPartOrDisplayName()); }

		Address parseMailbox(String localPartOrDisplayName)
		{
			String mailbox = null;
			Token t = l.current();
			if (t.type == Token.Type.AT)
				mailbox = parseAddrSpec(localPartOrDisplayName);
			else if (t.type == Token.Type.LESS) // name-addr
				mailbox = parseAngleAddr(localPartOrDisplayName);
			return new Address(mailbox);
		} // end of parseMailbox

        Field parseField()
        {
			Token t = l.next(Lexer.Mode.FIELDNAME);
			switch (t.repr)
			{
				case "Date:": break; // date-time CRLF
				case "From:": break; // mailbox-list CRLF
				case "Sender:": break; // mailbox CRLF
				case "Reply-To:": break; // address-list CRLF
				case "To:": break; // address-list CRLF
				case "Cc:": break; // address-list CRLF
				case "Bcc:": break; // [address-list/CFWS] CRLF

				//
				//	msg-id = [CFWS] "<" dot-atom-text "@" (dot-atom-text/ ("[" *dtext "]")) ">" [CFWS]
				//
				case "Message-ID:": break; // msg-id CRLF
				case "In-Reply-To:": break; // 1*msg-id CRLF
				case "References:": break; // 1*msg-id CRLF

				case "Subject:": break; // unstructured CRLF
				case "Comments:": break; // unstructured CRLF
				case "Keywords:": break; // 1*(atom/quoted-string) *("," 1*(atom/quoted-string)) CRLF

				case "Resent-Date:": break; // date-time CRLF
				case "Resent-From:": break; //  mailbox-list CRLF
				case "Resent-Sender:": break; //  mailbox CRLF
				case "Resent-To:": break; //  address-list CRLF
				case "Resent-Cc:": break; //  address-list CRLF
				case "Resent-Bcc:": break; //  [address-list / CFWS] CRLF
				case "Resent-Message-ID:": break; //  msg-id CRLF

					//
					// path = angle-addr / ([CFWS] "<" [CFWS] ">" [CFWS])
					//
				case "Return-Path:": break; // path CRLF
					//
					// received-token = word / angle-addr / addr-spec / domain
					//
				case "Received:": break; //  "Received:" *received-token ";" date-time CRLF
				default:
					//
					//	optional-field = 1*(33-57/59-126) ":" unstructured CRLF
					//
					break;
			}
			return null;
        }

		public class Field
		{
			public string name { get; set; }
			public string value { get; set; } 

			public Field(String name, String value) { this.name = name; this.value = value; }
		}

		public class Message
		{
			public ICollection<Field> fields;
			public String body;

			public Message(ICollection<Field> fields, String body) { this.fields = fields; this.body = body; }
		}

		Message parseMessage(String msg)
		{
			var fields = new LinkedList<Field>();
			do
			{
				Field fld = parseField();
				fields.AddLast(fld);
			} while (l.current().type != Token.Type.CRLF);

			StringBuilder sbBody = new StringBuilder();
			while(true)
			{
				Token t = l.next(Lexer.Mode.BODY);
				if (t.type == Token.Type.BODY_TEXT)
				{
					sbBody.Append(t.repr);
					t = l.next(Lexer.Mode.BODY);
					l.expectCurrent(Token.Type.CRLF);
				}
				else if (t.type == Token.Type.CRLF)
					break;
			}

			return new Message(fields, sbBody.ToString());
		}
	}
}
