using System.Text;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components;

namespace Blogzor.Code
{
    public abstract class BlogLexerOutput
    {
        public static readonly OutputEmpty Empty = new OutputEmpty();
        public abstract int Output(int sequence, RenderTreeBuilder builder);

        public class OutputEmpty : BlogLexerOutput
        {
            public override int Output(int sequence, RenderTreeBuilder builder)
            {
                return sequence;
            }
        }

        public class OutputText : BlogLexerOutput
        {
            string output;

            public OutputText(string _string) => output = _string;

            public override int Output(int sequence, RenderTreeBuilder builder)
            {
                builder.AddMarkupContent(++sequence, output);
                return sequence;
            }
        }

        public class OutputButton : BlogLexerOutput
        {
            private string _text;

            public OutputButton(string text)
            {
                _text = text;
            }

            public override int Output(int sequence, RenderTreeBuilder builder)
            {
                builder.OpenComponent(++sequence, typeof(MudBlazor.MudButton));
                builder.AddAttribute(++sequence, "ChildContent",
                    (RenderFragment)(b2 =>
                    {
                        b2.AddContent(0, _text);
                    }));
                builder.AddAttribute(++sequence, "Variant", MudBlazor.Variant.Text);
                builder.AddAttribute(++sequence, "Color", MudBlazor.Color.Info);
                builder.AddAttribute(++sequence, "Size", MudBlazor.Size.Small);
                builder.CloseComponent();
                return sequence;
            }
        }

        public class OutputLink : BlogLexerOutput
        {

            private string _text, _link;
            public OutputLink(string text, string link) { _text = text; _link = link; }


            public override int Output(int sequence, RenderTreeBuilder builder)
            {
                builder.OpenComponent<MudBlazor.MudLink>(++sequence);
                builder.AddAttribute(++sequence, "Href", _link);
                builder.AddAttribute(++sequence, "ChildContent",
                    (RenderFragment)(b2 =>
                    {
                        b2.AddContent(0, _text);
                    }));
                builder.AddAttribute(++sequence, "Size", MudBlazor.Size.Small);
                builder.CloseComponent();
                return sequence;
            }
        }
    }

    public abstract class BlogLexerParser
    {
        public abstract BlogLexerOutput Parse(StringReader sr);

        public class ParseText : BlogLexerParser
        {
            public override BlogLexerOutput Parse(StringReader sr)
            {
                StringBuilder sb = new StringBuilder();
                while (sr.Peek() != BlogLexer.SYMBOL_CLOSE)
                    sb.Append(sr.Read());
                return new BlogLexerOutput.OutputText(sb.ToString());
            }
        }

        public class ParseButton : BlogLexerParser
        {
            public override BlogLexerOutput Parse(StringReader sr)
            {
                StringBuilder sb = new StringBuilder();
                while (sr.Peek() != BlogLexer.SYMBOL_CLOSE)
                    sb.Append((char)sr.Read());
                return new BlogLexerOutput.OutputButton(sb.ToString());
            }
        }

        public class ParseLink : BlogLexerParser
        {
            public override BlogLexerOutput Parse(StringReader sr)
            {
                StringBuilder textBuilder = new();
                StringBuilder linkBuilder = new();
                while (sr.Peek() != BlogLexer.SYMBOL_CLOSE)
                {
                    char c = (char)sr.Read();
                    if (c == BlogLexer.VAR_START)
                    {
                        while (sr.Peek() != BlogLexer.SYMBOL_CLOSE)
                        {
                            linkBuilder.Append((char)sr.Read());
                        }
                        break;
                    }
                    textBuilder.Append(c);
                }

                return new BlogLexerOutput.OutputLink(textBuilder.ToString(), linkBuilder.ToString());
            }
        }

        public static BlogLexerParser ToParser(char c) => c switch
        {
            'b' => new ParseButton(),
            'l' => new ParseLink(),
            _ => new ParseText()
        };

        public static BlogLexerOutput ParseSymbol(StringReader sr)
        {
            if (sr.Peek() != BlogLexer.SYMBOL_OPEN)
            {
                return BlogLexerOutput.Empty;
            }

            sr.Read(); //SYMBOL_OPEN 
            char sympolType = (char)sr.Read();
            sr.SkipReturnOrSpace();
            var parser = ToParser(sympolType);
            var output = parser.Parse(sr);
            if (sr.Peek() != BlogLexer.SYMBOL_CLOSE)
            {
                throw new InvalidOperationException
                    ("parser did not leave string reader at symbol close!");
            }
            sr.Read(); //SYMBOL_CLOSE
            return output;
        }
    }

    public static class BlogLexerExtensions
    {
        public static char ReadNextNoReturn(this StringReader sr)
        {
            while (sr.Peek() == '\r' || sr.Peek() == '\n')
                sr.Read();
            return (char)sr.Read();
        }

        public static void SkipReturn(this StringReader sr)
        {
            while (sr.Peek() == '\r' || sr.Peek() == '\n')
                sr.Read();
        }

        public static void SkipReturnOrSpace(this StringReader sr)
        {
            while (sr.Peek() == '\r' || sr.Peek() == '\n' || sr.Peek() == ' ')
                sr.Read();
        }
    }

    public class BlogLexer
    {
        public const char SYMBOL_START = '$';
        public const char VAR_START = '@';
        public const char SYMBOL_OPEN = '[';
        public const char SYMBOL_CLOSE = ']';

        private string _text;

        public BlogLexer(string text)
        {
            _text = text;
        }

        public List<BlogLexerOutput> Lex(bool debug = false)
        {
            List<BlogLexerOutput> outputs = new List<BlogLexerOutput>();
            using (StringReader sr = new StringReader(_text))
            {
                StringBuilder sb = new StringBuilder();

                void AppendTextOutput()
                {
                    outputs.Add(new BlogLexerOutput.OutputText(sb.ToString()));
                    sb.Clear();
                }

                while (sr.Peek() != -1)
                {
                    char curr = (char)sr.Read();
                    char peek = (char)sr.Peek();
                    switch (curr)
                    {
                        case SYMBOL_START:
                            if (debug)
                                goto default;
                            AppendTextOutput();
                            var symbolOutput = BlogLexerParser.ParseSymbol(sr);
                            outputs.Add(symbolOutput); break;
                        case '\r':
                        case '\n':
                            if (peek == '\r' || peek == '\n')
                                sr.Read();
                            sb.Append("<br />"); break;
                        default:
                            sb.Append(curr); break;
                    }
                }
                if (sb.Length != 0)
                    AppendTextOutput();
            }
            return outputs;
        }
    }
}
