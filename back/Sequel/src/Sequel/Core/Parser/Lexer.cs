﻿/**
 * This class is heavily inspired by python-sqlparse
 * https://github.com/andialbrecht/sqlparse/blob/master/sqlparse/keywords.py
 */

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sequel.Core.Parser
{
    public static class Lexer
    {
        public static List<Token> GetTokens(string? sql)
        {
            if (sql.IsNullOrEmpty())
            {
                return new List<Token>();
            }

            int startat = 0;
            var tokens = new List<Token>();

            while (startat < sql.Length)
            {
                Match? match = null;
                foreach (var rex in SqlRegexes)
                { // Complex tokens
                    match = rex.Regex.Match(sql, startat);
                    if (match.Success)
                    {
                        tokens.Add(new Token(rex.TokenType, match.Value, tokens.LastOrDefault()));
                        startat += match.Length;
                        break;
                    }
                }
                if (match?.Success == true)
                {
                    continue;
                }

                match = RegexKeyword.Match(sql, startat);
                if (match.Success)
                { // Simple keywords
                    var token = IsKeyword(match.Value, tokens.LastOrDefault());
                    if (token != null)
                    {
                        tokens.Add(token);
                        startat += match.Length;
                        continue;
                    }
                }

                // Fallback to TokenType.Name
                tokens.Add(new Token(TokenType.Name, match.Value, tokens.LastOrDefault()));
                startat += match.Length;
            }

            return tokens;
        }

        private static readonly List<SqlRegex> SqlRegexes = new List<SqlRegex>
        {
            new SqlRegex(@"(\G--|\G# )\+.*?(\r\n|\r|\n|$)", TokenType.CommentHint),
            new SqlRegex(@"(\G--|\G# ).*?(\r\n|\r|\n|$)", TokenType.Comment),
            new SqlRegex(@"\G\/\*[\s\S]*?\*\/", TokenType.CommentMultiline),
            new SqlRegex(@"(\G\r\n|\G\r|\G\n)", TokenType.Newline),
            new SqlRegex(@"\G( |\t)+", TokenType.Whitespace),
            new SqlRegex(@"\G:=", TokenType.Assignment),
            new SqlRegex(@"\G::", TokenType.Punctuation),
            new SqlRegex(@"\G\*", TokenType.Wildcard),
            new SqlRegex(@"\G`(``|[^`])*`", TokenType.Name),
            new SqlRegex(@"\G´(´´|[^´])*´", TokenType.Name),
            new SqlRegex(@"\G((?<!\S)\$(?:[_A-ZÀ-Ü]\w*)?\$)[\s\S]*?\1", TokenType.Literal),
            new SqlRegex(@"\G\?", TokenType.NamePlaceholder),
            new SqlRegex(@"\G\\\w+", TokenType.Command),
            new SqlRegex(@"\G(NOT\s+)?(IN)\b", TokenType.Comparison),
            new SqlRegex(@"\G(CASE|IN|VALUES|USING|FROM|AS)\b", TokenType.Keyword),
            new SqlRegex(@"\G(@|##|#)[A-ZÀ-Ü]\w+", TokenType.Name),
            new SqlRegex(@"\G[A-ZÀ-Ü]\w*(?=\s*\.)", TokenType.Name),
            new SqlRegex(@"\G(?<=\.)[A-ZÀ-Ü]\w*", TokenType.Name),
            new SqlRegex(@"\G[A-ZÀ-Ü]\w*(?=\()", TokenType.Name),
            new SqlRegex(@"\G-?0x[\dA-F]+", TokenType.NumberHexadecimal),
            new SqlRegex(@"\G-?\d*(\.\d+)?E-?\d+", TokenType.NumberFloat),
            new SqlRegex(@"\G(?![_A-ZÀ-Ü])-?(\d+(\.\d*)|\.\d+)(?![_A-ZÀ-Ü])", TokenType.NumberFloat),
            new SqlRegex(@"\G(?![_A-ZÀ-Ü])-?\d+(?![_A-ZÀ-Ü])", TokenType.NumberInteger),
            new SqlRegex(@"\G'(''|\\\\|\\'|[^'])*'", TokenType.StringSingle),
            new SqlRegex("\\G\"(\"\"|\\\\|\\\"|[^\"])*\"", TokenType.StringSymbol),
            new SqlRegex("\\G(\"\"|\".*?[^\\\\]\")", TokenType.StringSymbol),
            new SqlRegex(@"\G(?<![\w\])])(\[[^\]\[]+\])", TokenType.Name),
            new SqlRegex(@"\G((LEFT\s+|RIGHT\s+|FULL\s+)?(INNER\s+|OUTER\s+|STRAIGHT\s+)?|(CROSS\s+|NATURAL\s+)?)?JOIN\b", TokenType.Keyword),
            new SqlRegex(@"\GEND(\s+IF|\s+LOOP|\s+WHILE)?\b", TokenType.Keyword),
            new SqlRegex(@"\GNOT\s+NULL\b", TokenType.Keyword),
            new SqlRegex(@"\GNULLS\s+(FIRST|LAST)\b", TokenType.Keyword),
            new SqlRegex(@"\GUNION\s+ALL\b", TokenType.Keyword),
            new SqlRegex(@"\GCREATE(\s+OR\s+REPLACE)?\b", TokenType.KeywordDDL),
            new SqlRegex(@"\GDOUBLE\s+PRECISION\b", TokenType.NameBuiltin),
            new SqlRegex(@"\GGROUP\s+BY\b", TokenType.Keyword),
            new SqlRegex(@"\GORDER\s+BY\b", TokenType.Keyword),
            new SqlRegex(@"\GHANDLER\s+FOR\b", TokenType.Keyword),
            new SqlRegex(@"\GLATERAL\s+VIEW\b", TokenType.Keyword),
            new SqlRegex(@"\G(EXPLODE|INLINE|PARSE_URL_TUPLE|POSEXPLODE|STACK)\b", TokenType.Keyword),
            new SqlRegex(@"\G(AT|WITH')\s+TIME\s+ZONE\s+'[^']+'", TokenType.KeywordTZCast),
            new SqlRegex(@"\G(NOT\s+)?(LIKE|ILIKE|RLIKE)\b", TokenType.OperatorComparison),
            new SqlRegex(@"\G[;:()\[\],\.]", TokenType.Punctuation),
            new SqlRegex(@"\G[<>=~!]+", TokenType.OperatorComparison),
            new SqlRegex(@"\G[+/@#%^&|`?^-]+", TokenType.Operator),
        };

        private static readonly Regex RegexKeyword = new Regex(@"\G[0-9_A-ZÀ-Ü][_$#\w]*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static Token? IsKeyword(string value, Token? previousToken)
        {
            var upperValue = value.ToUpperInvariant();

            return !CommonKeywords.TryGetValue(upperValue, out TokenType tokenType)
                && !Keywords.TryGetValue(upperValue, out tokenType)
                && !PostgreSQLKeywords.TryGetValue(upperValue, out tokenType)
                ? null
                : new Token(tokenType, value, previousToken);
        }

        private static readonly Dictionary<string, TokenType> CommonKeywords = new Dictionary<string, TokenType>
        {
            ["SELECT"] = TokenType.KeywordDML,
            ["INSERT"] = TokenType.KeywordDML,
            ["DELETE"] = TokenType.KeywordDML,
            ["UPDATE"] = TokenType.KeywordDML,
            ["UPSERT"] = TokenType.KeywordDML,
            ["REPLACE"] = TokenType.KeywordDML,
            ["MERGE"] = TokenType.KeywordDML,
            ["DROP"] = TokenType.KeywordDDL,
            ["CREATE"] = TokenType.KeywordDDL,
            ["ALTER"] = TokenType.KeywordDDL,

            ["WHERE"] = TokenType.Keyword,
            ["FROM"] = TokenType.Keyword,
            ["INNER"] = TokenType.Keyword,
            ["JOIN"] = TokenType.Keyword,
            ["STRAIGHT_JOIN"] = TokenType.Keyword,
            ["AND"] = TokenType.Keyword,
            ["OR"] = TokenType.Keyword,
            ["LIKE"] = TokenType.Keyword,
            ["ON"] = TokenType.Keyword,
            ["IN"] = TokenType.Keyword,
            ["SET"] = TokenType.Keyword,

            ["BY"] = TokenType.Keyword,
            ["GROUP"] = TokenType.Keyword,
            ["ORDER"] = TokenType.Keyword,
            ["LEFT"] = TokenType.Keyword,
            ["OUTER"] = TokenType.Keyword,
            ["FULL"] = TokenType.Keyword,

            ["IF"] = TokenType.Keyword,
            ["END"] = TokenType.Keyword,
            ["THEN"] = TokenType.Keyword,
            ["LOOP"] = TokenType.Keyword,
            ["AS"] = TokenType.Keyword,
            ["ELSE"] = TokenType.Keyword,
            ["FOR"] = TokenType.Keyword,
            ["WHILE"] = TokenType.Keyword,

            ["CASE"] = TokenType.Keyword,
            ["WHEN"] = TokenType.Keyword,
            ["MIN"] = TokenType.Keyword,
            ["MAX"] = TokenType.Keyword,
            ["DISTINCT"] = TokenType.Keyword,
        };

        private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
        {
            ["ABORT"] = TokenType.Keyword,
            ["ABS"] = TokenType.Keyword,
            ["ABSOLUTE"] = TokenType.Keyword,
            ["ADA"] = TokenType.Keyword,
            ["ADD"] = TokenType.Keyword,
            ["ADMIN"] = TokenType.Keyword,
            ["AFTER"] = TokenType.Keyword,
            ["AGGREGATE"] = TokenType.Keyword,
            ["ALIAS"] = TokenType.Keyword,
            ["ALL"] = TokenType.Keyword,
            ["ALLOCATE"] = TokenType.Keyword,
            ["ANALYSE"] = TokenType.Keyword,
            ["ANALYZE"] = TokenType.Keyword,
            ["ANY"] = TokenType.Keyword,
            ["ARRAYLEN"] = TokenType.Keyword,
            ["ARE"] = TokenType.Keyword,
            ["ASC"] = TokenType.KeywordOrder,
            ["ASENSITIVE"] = TokenType.Keyword,
            ["ASSERTION"] = TokenType.Keyword,
            ["ASSIGNMENT"] = TokenType.Keyword,
            ["ASYMMETRIC"] = TokenType.Keyword,
            ["AT"] = TokenType.Keyword,
            ["ATOMIC"] = TokenType.Keyword,
            ["AUDIT"] = TokenType.Keyword,
            ["AUTHORIZATION"] = TokenType.Keyword,
            ["AUTO_INCREMENT"] = TokenType.Keyword,
            ["AVG"] = TokenType.Keyword,

            ["BACKWARD"] = TokenType.Keyword,
            ["BEFORE"] = TokenType.Keyword,
            ["BEGIN"] = TokenType.Keyword,
            ["BETWEEN"] = TokenType.Keyword,
            ["BITVAR"] = TokenType.Keyword,
            ["BIT_LENGTH"] = TokenType.Keyword,
            ["BOTH"] = TokenType.Keyword,
            ["BREADTH"] = TokenType.Keyword,

            ["CACHE"] = TokenType.Keyword,
            ["CALL"] = TokenType.Keyword,
            ["CALLED"] = TokenType.Keyword,
            ["CARDINALITY"] = TokenType.Keyword,
            ["CASCADE"] = TokenType.Keyword,
            ["CASCADED"] = TokenType.Keyword,
            ["CAST"] = TokenType.Keyword,
            ["CATALOG"] = TokenType.Keyword,
            ["CATALOG_NAME"] = TokenType.Keyword,
            ["CHAIN"] = TokenType.Keyword,
            ["CHARACTERISTICS"] = TokenType.Keyword,
            ["CHARACTER_LENGTH"] = TokenType.Keyword,
            ["CHARACTER_SET_CATALOG"] = TokenType.Keyword,
            ["CHARACTER_SET_NAME"] = TokenType.Keyword,
            ["CHARACTER_SET_SCHEMA"] = TokenType.Keyword,
            ["CHAR_LENGTH"] = TokenType.Keyword,
            ["CHARSET"] = TokenType.Keyword,
            ["CHECK"] = TokenType.Keyword,
            ["CHECKED"] = TokenType.Keyword,
            ["CHECKPOINT"] = TokenType.Keyword,
            ["CLASS"] = TokenType.Keyword,
            ["CLASS_ORIGIN"] = TokenType.Keyword,
            ["CLOB"] = TokenType.Keyword,
            ["CLOSE"] = TokenType.Keyword,
            ["CLUSTER"] = TokenType.Keyword,
            ["COALESCE"] = TokenType.Keyword,
            ["COBOL"] = TokenType.Keyword,
            ["COLLATE"] = TokenType.Keyword,
            ["COLLATION"] = TokenType.Keyword,
            ["COLLATION_CATALOG"] = TokenType.Keyword,
            ["COLLATION_NAME"] = TokenType.Keyword,
            ["COLLATION_SCHEMA"] = TokenType.Keyword,
            ["COLLECT"] = TokenType.Keyword,
            ["COLUMN"] = TokenType.Keyword,
            ["COLUMN_NAME"] = TokenType.Keyword,
            ["COMPRESS"] = TokenType.Keyword,
            ["COMMAND_FUNCTION"] = TokenType.Keyword,
            ["COMMAND_FUNCTION_CODE"] = TokenType.Keyword,
            ["COMMENT"] = TokenType.Keyword,
            ["COMMIT"] = TokenType.KeywordDML,
            ["COMMITTED"] = TokenType.Keyword,
            ["COMPLETION"] = TokenType.Keyword,
            ["CONCURRENTLY"] = TokenType.Keyword,
            ["CONDITION_NUMBER"] = TokenType.Keyword,
            ["CONNECT"] = TokenType.Keyword,
            ["CONNECTION"] = TokenType.Keyword,
            ["CONNECTION_NAME"] = TokenType.Keyword,
            ["CONSTRAINT"] = TokenType.Keyword,
            ["CONSTRAINTS"] = TokenType.Keyword,
            ["CONSTRAINT_CATALOG"] = TokenType.Keyword,
            ["CONSTRAINT_NAME"] = TokenType.Keyword,
            ["CONSTRAINT_SCHEMA"] = TokenType.Keyword,
            ["CONSTRUCTOR"] = TokenType.Keyword,
            ["CONTAINS"] = TokenType.Keyword,
            ["CONTINUE"] = TokenType.Keyword,
            ["CONVERSION"] = TokenType.Keyword,
            ["CONVERT"] = TokenType.Keyword,
            ["COPY"] = TokenType.Keyword,
            ["CORRESPONDING"] = TokenType.Keyword,
            ["COUNT"] = TokenType.Keyword,
            ["CREATEDB"] = TokenType.Keyword,
            ["CREATEUSER"] = TokenType.Keyword,
            ["CROSS"] = TokenType.Keyword,
            ["CUBE"] = TokenType.Keyword,
            ["CURRENT"] = TokenType.Keyword,
            ["CURRENT_DATE"] = TokenType.Keyword,
            ["CURRENT_PATH"] = TokenType.Keyword,
            ["CURRENT_ROLE"] = TokenType.Keyword,
            ["CURRENT_TIME"] = TokenType.Keyword,
            ["CURRENT_TIMESTAMP"] = TokenType.Keyword,
            ["CURRENT_USER"] = TokenType.Keyword,
            ["CURSOR"] = TokenType.Keyword,
            ["CURSOR_NAME"] = TokenType.Keyword,
            ["CYCLE"] = TokenType.Keyword,

            ["DATA"] = TokenType.Keyword,
            ["DATABASE"] = TokenType.Keyword,
            ["DATETIME_INTERVAL_CODE"] = TokenType.Keyword,
            ["DATETIME_INTERVAL_PRECISION"] = TokenType.Keyword,
            ["DAY"] = TokenType.Keyword,
            ["DEALLOCATE"] = TokenType.Keyword,
            ["DECLARE"] = TokenType.Keyword,
            ["DEFAULT"] = TokenType.Keyword,
            ["DEFAULTS"] = TokenType.Keyword,
            ["DEFERRABLE"] = TokenType.Keyword,
            ["DEFERRED"] = TokenType.Keyword,
            ["DEFINED"] = TokenType.Keyword,
            ["DEFINER"] = TokenType.Keyword,
            ["DELIMITER"] = TokenType.Keyword,
            ["DELIMITERS"] = TokenType.Keyword,
            ["DEREF"] = TokenType.Keyword,
            ["DESC"] = TokenType.KeywordOrder,
            ["DESCRIBE"] = TokenType.Keyword,
            ["DESCRIPTOR"] = TokenType.Keyword,
            ["DESTROY"] = TokenType.Keyword,
            ["DESTRUCTOR"] = TokenType.Keyword,
            ["DETERMINISTIC"] = TokenType.Keyword,
            ["DIAGNOSTICS"] = TokenType.Keyword,
            ["DICTIONARY"] = TokenType.Keyword,
            ["DISABLE"] = TokenType.Keyword,
            ["DISCONNECT"] = TokenType.Keyword,
            ["DISPATCH"] = TokenType.Keyword,
            ["DO"] = TokenType.Keyword,
            ["DOMAIN"] = TokenType.Keyword,
            ["DYNAMIC"] = TokenType.Keyword,
            ["DYNAMIC_FUNCTION"] = TokenType.Keyword,
            ["DYNAMIC_FUNCTION_CODE"] = TokenType.Keyword,

            ["EACH"] = TokenType.Keyword,
            ["ENABLE"] = TokenType.Keyword,
            ["ENCODING"] = TokenType.Keyword,
            ["ENCRYPTED"] = TokenType.Keyword,
            ["END-EXEC"] = TokenType.Keyword,
            ["ENGINE"] = TokenType.Keyword,
            ["EQUALS"] = TokenType.Keyword,
            ["ESCAPE"] = TokenType.Keyword,
            ["EVERY"] = TokenType.Keyword,
            ["EXCEPT"] = TokenType.Keyword,
            ["EXCEPTION"] = TokenType.Keyword,
            ["EXCLUDING"] = TokenType.Keyword,
            ["EXCLUSIVE"] = TokenType.Keyword,
            ["EXEC"] = TokenType.Keyword,
            ["EXECUTE"] = TokenType.Keyword,
            ["EXISTING"] = TokenType.Keyword,
            ["EXISTS"] = TokenType.Keyword,
            ["EXPLAIN"] = TokenType.Keyword,
            ["EXTERNAL"] = TokenType.Keyword,
            ["EXTRACT"] = TokenType.Keyword,

            ["FALSE"] = TokenType.Keyword,
            ["FETCH"] = TokenType.Keyword,
            ["FILE"] = TokenType.Keyword,
            ["FINAL"] = TokenType.Keyword,
            ["FIRST"] = TokenType.Keyword,
            ["FORCE"] = TokenType.Keyword,
            ["FOREACH"] = TokenType.Keyword,
            ["FOREIGN"] = TokenType.Keyword,
            ["FORTRAN"] = TokenType.Keyword,
            ["FORWARD"] = TokenType.Keyword,
            ["FOUND"] = TokenType.Keyword,
            ["FREE"] = TokenType.Keyword,
            ["FREEZE"] = TokenType.Keyword,
            ["FULL"] = TokenType.Keyword,
            ["FUNCTION"] = TokenType.Keyword,

            ["GENERAL"] = TokenType.Keyword,
            ["GENERATED"] = TokenType.Keyword,
            ["GET"] = TokenType.Keyword,
            ["GLOBAL"] = TokenType.Keyword,
            ["GO"] = TokenType.Keyword,
            ["GOTO"] = TokenType.Keyword,
            ["GRANT"] = TokenType.Keyword,
            ["GRANTED"] = TokenType.Keyword,
            ["GROUPING"] = TokenType.Keyword,

            ["HAVING"] = TokenType.Keyword,
            ["HIERARCHY"] = TokenType.Keyword,
            ["HOLD"] = TokenType.Keyword,
            ["HOUR"] = TokenType.Keyword,
            ["HOST"] = TokenType.Keyword,

            ["IDENTIFIED"] = TokenType.Keyword,
            ["IDENTITY"] = TokenType.Keyword,
            ["IGNORE"] = TokenType.Keyword,
            ["ILIKE"] = TokenType.Keyword,
            ["IMMEDIATE"] = TokenType.Keyword,
            ["IMMUTABLE"] = TokenType.Keyword,

            ["IMPLEMENTATION"] = TokenType.Keyword,
            ["IMPLICIT"] = TokenType.Keyword,
            ["INCLUDING"] = TokenType.Keyword,
            ["INCREMENT"] = TokenType.Keyword,
            ["INDEX"] = TokenType.Keyword,

            ["INDITCATOR"] = TokenType.Keyword,
            ["INFIX"] = TokenType.Keyword,
            ["INHERITS"] = TokenType.Keyword,
            ["INITIAL"] = TokenType.Keyword,
            ["INITIALIZE"] = TokenType.Keyword,
            ["INITIALLY"] = TokenType.Keyword,
            ["INOUT"] = TokenType.Keyword,
            ["INPUT"] = TokenType.Keyword,
            ["INSENSITIVE"] = TokenType.Keyword,
            ["INSTANTIABLE"] = TokenType.Keyword,
            ["INSTEAD"] = TokenType.Keyword,
            ["INTERSECT"] = TokenType.Keyword,
            ["INTO"] = TokenType.Keyword,
            ["INVOKER"] = TokenType.Keyword,
            ["IS"] = TokenType.Keyword,
            ["ISNULL"] = TokenType.Keyword,
            ["ISOLATION"] = TokenType.Keyword,
            ["ITERATE"] = TokenType.Keyword,

            ["KEY"] = TokenType.Keyword,
            ["KEY_MEMBER"] = TokenType.Keyword,
            ["KEY_TYPE"] = TokenType.Keyword,

            ["LANCOMPILER"] = TokenType.Keyword,
            ["LANGUAGE"] = TokenType.Keyword,
            ["LARGE"] = TokenType.Keyword,
            ["LAST"] = TokenType.Keyword,
            ["LATERAL"] = TokenType.Keyword,
            ["LEADING"] = TokenType.Keyword,
            ["LENGTH"] = TokenType.Keyword,
            ["LESS"] = TokenType.Keyword,
            ["LEVEL"] = TokenType.Keyword,
            ["LIMIT"] = TokenType.Keyword,
            ["LISTEN"] = TokenType.Keyword,
            ["LOAD"] = TokenType.Keyword,
            ["LOCAL"] = TokenType.Keyword,
            ["LOCALTIME"] = TokenType.Keyword,
            ["LOCALTIMESTAMP"] = TokenType.Keyword,
            ["LOCATION"] = TokenType.Keyword,
            ["LOCATOR"] = TokenType.Keyword,
            ["LOCK"] = TokenType.Keyword,
            ["LOWER"] = TokenType.Keyword,

            ["MAP"] = TokenType.Keyword,
            ["MATCH"] = TokenType.Keyword,
            ["MAXEXTENTS"] = TokenType.Keyword,
            ["MAXVALUE"] = TokenType.Keyword,
            ["MESSAGE_LENGTH"] = TokenType.Keyword,
            ["MESSAGE_OCTET_LENGTH"] = TokenType.Keyword,
            ["MESSAGE_TEXT"] = TokenType.Keyword,
            ["METHOD"] = TokenType.Keyword,
            ["MINUTE"] = TokenType.Keyword,
            ["MINUS"] = TokenType.Keyword,
            ["MINVALUE"] = TokenType.Keyword,
            ["MOD"] = TokenType.Keyword,
            ["MODE"] = TokenType.Keyword,
            ["MODIFIES"] = TokenType.Keyword,
            ["MODIFY"] = TokenType.Keyword,
            ["MONTH"] = TokenType.Keyword,
            ["MORE"] = TokenType.Keyword,
            ["MOVE"] = TokenType.Keyword,
            ["MUMPS"] = TokenType.Keyword,

            ["NAMES"] = TokenType.Keyword,
            ["NATIONAL"] = TokenType.Keyword,
            ["NATURAL"] = TokenType.Keyword,
            ["NCHAR"] = TokenType.Keyword,
            ["NCLOB"] = TokenType.Keyword,
            ["NEW"] = TokenType.Keyword,
            ["NEXT"] = TokenType.Keyword,
            ["NO"] = TokenType.Keyword,
            ["NOAUDIT"] = TokenType.Keyword,
            ["NOCOMPRESS"] = TokenType.Keyword,
            ["NOCREATEDB"] = TokenType.Keyword,
            ["NOCREATEUSER"] = TokenType.Keyword,
            ["NONE"] = TokenType.Keyword,
            ["NOT"] = TokenType.Keyword,
            ["NOTFOUND"] = TokenType.Keyword,
            ["NOTHING"] = TokenType.Keyword,
            ["NOTIFY"] = TokenType.Keyword,
            ["NOTNULL"] = TokenType.Keyword,
            ["NOWAIT"] = TokenType.Keyword,
            ["NULL"] = TokenType.Keyword,
            ["NULLABLE"] = TokenType.Keyword,
            ["NULLIF"] = TokenType.Keyword,

            ["OBJECT"] = TokenType.Keyword,
            ["OCTET_LENGTH"] = TokenType.Keyword,
            ["OF"] = TokenType.Keyword,
            ["OFF"] = TokenType.Keyword,
            ["OFFLINE"] = TokenType.Keyword,
            ["OFFSET"] = TokenType.Keyword,
            ["OIDS"] = TokenType.Keyword,
            ["OLD"] = TokenType.Keyword,
            ["ONLINE"] = TokenType.Keyword,
            ["ONLY"] = TokenType.Keyword,
            ["OPEN"] = TokenType.Keyword,
            ["OPERATION"] = TokenType.Keyword,
            ["OPERATOR"] = TokenType.Keyword,
            ["OPTION"] = TokenType.Keyword,
            ["OPTIONS"] = TokenType.Keyword,
            ["ORDINALITY"] = TokenType.Keyword,
            ["OUT"] = TokenType.Keyword,
            ["OUTPUT"] = TokenType.Keyword,
            ["OVERLAPS"] = TokenType.Keyword,
            ["OVERLAY"] = TokenType.Keyword,
            ["OVERRIDING"] = TokenType.Keyword,
            ["OWNER"] = TokenType.Keyword,

            ["QUARTER"] = TokenType.Keyword,

            ["PAD"] = TokenType.Keyword,
            ["PARAMETER"] = TokenType.Keyword,
            ["PARAMETERS"] = TokenType.Keyword,
            ["PARAMETER_MODE"] = TokenType.Keyword,
            ["PARAMETER_NAME"] = TokenType.Keyword,
            ["PARAMETER_ORDINAL_POSITION"] = TokenType.Keyword,
            ["PARAMETER_SPECIFIC_CATALOG"] = TokenType.Keyword,
            ["PARAMETER_SPECIFIC_NAME"] = TokenType.Keyword,
            ["PARAMETER_SPECIFIC_SCHEMA"] = TokenType.Keyword,
            ["PARTIAL"] = TokenType.Keyword,
            ["PASCAL"] = TokenType.Keyword,
            ["PCTFREE"] = TokenType.Keyword,
            ["PENDANT"] = TokenType.Keyword,
            ["PLACING"] = TokenType.Keyword,
            ["PLI"] = TokenType.Keyword,
            ["POSITION"] = TokenType.Keyword,
            ["POSTFIX"] = TokenType.Keyword,
            ["PRECISION"] = TokenType.Keyword,
            ["PREFIX"] = TokenType.Keyword,
            ["PREORDER"] = TokenType.Keyword,
            ["PREPARE"] = TokenType.Keyword,
            ["PRESERVE"] = TokenType.Keyword,
            ["PRIMARY"] = TokenType.Keyword,
            ["PRIOR"] = TokenType.Keyword,
            ["PRIVILEGES"] = TokenType.Keyword,
            ["PROCEDURAL"] = TokenType.Keyword,
            ["PROCEDURE"] = TokenType.Keyword,
            ["PUBLIC"] = TokenType.Keyword,

            ["RAISE"] = TokenType.Keyword,
            ["RAW"] = TokenType.Keyword,
            ["READ"] = TokenType.Keyword,
            ["READS"] = TokenType.Keyword,
            ["RECHECK"] = TokenType.Keyword,
            ["RECURSIVE"] = TokenType.Keyword,
            ["REF"] = TokenType.Keyword,
            ["REFERENCES"] = TokenType.Keyword,
            ["REFERENCING"] = TokenType.Keyword,
            ["REINDEX"] = TokenType.Keyword,
            ["RELATIVE"] = TokenType.Keyword,
            ["RENAME"] = TokenType.Keyword,
            ["REPEATABLE"] = TokenType.Keyword,
            ["RESET"] = TokenType.Keyword,
            ["RESOURCE"] = TokenType.Keyword,
            ["RESTART"] = TokenType.Keyword,
            ["RESTRICT"] = TokenType.Keyword,
            ["RESULT"] = TokenType.Keyword,
            ["RETURN"] = TokenType.Keyword,
            ["RETURNED_LENGTH"] = TokenType.Keyword,
            ["RETURNED_OCTET_LENGTH"] = TokenType.Keyword,
            ["RETURNED_SQLSTATE"] = TokenType.Keyword,
            ["RETURNING"] = TokenType.Keyword,
            ["RETURNS"] = TokenType.Keyword,
            ["REVOKE"] = TokenType.Keyword,
            ["RIGHT"] = TokenType.Keyword,
            ["ROLE"] = TokenType.Keyword,
            ["ROLLBACK"] = TokenType.KeywordDML,
            ["ROLLUP"] = TokenType.Keyword,
            ["ROUTINE"] = TokenType.Keyword,
            ["ROUTINE_CATALOG"] = TokenType.Keyword,
            ["ROUTINE_NAME"] = TokenType.Keyword,
            ["ROUTINE_SCHEMA"] = TokenType.Keyword,
            ["ROW"] = TokenType.Keyword,
            ["ROWS"] = TokenType.Keyword,
            ["ROW_COUNT"] = TokenType.Keyword,
            ["RULE"] = TokenType.Keyword,

            ["SAVE_POINT"] = TokenType.Keyword,
            ["SCALE"] = TokenType.Keyword,
            ["SCHEMA"] = TokenType.Keyword,
            ["SCHEMA_NAME"] = TokenType.Keyword,
            ["SCOPE"] = TokenType.Keyword,
            ["SCROLL"] = TokenType.Keyword,
            ["SEARCH"] = TokenType.Keyword,
            ["SECOND"] = TokenType.Keyword,
            ["SECURITY"] = TokenType.Keyword,
            ["SELF"] = TokenType.Keyword,
            ["SENSITIVE"] = TokenType.Keyword,
            ["SEQUENCE"] = TokenType.Keyword,
            ["SERIALIZABLE"] = TokenType.Keyword,
            ["SERVER_NAME"] = TokenType.Keyword,
            ["SESSION"] = TokenType.Keyword,
            ["SESSION_USER"] = TokenType.Keyword,
            ["SETOF"] = TokenType.Keyword,
            ["SETS"] = TokenType.Keyword,
            ["SHARE"] = TokenType.Keyword,
            ["SHOW"] = TokenType.Keyword,
            ["SIMILAR"] = TokenType.Keyword,
            ["SIMPLE"] = TokenType.Keyword,
            ["SIZE"] = TokenType.Keyword,
            ["SOME"] = TokenType.Keyword,
            ["SOURCE"] = TokenType.Keyword,
            ["SPACE"] = TokenType.Keyword,
            ["SPECIFIC"] = TokenType.Keyword,
            ["SPECIFICTYPE"] = TokenType.Keyword,
            ["SPECIFIC_NAME"] = TokenType.Keyword,
            ["SQL"] = TokenType.Keyword,
            ["SQLBUF"] = TokenType.Keyword,
            ["SQLCODE"] = TokenType.Keyword,
            ["SQLERROR"] = TokenType.Keyword,
            ["SQLEXCEPTION"] = TokenType.Keyword,
            ["SQLSTATE"] = TokenType.Keyword,
            ["SQLWARNING"] = TokenType.Keyword,
            ["STABLE"] = TokenType.Keyword,
            ["START"] = TokenType.KeywordDML,
            ["STATEMENT"] = TokenType.Keyword,
            ["STATIC"] = TokenType.Keyword,
            ["STATISTICS"] = TokenType.Keyword,
            ["STDIN"] = TokenType.Keyword,
            ["STDOUT"] = TokenType.Keyword,
            ["STORAGE"] = TokenType.Keyword,
            ["STRICT"] = TokenType.Keyword,
            ["STRUCTURE"] = TokenType.Keyword,
            ["STYPE"] = TokenType.Keyword,
            ["SUBCLASS_ORIGIN"] = TokenType.Keyword,
            ["SUBLIST"] = TokenType.Keyword,
            ["SUBSTRING"] = TokenType.Keyword,
            ["SUCCESSFUL"] = TokenType.Keyword,
            ["SUM"] = TokenType.Keyword,
            ["SYMMETRIC"] = TokenType.Keyword,
            ["SYNONYM"] = TokenType.Keyword,
            ["SYSID"] = TokenType.Keyword,
            ["SYSTEM"] = TokenType.Keyword,
            ["SYSTEM_USER"] = TokenType.Keyword,

            ["TABLE"] = TokenType.Keyword,
            ["TABLE_NAME"] = TokenType.Keyword,
            ["TEMP"] = TokenType.Keyword,
            ["TEMPLATE"] = TokenType.Keyword,
            ["TEMPORARY"] = TokenType.Keyword,
            ["TERMINATE"] = TokenType.Keyword,
            ["THAN"] = TokenType.Keyword,
            ["TIMESTAMP"] = TokenType.Keyword,
            ["TIMEZONE_HOUR"] = TokenType.Keyword,
            ["TIMEZONE_MINUTE"] = TokenType.Keyword,
            ["TO"] = TokenType.Keyword,
            ["TOAST"] = TokenType.Keyword,
            ["TRAILING"] = TokenType.Keyword,
            ["TRANSATION"] = TokenType.Keyword,
            ["TRANSACTIONS_COMMITTED"] = TokenType.Keyword,
            ["TRANSACTIONS_ROLLED_BACK"] = TokenType.Keyword,
            ["TRANSATION_ACTIVE"] = TokenType.Keyword,
            ["TRANSFORM"] = TokenType.Keyword,
            ["TRANSFORMS"] = TokenType.Keyword,
            ["TRANSLATE"] = TokenType.Keyword,
            ["TRANSLATION"] = TokenType.Keyword,
            ["TREAT"] = TokenType.Keyword,
            ["TRIGGER"] = TokenType.Keyword,
            ["TRIGGER_CATALOG"] = TokenType.Keyword,
            ["TRIGGER_NAME"] = TokenType.Keyword,
            ["TRIGGER_SCHEMA"] = TokenType.Keyword,
            ["TRIM"] = TokenType.Keyword,
            ["TRUE"] = TokenType.Keyword,
            ["TRUNCATE"] = TokenType.Keyword,
            ["TRUSTED"] = TokenType.Keyword,
            ["TYPE"] = TokenType.Keyword,

            ["UID"] = TokenType.Keyword,
            ["UNCOMMITTED"] = TokenType.Keyword,
            ["UNDER"] = TokenType.Keyword,
            ["UNENCRYPTED"] = TokenType.Keyword,
            ["UNION"] = TokenType.Keyword,
            ["UNIQUE"] = TokenType.Keyword,
            ["UNKNOWN"] = TokenType.Keyword,
            ["UNLISTEN"] = TokenType.Keyword,
            ["UNNAMED"] = TokenType.Keyword,
            ["UNNEST"] = TokenType.Keyword,
            ["UNTIL"] = TokenType.Keyword,
            ["UPPER"] = TokenType.Keyword,
            ["USAGE"] = TokenType.Keyword,
            ["USE"] = TokenType.Keyword,
            ["USER"] = TokenType.Keyword,
            ["USER_DEFINED_TYPE_CATALOG"] = TokenType.Keyword,
            ["USER_DEFINED_TYPE_NAME"] = TokenType.Keyword,
            ["USER_DEFINED_TYPE_SCHEMA"] = TokenType.Keyword,
            ["USING"] = TokenType.Keyword,

            ["VACUUM"] = TokenType.Keyword,
            ["VALID"] = TokenType.Keyword,
            ["VALIDATE"] = TokenType.Keyword,
            ["VALIDATOR"] = TokenType.Keyword,
            ["VALUES"] = TokenType.Keyword,
            ["VARIABLE"] = TokenType.Keyword,
            ["VERBOSE"] = TokenType.Keyword,
            ["VERSION"] = TokenType.Keyword,
            ["VIEW"] = TokenType.Keyword,
            ["VOLATILE"] = TokenType.Keyword,

            ["WEEK"] = TokenType.Keyword,
            ["WHENEVER"] = TokenType.Keyword,
            ["WITH"] = TokenType.KeywordCTE,
            ["WITHOUT"] = TokenType.Keyword,
            ["WORK"] = TokenType.Keyword,
            ["WRITE"] = TokenType.Keyword,

            ["YEAR"] = TokenType.Keyword,

            ["ZONE"] = TokenType.Keyword,

            ["ARRAY"] = TokenType.NameBuiltin,
            ["BIGINT"] = TokenType.NameBuiltin,
            ["BINARY"] = TokenType.NameBuiltin,
            ["BIT"] = TokenType.NameBuiltin,
            ["BLOB"] = TokenType.NameBuiltin,
            ["BOOLEAN"] = TokenType.NameBuiltin,
            ["CHAR"] = TokenType.NameBuiltin,
            ["CHARACTER"] = TokenType.NameBuiltin,
            ["DATE"] = TokenType.NameBuiltin,
            ["DEC"] = TokenType.NameBuiltin,
            ["DECIMAL"] = TokenType.NameBuiltin,
            ["FILE_TYPE"] = TokenType.NameBuiltin,
            ["FLOAT"] = TokenType.NameBuiltin,
            ["INT"] = TokenType.NameBuiltin,
            ["INT8"] = TokenType.NameBuiltin,
            ["INTEGER"] = TokenType.NameBuiltin,
            ["INTERVAL"] = TokenType.NameBuiltin,
            ["LONG"] = TokenType.NameBuiltin,
            ["NATURALN"] = TokenType.NameBuiltin,
            ["NVARCHAR"] = TokenType.NameBuiltin,
            ["NUMBER"] = TokenType.NameBuiltin,
            ["NUMERIC"] = TokenType.NameBuiltin,
            ["PLS_INTEGER"] = TokenType.NameBuiltin,
            ["POSITIVE"] = TokenType.NameBuiltin,
            ["POSITIVEN"] = TokenType.NameBuiltin,
            ["REAL"] = TokenType.NameBuiltin,
            ["ROWID"] = TokenType.NameBuiltin,
            ["ROWLABEL"] = TokenType.NameBuiltin,
            ["ROWNUM"] = TokenType.NameBuiltin,
            ["SERIAL"] = TokenType.NameBuiltin,
            ["SERIAL8"] = TokenType.NameBuiltin,
            ["SIGNED"] = TokenType.NameBuiltin,
            ["SIGNTYPE"] = TokenType.NameBuiltin,
            ["SIMPLE_DOUBLE"] = TokenType.NameBuiltin,
            ["SIMPLE_FLOAT"] = TokenType.NameBuiltin,
            ["SIMPLE_INTEGER"] = TokenType.NameBuiltin,
            ["SMALLINT"] = TokenType.NameBuiltin,
            ["SYS_REFCURSOR"] = TokenType.NameBuiltin,
            ["SYSDATE"] = TokenType.Name,
            ["TEXT"] = TokenType.NameBuiltin,
            ["TINYINT"] = TokenType.NameBuiltin,
            ["UNSIGNED"] = TokenType.NameBuiltin,
            ["UROWID"] = TokenType.NameBuiltin,
            ["UTL_FILE"] = TokenType.NameBuiltin,
            ["VARCHAR"] = TokenType.NameBuiltin,
            ["VARCHAR2"] = TokenType.NameBuiltin,
            ["VARYING"] = TokenType.NameBuiltin,
        };

        private static readonly Dictionary<string, TokenType> PostgreSQLKeywords = new Dictionary<string, TokenType>
        {
            ["WINDOW"] = TokenType.Keyword,
            ["PARTITION"] = TokenType.Keyword,
            ["OVER"] = TokenType.Keyword,
            ["PERFORM"] = TokenType.Keyword,
            ["NOTICE"] = TokenType.Keyword,
            ["PLPGSQL"] = TokenType.Keyword,
            ["INHERIT"] = TokenType.Keyword,
            ["INDEXES"] = TokenType.Keyword,

            ["BYTEA"] = TokenType.Keyword,
            ["BIGSERIAL"] = TokenType.Keyword,
            ["BIT VARYING"] = TokenType.Keyword,
            ["BOX"] = TokenType.Keyword,
            ["CHARACTER"] = TokenType.Keyword,
            ["CHARACTER VARYING"] = TokenType.Keyword,
            ["CIDR"] = TokenType.Keyword,
            ["CIRCLE"] = TokenType.Keyword,
            ["DOUBLE PRECISION"] = TokenType.Keyword,
            ["INET"] = TokenType.Keyword,
            ["JSON"] = TokenType.Keyword,
            ["JSONB"] = TokenType.Keyword,
            ["LINE"] = TokenType.Keyword,
            ["LSEG"] = TokenType.Keyword,
            ["MACADDR"] = TokenType.Keyword,
            ["MONEY"] = TokenType.Keyword,
            ["PATH"] = TokenType.Keyword,
            ["PG_LSN"] = TokenType.Keyword,
            ["POINT"] = TokenType.Keyword,
            ["POLYGON"] = TokenType.Keyword,
            ["SMALLSERIAL"] = TokenType.Keyword,
            ["TSQUERY"] = TokenType.Keyword,
            ["TSVECTOR"] = TokenType.Keyword,
            ["TXID_SNAPSHOT"] = TokenType.Keyword,
            ["UUID"] = TokenType.Keyword,
            ["XML"] = TokenType.Keyword,

            ["FOR"] = TokenType.Keyword,
            ["IN"] = TokenType.Keyword,
            ["LOOP"] = TokenType.Keyword,
        };

/*
        private static readonly Dictionary<string, TokenType> OracleKeywords = new Dictionary<string, TokenType>
        {
            ["ARCHIVE"] = TokenType.Keyword,
            ["ARCHIVELOG"] = TokenType.Keyword,

            ["BACKUP"] = TokenType.Keyword,
            ["BECOME"] = TokenType.Keyword,
            ["BLOCK"] = TokenType.Keyword,
            ["BODY"] = TokenType.Keyword,

            ["CANCEL"] = TokenType.Keyword,
            ["CHANGE"] = TokenType.Keyword,
            ["COMPILE"] = TokenType.Keyword,
            ["CONTENTS"] = TokenType.Keyword,
            ["CONTROLFILE"] = TokenType.Keyword,

            ["DATAFILE"] = TokenType.Keyword,
            ["DBA"] = TokenType.Keyword,
            ["DISMOUNT"] = TokenType.Keyword,
            ["DOUBLE"] = TokenType.Keyword,
            ["DUMP"] = TokenType.Keyword,

            ["EVENTS"] = TokenType.Keyword,
            ["EXCEPTIONS"] = TokenType.Keyword,
            ["EXPLAIN"] = TokenType.Keyword,
            ["EXTENT"] = TokenType.Keyword,
            ["EXTERNALLY"] = TokenType.Keyword,

            ["FLUSH"] = TokenType.Keyword,
            ["FREELIST"] = TokenType.Keyword,
            ["FREELISTS"] = TokenType.Keyword,

            ["INDICATOR"] = TokenType.Keyword,
            ["INITRANS"] = TokenType.Keyword,
            ["INSTANCE"] = TokenType.Keyword,

            ["LAYER"] = TokenType.Keyword,
            ["LINK"] = TokenType.Keyword,
            ["LISTS"] = TokenType.Keyword,
            ["LOGFILE"] = TokenType.Keyword,

            ["MANAGE"] = TokenType.Keyword,
            ["MANUAL"] = TokenType.Keyword,
            ["MAXDATAFILES"] = TokenType.Keyword,
            ["MAXINSTANCES"] = TokenType.Keyword,
            ["MAXLOGFILES"] = TokenType.Keyword,
            ["MAXLOGHISTORY"] = TokenType.Keyword,
            ["MAXLOGMEMBERS"] = TokenType.Keyword,
            ["MAXTRANS"] = TokenType.Keyword,
            ["MINEXTENTS"] = TokenType.Keyword,
            ["MODULE"] = TokenType.Keyword,
            ["MOUNT"] = TokenType.Keyword,

            ["NOARCHIVELOG"] = TokenType.Keyword,
            ["NOCACHE"] = TokenType.Keyword,
            ["NOCYCLE"] = TokenType.Keyword,
            ["NOMAXVALUE"] = TokenType.Keyword,
            ["NOMINVALUE"] = TokenType.Keyword,
            ["NOORDER"] = TokenType.Keyword,
            ["NORESETLOGS"] = TokenType.Keyword,
            ["NORMAL"] = TokenType.Keyword,
            ["NOSORT"] = TokenType.Keyword,

            ["OPTIMAL"] = TokenType.Keyword,
            ["OWN"] = TokenType.Keyword,

            ["PACKAGE"] = TokenType.Keyword,
            ["PARALLEL"] = TokenType.Keyword,
            ["PCTINCREASE"] = TokenType.Keyword,
            ["PCTUSED"] = TokenType.Keyword,
            ["PLAN"] = TokenType.Keyword,
            ["PRIVATE"] = TokenType.Keyword,
            ["PROFILE"] = TokenType.Keyword,

            ["QUOTA"] = TokenType.Keyword,

            ["RECOVER"] = TokenType.Keyword,
            ["RESETLOGS"] = TokenType.Keyword,
            ["RESTRICTED"] = TokenType.Keyword,
            ["REUSE"] = TokenType.Keyword,
            ["ROLES"] = TokenType.Keyword,

            ["SAVEPOINT"] = TokenType.Keyword,
            ["SCN"] = TokenType.Keyword,
            ["SECTION"] = TokenType.Keyword,
            ["SEGMENT"] = TokenType.Keyword,
            ["SHARED"] = TokenType.Keyword,
            ["SNAPSHOT"] = TokenType.Keyword,
            ["SORT"] = TokenType.Keyword,
            ["STATEMENT_ID"] = TokenType.Keyword,
            ["STOP"] = TokenType.Keyword,
            ["SWITCH"] = TokenType.Keyword,

            ["TABLES"] = TokenType.Keyword,
            ["TABLESPACE"] = TokenType.Keyword,
            ["THREAD"] = TokenType.Keyword,
            ["TIME"] = TokenType.Keyword,
            ["TRACING"] = TokenType.Keyword,
            ["TRANSACTION"] = TokenType.Keyword,
            ["TRIGGERS"] = TokenType.Keyword,

            ["UNLIMITED"] = TokenType.Keyword,
            ["UNLOCK"] = TokenType.Keyword,
        };
*/
        
        public class SqlRegex
        {
            public SqlRegex(string pattern, TokenType tokenType)
            {
                Regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                TokenType = tokenType;
            }

            public Regex Regex { get; }
            public TokenType TokenType { get; }
        }
    }
}
