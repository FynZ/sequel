﻿/**
 * This class is heavily inspired by python-sqlparse
 * https://github.com/andialbrecht/sqlparse/blob/master/sqlparse/engine/statement_splitter.py
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sequel.Core.Parser
{
    public class Splitter
    {
        private int _level;
        private bool _isInCreate;
        private bool _isInDeclare;
        private int _beginDepth;

        public List<Statement> Process(string sql)
        {
            var statements = new List<Statement>();
            var tokens = Lexer.GetTokens(sql);

            var statement = new Statement();
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                _level += ChangeSplitLevel(token);
                token.Depth = _level;
                statement.Add(token);

                if (_level <= 0 && token.Type == TokenType.Punctuation && token.Text == ";")
                {
                    if (_isInDeclare)
                    { // End of a block DECLARE
                        _isInDeclare = false;
                    }
                    else
                    { // End of a statement
                        ConsumeWhitespace(ref i);
                        statements.Add(statement);

                        Reset();
                    }
                }
            }

            if (statement.Any())
            { // Incomplete statement
                statement.NeedsSemicolon = true;
                statements.Add(statement);
            }

            return statements;

            void ConsumeWhitespace(ref int i)
            { // Reads all tokens as long as they are whitespace or new line
                while (true)
                {
                    if (i + 1 >= tokens.Count || (tokens[i + 1].Type != TokenType.Whitespace && tokens[i + 1].Type != TokenType.Newline))
                    {
                        break;
                    }

                    i += 1;
                    statement.Add(tokens[i]);
                }
            }

            void Reset()
            { // Prepare to process next statement
                _level = 0;
                _isInCreate = false;
                _beginDepth = 0;
                statement = new Statement();
            }
        }

        private int ChangeSplitLevel(Token token)
        {
            // Parenthesis increase/decrease a level
            if (token.IsOpenParenthesis())
            {
                return 1;
            }
            else if (token.IsCloseParenthesis())
            {
                return -1;
            }
            else if (!token.IsKeyword())
            {
                return 0;
            }

            // Everything after here is TokenType = Keyword
            string upperText = token.Text.ToUpperInvariant();

            // Three keywords begin with CREATE, but only one of them is DDL
            // DDL Create though can contain more words such as "or replace"
            if (token.Type == TokenType.KeywordDDL && upperText.StartsWith("CREATE"))
            {
                _isInCreate = true;
                return 0;
            }

            // Can have nested declare inside of begin
            if (upperText == "DECLARE" && _isInCreate && _beginDepth == 0)
            {
                _isInDeclare = true;
                return 0;
            }

            if (upperText == "BEGIN")
            {
                _beginDepth += 1;
                return _isInCreate ? 1 : 0;
            }

            if (upperText == "END")
            {
                _beginDepth = Math.Max(0, _beginDepth - 1);
                return -1;
            }

            if (_isInCreate && _beginDepth > 0 && (upperText == "IF" || upperText == "FOR" || upperText == "WHILE" || upperText == "CASE"))
            {
                return 1;
            }

            if (upperText == "END IF" || upperText == "END FOR" || upperText == "END WHILE" )
            {
                return -1;
            }

            // Default
            return 0;
        }
    }
}
