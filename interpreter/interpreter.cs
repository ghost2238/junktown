using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace interpreter
{
    public enum Tok
    {
        LEFT_PAREN,  // (
        RIGHT_PAREN, // )
        LEFT_BRACE,  // {
        RIGHT_BRACE, // }
        COMMA,       // ,
        DOT,         // .
        MINUS,       // -
        PLUS,        // +
        
        BACKSLASH,   // \
        SLASH,       // /
        STAR,        // *
        WORD,        // example

        ASSIGN,      // =
        EQ,          // ==
        NOT_EQ,      // !=
        GT,          // >
        GTE,         // >=
        LT,          // <
        LTE,         // <=

        NEWLINE,

        COMMENT,   
        STRING,  // "abc"
        NUMBER,  // 1234

        KEY_STRING, // string
        KEY_NUMBER, // num
        KEY_BOOL, // bool
        DECLARE, // let
        IF,      // if
        ELSE,    // else

        EOF
    }

    public enum VarType
    {
        BOOL,
        INT,
        STR,
        STRINTTUPLE // ("", 0)
    }

    public class Token
    {
        public Tok type;
        public object literal;
    }

    class Tokenizer
    {
        int i=0;
        int len;
        int line=1;
        string buffer;
        List<Token> tokens;

        public List<Token> Tokenize(string file)
        {
            tokens = new List<Token>();
            buffer = File.ReadAllText(file);
            len = buffer.Length - 1;
            while (i != len)
            {
                Scan();
            }
            tokens.Add(new Token
            {
                type = Tok.EOF
            });
            return tokens;
        }

        public void Scan()
        {
            void _n(Tok type)
                => tokens.Add(new Token { type = type }); 
            void _nr(Tok type)
            {
                _n(type);
                Read();
            }

            var t = Read();
            switch (t)
            {
                case ',': _n(Tok.COMMA); break;
                case '.': _n(Tok.DOT); break;
                case '\\':_n(Tok.BACKSLASH); break;
                case '(': _n(Tok.LEFT_PAREN); break;
                case ')': _n(Tok.RIGHT_PAREN); break;
                case '{': _n(Tok.LEFT_BRACE); break;
                case '}': _n(Tok.RIGHT_BRACE); break;
                case '/':
                    {
                        var p = Peek();
                        if (p == '/')
                        {
                            Read();
                            Comment();
                        }
                    }
                    break;
                case '"': _String(); break;
                case '+': _n(Tok.PLUS); break;
                case '-': _n(Tok.MINUS); break;
                case '*': _n(Tok.STAR); break;
                case '=':
                    {
                        switch(Peek())
                        {
                            case '=': _nr(Tok.EQ); break;
                            case '<': _nr(Tok.LTE); break;
                            default: _n(Tok.ASSIGN); break;
                        }
                    } break;
                case '!':
                    {
                        if (Peek() == '=')
                            _nr(Tok.NOT_EQ);
                    } break;
                case '\r': break;
                case '\t': break;
                case '\n': line++; _n(Tok.NEWLINE); break;
                case ' ': break;
                default:
                {
                    if(char.IsLetter(t))
                    {
                        var word = ReadUntil(null, new char[] { ' ', '(', ')', ',', '\r', '\n' });
                        if (word == "let")
                        {
                            _nr(Tok.DECLARE);
                        } 
                        else if(word == "string")
                            _nr(Tok.KEY_STRING);
                        else if (word == "num")
                            _nr(Tok.KEY_NUMBER);
                        else if (word == "bool")
                            _nr(Tok.KEY_BOOL);
                        else
                        {
                            tokens.Add(
                                new Token { type = Tok.WORD, literal = word }
                            ); 
                        }
                    } else if (char.IsDigit(t))
                    {
                        var number = ReadUntil(null, new char[] { ' ', '(', ')', ',', '+', '-', '*', '\r', '\n' });
                        tokens.Add(
                            new Token { type = Tok.NUMBER, literal = int.Parse(number) }
                        );
                    }
                    else
                    {
                        throw new Exception($"unable to read '{t}' at pos {i}, line {line}");
                    }
                    break;
                }
            }
        }

        public void Comment()
        {
            i++;
            tokens.Add(new Token { type = Tok.COMMENT, literal = ReadUntil("comment", new char[]{ '\r', '\n' }) });
        }

        public void _String()
        {
            i++;
            tokens.Add(new Token { type = Tok.STRING, literal = ReadUntil("string", '"') });
            i++;
        }

        public string ReadUntil(string t, char c)
            => ReadUntil(t, new char[] { c });

        public string ReadUntil(string t, char[] c)
        {
            int spos = i;
            var sb = new StringBuilder();
            sb.Append(buffer[i-1]);
            while (true)
            {
                if (c.Any(x => x == Peek()))
                {
                    return sb.ToString();
                }
                sb.Append(Read());

                if (EOF())
                    throw new Exception($"unterminated {t}, starting from {spos}");
            }
        }


        public bool Digit()
            => char.IsDigit(buffer[i]);

        public bool EOF()
            => i == len;

        public char Read()
            => buffer[i++];

        public char Peek()
            => buffer[i];

    }

    class Parser
    {
        List<Token> tokens;
        Function currentFunction;
        public List<Function> functions;

        int i;
        int len;

        public Parser(string file)
        {
            var tokenizer = new Tokenizer();
            tokens = tokenizer.Tokenize(file);
            tokens.RemoveAll(x => x.type == Tok.COMMENT);
            functions = new List<Function>();
            len = tokens.Count;
            while(i < len)
            {
                var c = tokens[i++];
                switch (c.type)
                {
                    case Tok.COMMENT: break;
                    case Tok.WORD:
                    {
                        if(PeekType() == Tok.LEFT_PAREN)
                        {
                            Read();
                            var argTokens = ParseUntil(Tok.RIGHT_PAREN);
                            Read();
                            var pt = PeekType();
                            if (pt == Tok.LEFT_BRACE)
                            {
                                var func = new Function
                                {
                                    name=(string)c.literal
                                };
                                if (argTokens.Any())
                                {
                                    func.arguments = ParseArgs(argTokens);
                                }
                                currentFunction = func;
                                functions.Add(func);
                            }
                        }

                    }; break;
                    case Tok.DECLARE: // TODO: Check if variable is declared already.
                    {
                        var variable = Peek();
                        if(variable.type != Tok.WORD)
                            throw new Exception($"expected word token for declaration, {tokens[i].type.ToString()} encountered.");
                            var expr = ParseUntil(new Tok[] { Tok.NEWLINE });

                            currentFunction.instructions.Add(new Instruction
                            {
                                type = OpType.OP_DECLARE,
                                data = expr
                            });

                    } break;
                }
            }
        }

        public VarType VarTypeFromWord(Token tok)
        {                
            switch (tok.type)
            {
                case Tok.KEY_STRING: return VarType.STR;
                case Tok.KEY_NUMBER: return VarType.INT;
                case Tok.KEY_BOOL: return VarType.BOOL;
                default: throw new Exception($"not a valid variable type token, {tokens[i].type.ToString()} encountered.");
            }
        }

        public List<Argument> ParseArgs(List<Token> tokens)
        {
            var args = new List<Argument>();
            int i = 0;
            // TODO: Check if names are unique
            while (i<tokens.Count) {
                var type = tokens[i++];
                var name = tokens[i++];
                args.Add(new Argument
                {
                    type= VarTypeFromWord(type),
                    name=(string)name.literal
                });

                if (i == tokens.Count())
                    break;
                if (tokens[i].type != Tok.COMMA)
                    throw new Exception($"comma expected, {tokens[i].type.ToString()} encountered.");
                i++;
            }
            return args;
        }

        public List<Token> ParseUntil(Tok type)
            => ParseUntil(new Tok[] { type });

        public List<Token> ParseUntil(Tok[] type)
        {
            var toks = new List<Token>();
            while (!type.Any(x => x == PeekType()))
                toks.Add(Read());
            return toks;
        }

        public Tok PeekType(int c = 0)
            => tokens[i+c].type;

        public Token Read()
            => tokens[i++];

        public Token Peek(int c=0)
            => tokens[i+c];
    }

    public enum OpType
    {
        OP_DECLARE,
        OP_ASSIGN,
        OP_ADD,
        OP_SUB,
        OP_MUL,
        OP_CALL
    }

    public class Instruction
    {
        public OpType type;
        public object data;
    }

    class Expression
    {
        public List<Token> token;
    }

    public class Argument 
    {
        public string name;
        public VarType type;
    }

    public class Function
    {
        public string name;
        public List<Argument> arguments;
        public List<Instruction> instructions = new List<Instruction>();
        public VarType retType;
    }

    class Variable
    {
        public string name;
        public VarType type;
        public object value;
        public bool array;
    }

    public class Interpreter
    {
        Dictionary<string, Variable> globals = new Dictionary<string, Variable>();
        Dictionary<string, Variable> local;
        List<object> stack;

        public void LoadScript(string file)
        {
            var parser = new Parser(file);
            Run(parser.functions.SingleOrDefault(x => x.name == "main"));
        }

        public void Run(Function func)
        {
            foreach(var inst in func.instructions)
            {
                switch(inst.type)
                {
                    case OpType.OP_DECLARE:
                    {
                        var tokens = (List<Token>)inst.data;
                        var name = (string)tokens[0].literal;
                        object val = null;
                        if(tokens.Count>1)
                        {
                            val = Evaluate(tokens.GetRange(2, tokens.Count - 2));
                        }
                        globals[name] = new Variable
                        {
                            name=name,
                            value=val,
                            type=VarType.INT
                        };

                    } break;
                }
            }
        }

        // https://en.wikipedia.org/wiki/Shunting-yard_algorithm
        public object Evaluate(List<Token> expr)
        {
            var op = new Stack<Token>();
            var data = new Stack<Token>();

            int i = 0;
            while (i<expr.Count)
            {
                var t = expr[i++];
                switch(t.type)
                {
                    case Tok.NUMBER:
                    {
                        data.Push(t);
                        /*if(op.Any() && op.Peek().type == Tok.STAR)
                        {
                            data.Push(op.Pop());
                        }*/
                    } break;
                    case Tok.STAR:
                    case Tok.LEFT_PAREN:
                    case Tok.PLUS:
                    case Tok.MINUS:
                    case Tok.SLASH:
                        op.Push(t); break;
                    case Tok.WORD:
                    {
                        var variable = globals[(string)t.literal];
                        var numtok = new Token
                        {
                            literal=variable.value,
                            type=Tok.NUMBER
                        };

                        op.Push(numtok);
                    } break;
                    case Tok.RIGHT_PAREN:
                    {
                        while (true)
                        {
                            var parenTok = op.Pop();
                            if (parenTok.type == Tok.LEFT_PAREN)
                                break;
                            data.Push(parenTok);
                            /*if (op.Any() && op.Peek().type == Tok.STAR)
                            {
                                data.Push(op.Pop());
                            }*/
                        }
                    } break;

                    default:
                        throw new Exception($"Invalid expression token {t.type.ToString()}");
                }
            }

            while(op.Count>0)
                data.Push(op.Pop());

            var result = new Stack<int>();
            foreach(var t in data.Reverse())
            {
                //var t = data.Pop();
                if (t.type == Tok.NUMBER)
                    result.Push((int)t.literal);
                if (t.type == Tok.MINUS || t.type == Tok.PLUS || t.type == Tok.STAR || t.type == Tok.SLASH)
                {
                    int i2 = result.Pop();
                    int i1 = result.Pop();
                    if (t.type== Tok.MINUS)  result.Push(i1 - i2);
                    if (t.type == Tok.PLUS)  result.Push(i1 + i2);
                    if (t.type == Tok.STAR)  result.Push(i1 * i2);
                    if (t.type == Tok.SLASH) result.Push(i1 / i2);
                }
            }

            return result.Pop();
        }


    }
}
