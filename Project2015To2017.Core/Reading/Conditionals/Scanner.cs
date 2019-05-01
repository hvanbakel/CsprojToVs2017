// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Project2015To2017.Reading.Conditionals
{
	/// <summary>
	/// Class:       Scanner
	/// This class does the scanning of the input and returns tokens.
	/// The usage pattern is:
	///    Scanner s = new Scanner(expression, CultureInfo)
	///    do {
	///      s.Advance();
	///    while (s.IsNext(Token.EndOfInput));
	/// 
	///  After Advance() is called, you can get the current token (s.CurrentToken),
	///  check it's type (s.IsNext()), get the string for it (s.NextString()).
	/// </summary>
	internal sealed class Scanner
	{
		private string _expression;
		private int _parsePoint;
		private Token _lookahead;
		private bool _errorState;
		private int _errorPosition;
		// What we found instead of what we were looking for
		private string _unexpectedlyFound = null;
		private ParserOptions _options;
		private string _errorResource = null;
		private static string s_endOfInput = null;

		/// <summary>
		/// Lazily format resource string to help avoid (in some perf critical cases) even loading
		/// resources at all.
		/// </summary>
		private string EndOfInput
		{
			get
			{
				if (s_endOfInput == null)
				{
					s_endOfInput = "EndOfInputTokenName";
				}

				return s_endOfInput;
			}
		}

		private Scanner() { }
		//
		// Constructor takes the string to parse and the culture.
		//
		internal Scanner(string expressionToParse, ParserOptions options)
		{
			// We currently have no support (and no scenarios) for disallowing property references
			// in Conditions.
			ErrorUtilities.VerifyThrow(0 != (options & ParserOptions.AllowProperties),
				"Properties should always be allowed.");

			this._expression = expressionToParse;
			this._parsePoint = 0;
			this._errorState = false;
			this._errorPosition = -1; // invalid
			this._options = options;
		}

		/// <summary>
		/// If the lexer errors, it has the best knowledge of the error message to show. For example,
		/// 'unexpected character' or 'illformed operator'. This method returns the name of the resource
		/// string that the parser should display.
		/// </summary>
		/// <remarks>Intentionally not a property getter to avoid the debugger triggering the Assert dialog</remarks>
		/// <returns></returns>
		internal string GetErrorResource()
		{
			if (this._errorResource == null)
			{
				// I do not believe this is reachable, but provide a reasonable default.
				Debug.Assert(false, "What code path did not set an appropriate error resource? Expression: " + this._expression);
				this._unexpectedlyFound = this.EndOfInput;
				return "UnexpectedCharacterInCondition";
			}
			else
			{
				return this._errorResource;
			}
		}

		internal bool IsNext(Token.TokenType type)
		{
			return this._lookahead.IsToken(type);
		}

		internal string IsNextString()
		{
			return this._lookahead.String;
		}

		internal Token CurrentToken
		{
			get { return this._lookahead; }
		}

		internal int GetErrorPosition()
		{
			Debug.Assert(-1 != this._errorPosition); // We should have set it
			return this._errorPosition;
		}

		// The string (usually a single character) we found unexpectedly. 
		// We might want to show it in the error message, to help the user spot the error.
		internal string UnexpectedlyFound
		{
			get
			{
				return this._unexpectedlyFound;
			}
		}

		/// <summary>
		/// Advance
		/// returns true on successful advance
		///     and false on an erroneous token
		///
		/// Doesn't return error until the bogus input is encountered.
		/// Advance() returns true even after EndOfInput is encountered.
		/// </summary>
		internal bool Advance()
		{
			if (this._errorState)
				return false;

			if (this._lookahead != null && this._lookahead.IsToken(Token.TokenType.EndOfInput))
				return true;

			SkipWhiteSpace();

			// Update error position after skipping whitespace
			this._errorPosition = this._parsePoint + 1;

			if (this._parsePoint >= this._expression.Length)
			{
				this._lookahead = Token.EndOfInput;
			}
			else
			{
				switch (this._expression[this._parsePoint])
				{
					case ',':
						this._lookahead = Token.Comma;
						this._parsePoint++;
						break;
					case '(':
						this._lookahead = Token.LeftParenthesis;
						this._parsePoint++;
						break;
					case ')':
						this._lookahead = Token.RightParenthesis;
						this._parsePoint++;
						break;
					case '$':
						if (!ParseProperty())
							return false;
						break;
					case '%':
						if (!ParseItemMetadata())
							return false;
						break;
					case '@':
						int start = this._parsePoint;
						// If the caller specified that he DOESN'T want to allow item lists ...
						if ((this._options & ParserOptions.AllowItemLists) == 0)
						{
							if ((this._parsePoint + 1) < this._expression.Length && this._expression[this._parsePoint + 1] == '(')
							{
								this._errorPosition = start + 1;
								this._errorState = true;
								this._errorResource = "ItemListNotAllowedInThisConditional";
								return false;
							}
						}
						if (!ParseItemList())
							return false;
						break;
					case '!':
						// negation and not-equal
						if ((this._parsePoint + 1) < this._expression.Length && this._expression[this._parsePoint + 1] == '=')
						{
							this._lookahead = Token.NotEqualTo;
							this._parsePoint += 2;
						}
						else
						{
							this._lookahead = Token.Not;
							this._parsePoint++;
						}
						break;
					case '>':
						// gt and gte
						if ((this._parsePoint + 1) < this._expression.Length && this._expression[this._parsePoint + 1] == '=')
						{
							this._lookahead = Token.GreaterThanOrEqualTo;
							this._parsePoint += 2;
						}
						else
						{
							this._lookahead = Token.GreaterThan;
							this._parsePoint++;
						}
						break;
					case '<':
						// lt and lte
						if ((this._parsePoint + 1) < this._expression.Length && this._expression[this._parsePoint + 1] == '=')
						{
							this._lookahead = Token.LessThanOrEqualTo;
							this._parsePoint += 2;
						}
						else
						{
							this._lookahead = Token.LessThan;
							this._parsePoint++;
						}
						break;
					case '=':
						if ((this._parsePoint + 1) < this._expression.Length && this._expression[this._parsePoint + 1] == '=')
						{
							this._lookahead = Token.EqualTo;
							this._parsePoint += 2;
						}
						else
						{
							this._errorPosition = this._parsePoint + 2; // expression[parsePoint + 1], counting from 1
							this._errorResource = "IllFormedEqualsInCondition";
							if ((this._parsePoint + 1) < this._expression.Length)
							{
								// store the char we found instead
								this._unexpectedlyFound = Convert.ToString(this._expression[this._parsePoint + 1], CultureInfo.InvariantCulture);
							}
							else
							{
								this._unexpectedlyFound = this.EndOfInput;
							}
							this._parsePoint++;
							this._errorState = true;
							return false;
						}
						break;
					case '\'':
						if (!ParseQuotedString())
							return false;
						break;
					default:
						// Simple strings, function calls, decimal numbers, hex numbers
						if (!ParseRemaining())
							return false;
						break;
				}
			}
			return true;
		}

		/// <summary>
		/// Parses either the $(propertyname) syntax or the %(metadataname) syntax, 
		/// and returns the parsed string beginning with the '$' or '%', and ending with the
		/// closing parenthesis.
		/// </summary>
		/// <returns></returns>
		private string ParsePropertyOrItemMetadata()
		{
			int start = this._parsePoint; // set start so that we include "$(" or "%("
			this._parsePoint++;

			if (this._parsePoint < this._expression.Length && this._expression[this._parsePoint] != '(')
			{
				this._errorState = true;
				this._errorPosition = start + 1;
				this._errorResource = "IllFormedPropertyOpenParenthesisInCondition";
				this._unexpectedlyFound = Convert.ToString(this._expression[this._parsePoint], CultureInfo.InvariantCulture);
				return null;
			}

			this._parsePoint = ScanForPropertyExpressionEnd(this._expression, this._parsePoint++);

			// Maybe we need to generate an error for invalid characters in property/metadata name?
			// For now, just wait and let the property/metadata evaluation handle the error case.
			if (this._parsePoint >= this._expression.Length)
			{
				this._errorState = true;
				this._errorPosition = start + 1;
				this._errorResource = "IllFormedPropertyCloseParenthesisInCondition";
				this._unexpectedlyFound = this.EndOfInput;
				return null;
			}

			this._parsePoint++;
			return this._expression.Substring(start, this._parsePoint - start);
		}

		/// <summary>
		/// Scan for the end of the property expression
		/// </summary>
		private static int ScanForPropertyExpressionEnd(string expression, int index)
		{
			int nestLevel = 0;

			while (index < expression.Length)
			{
				char character = expression[index];
				if (character == '(')
				{
					nestLevel++;
				}
				else if (character == ')')
				{
					nestLevel--;
				}

				// We have reached the end of the parenthesis nesting
				// this should be the end of the property expression
				// If it is not then the calling code will determine that
				if (nestLevel == 0)
				{
					return index;
				}
				else
				{
					index++;
				}
			}

			return index;
		}

		/// <summary>
		/// Parses a string of the form $(propertyname).
		/// </summary>
		/// <returns></returns>
		private bool ParseProperty()
		{
			string propertyExpression = this.ParsePropertyOrItemMetadata();

			if (propertyExpression == null)
			{
				return false;
			}
			else
			{
				this._lookahead = new Token(Token.TokenType.Property, propertyExpression);
				return true;
			}
		}

		/// <summary>
		/// Parses a string of the form %(itemmetadataname).
		/// </summary>
		/// <returns></returns>
		private bool ParseItemMetadata()
		{
			string itemMetadataExpression = this.ParsePropertyOrItemMetadata();

			if (itemMetadataExpression == null)
			{
				// The ParsePropertyOrItemMetadata method returns the correct error resources
				// for parsing properties such as $(propertyname).  At this stage in the Whidbey
				// cycle, we're not allowed to add new string resources, so I can't add a new
				// resource specific to item metadata, so here, we just change the error to
				// the generic "UnexpectedCharacter".
				this._errorResource = "UnexpectedCharacterInCondition";
				return false;
			}

			this._lookahead = new Token(Token.TokenType.ItemMetadata, itemMetadataExpression);

			if (!CheckForUnexpectedMetadata(itemMetadataExpression))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Helper to verify that any AllowBuiltInMetadata or AllowCustomMetadata
		/// specifications are not respected.
		/// Returns true if it is ok, otherwise false.
		/// </summary>
		private bool CheckForUnexpectedMetadata(string expression)
		{
			if ((this._options & ParserOptions.AllowItemMetadata) == ParserOptions.AllowItemMetadata)
			{
				return true;
			}

			return true;
		}

		private bool ParseInternalItemList()
		{
			int start = this._parsePoint;
			this._parsePoint++;

			if (this._parsePoint < this._expression.Length && this._expression[this._parsePoint] != '(')
			{
				// @ was not followed by (
				this._errorPosition = start + 1;
				this._errorResource = "IllFormedItemListOpenParenthesisInCondition";
				// Not useful to set unexpectedlyFound here. The message is going to be detailed enough.
				this._errorState = true;
				return false;
			}
			this._parsePoint++;
			// Maybe we need to generate an error for invalid characters in itemgroup name?
			// For now, just let item evaluation handle the error.
			bool fInReplacement = false;
			int parenToClose = 0;
			while (this._parsePoint < this._expression.Length)
			{
				if (this._expression[this._parsePoint] == '\'')
				{
					fInReplacement = !fInReplacement;
				}
				else if (this._expression[this._parsePoint] == '(' && !fInReplacement)
				{
					parenToClose++;
				}
				else if (this._expression[this._parsePoint] == ')' && !fInReplacement)
				{
					if (parenToClose == 0)
					{
						break;
					}
					else { parenToClose--; }
				}
				this._parsePoint++;
			}
			if (this._parsePoint >= this._expression.Length)
			{
				this._errorPosition = start + 1;
				if (fInReplacement)
				{
					// @( ... ' was never followed by a closing quote before the closing parenthesis
					this._errorResource = "IllFormedItemListQuoteInCondition";
				}
				else
				{
					// @( was never followed by a )
					this._errorResource = "IllFormedItemListCloseParenthesisInCondition";
				}
				// Not useful to set unexpectedlyFound here. The message is going to be detailed enough.
				this._errorState = true;
				return false;
			}
			this._parsePoint++;
			return true;
		}

		private bool ParseItemList()
		{
			int start = this._parsePoint;
			if (!ParseInternalItemList())
			{
				return false;
			}
			this._lookahead = new Token(Token.TokenType.ItemList, this._expression.Substring(start, this._parsePoint - start));
			return true;
		}

		/// <summary>
		/// Parse any part of the conditional expression that is quoted. It may contain a property, item, or 
		/// metadata element that needs expansion during evaluation.
		/// </summary>
		private bool ParseQuotedString()
		{
			this._parsePoint++;
			int start = this._parsePoint;
			bool expandable = false;
			while (this._parsePoint < this._expression.Length && this._expression[this._parsePoint] != '\'')
			{
				// Standalone percent-sign must be allowed within a condition because it's
				// needed to escape special characters.  However, percent-sign followed
				// by open-parenthesis is an indication of an item metadata reference, and
				// that is only allowed in certain contexts.
				if ((this._expression[this._parsePoint] == '%') && ((this._parsePoint + 1) < this._expression.Length) && (this._expression[this._parsePoint + 1] == '('))
				{
					expandable = true;
					string name = String.Empty;

					int endOfName = this._expression.IndexOf(')', this._parsePoint) - 1;
					if (endOfName < 0)
					{
						endOfName = this._expression.Length - 1;
					}

					// If it's %(a.b) the name is just 'b'
					if (this._parsePoint + 3 < this._expression.Length)
					{
						name = this._expression.Substring(this._parsePoint + 2, (endOfName - this._parsePoint - 2 + 1));
					}

					if (!CheckForUnexpectedMetadata(name))
					{
						return false;
					}
				}
				else if (this._expression[this._parsePoint] == '@' && ((this._parsePoint + 1) < this._expression.Length) && (this._expression[this._parsePoint + 1] == '('))
				{
					expandable = true;

					// If the caller specified that he DOESN'T want to allow item lists ...
					if ((this._options & ParserOptions.AllowItemLists) == 0)
					{
						this._errorPosition = start + 1;
						this._errorState = true;
						this._errorResource = "ItemListNotAllowedInThisConditional";
						return false;
					}

					// Item lists have to be parsed because of the replacement syntax e.g. @(Foo,'_').
					// I have to know how to parse those so I can skip over the tic marks.  I don't
					// have to do that with other things like propertygroups, hence itemlists are
					// treated specially.

					ParseInternalItemList();
					continue;
				}
				else if (this._expression[this._parsePoint] == '$' && ((this._parsePoint + 1) < this._expression.Length) && (this._expression[this._parsePoint + 1] == '('))
				{
					expandable = true;
				}
				else if (this._expression[this._parsePoint] == '%')
				{
					// There may be some escaped characters in the expression
					expandable = true;
				}
				this._parsePoint++;
			}

			if (this._parsePoint >= this._expression.Length)
			{
				// Quoted string wasn't closed
				this._errorState = true;
				this._errorPosition = start; // The message is going to say "expected after position n" so don't add 1 here.
				this._errorResource = "IllFormedQuotedStringInCondition";
				// Not useful to set unexpectedlyFound here. By definition it got to the end of the string.
				return false;
			}
			string originalTokenString = this._expression.Substring(start, this._parsePoint - start);

			this._lookahead = new Token(Token.TokenType.String, originalTokenString, expandable);
			this._parsePoint++;
			return true;
		}

		private bool ParseRemaining()
		{
			int start = this._parsePoint;
			if (CharacterUtilities.IsNumberStart(this._expression[this._parsePoint])) // numeric
			{
				if (!ParseNumeric(start))
					return false;
			}
			else if (CharacterUtilities.IsSimpleStringStart(this._expression[this._parsePoint])) // simple string (handle 'and' and 'or')
			{
				if (!ParseSimpleStringOrFunction(start))
					return false;
			}
			else
			{
				// Something that wasn't a number or a letter, like a newline (%0a)
				this._errorState = true;
				this._errorPosition = start + 1;
				this._errorResource = "UnexpectedCharacterInCondition";
				this._unexpectedlyFound = Convert.ToString(this._expression[this._parsePoint], CultureInfo.InvariantCulture);
				return false;
			}
			return true;
		}

		// There is a bug here that spaces are not required around 'and' and 'or'. For example,
		// this works perfectly well:
		// Condition="%(a.Identity)!=''and%(a.m)=='1'"
		// Since people now depend on this behavior, we must not change it.
		private bool ParseSimpleStringOrFunction(int start)
		{
			SkipSimpleStringChars();
			if (0 == string.Compare(this._expression.Substring(start, this._parsePoint - start), "and", StringComparison.OrdinalIgnoreCase))
			{
				this._lookahead = Token.And;
			}
			else if (0 == string.Compare(this._expression.Substring(start, this._parsePoint - start), "or", StringComparison.OrdinalIgnoreCase))
			{
				this._lookahead = Token.Or;
			}
			else
			{
				int end = this._parsePoint;
				SkipWhiteSpace();
				if (this._parsePoint < this._expression.Length && this._expression[this._parsePoint] == '(')
				{
					this._lookahead = new Token(Token.TokenType.Function, this._expression.Substring(start, end - start));
				}
				else
				{
					string tokenValue = this._expression.Substring(start, end - start);
					this._lookahead = new Token(Token.TokenType.String, tokenValue);
				}
			}
			return true;
		}
		private bool ParseNumeric(int start)
		{
			if ((this._expression.Length - this._parsePoint) > 2 && this._expression[this._parsePoint] == '0' && (this._expression[this._parsePoint + 1] == 'x' || this._expression[this._parsePoint + 1] == 'X'))
			{
				// Hex number
				this._parsePoint += 2;
				SkipHexDigits();
				this._lookahead = new Token(Token.TokenType.Numeric, this._expression.Substring(start, this._parsePoint - start));
			}
			else if (CharacterUtilities.IsNumberStart(this._expression[this._parsePoint]))
			{
				// Decimal number
				if (this._expression[this._parsePoint] == '+')
				{
					this._parsePoint++;
				}
				else if (this._expression[this._parsePoint] == '-')
				{
					this._parsePoint++;
				}
				do
				{
					SkipDigits();
					if (this._parsePoint < this._expression.Length && this._expression[this._parsePoint] == '.')
					{
						this._parsePoint++;
					}
					if (this._parsePoint < this._expression.Length)
					{
						SkipDigits();
					}
				} while (this._parsePoint < this._expression.Length && this._expression[this._parsePoint] == '.');
				// Do we need to error on malformed input like 0.00.00)? or will the conversion handle it?
				// For now, let the conversion generate the error.
				this._lookahead = new Token(Token.TokenType.Numeric, this._expression.Substring(start, this._parsePoint - start));
			}
			else
			{
				// Unreachable
				this._errorState = true;
				this._errorPosition = start + 1;
				return false;
			}
			return true;
		}
		private void SkipWhiteSpace()
		{
			while (this._parsePoint < this._expression.Length && char.IsWhiteSpace(this._expression[this._parsePoint]))
				this._parsePoint++;
			return;
		}
		private void SkipDigits()
		{
			while (this._parsePoint < this._expression.Length && char.IsDigit(this._expression[this._parsePoint]))
				this._parsePoint++;
			return;
		}
		private void SkipHexDigits()
		{
			while (this._parsePoint < this._expression.Length && CharacterUtilities.IsHexDigit(this._expression[this._parsePoint]))
				this._parsePoint++;
			return;
		}
		private void SkipSimpleStringChars()
		{
			while (this._parsePoint < this._expression.Length && CharacterUtilities.IsSimpleStringChar(this._expression[this._parsePoint]))
				this._parsePoint++;
			return;
		}
	}
}
