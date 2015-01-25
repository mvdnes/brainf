using System;
using System.IO;

namespace BrainfCompiler
{
    enum BFToken
    {
        Plus,
        Minus,
        Left,
        Right,
        LoopStart,
        LoopEnd,
        Read,
        Write,
        EOF,
    }

    class Lexer
    {
        StreamReader reader;

        public Lexer(StreamReader reader)
        {
            this.reader = reader;
        }

        public BFToken getNext()
        {
            BFToken? token;
            do
            {
                token = getToken();
            }
            while (!token.HasValue);

            return token.Value;
        }

        private BFToken? getToken()
        {
            int readerOut = this.reader.Read();
            if (readerOut == -1)
            {
                return BFToken.EOF;
            }

            char kar = (char)readerOut;
            switch (kar)
            {
                case '+':
                    return BFToken.Plus;
                case '-':
                    return BFToken.Minus;
                case '<':
                    return BFToken.Left;
                case '>':
                    return BFToken.Right;
                case '[':
                    return BFToken.LoopStart;
                case ']':
                    return BFToken.LoopEnd;
                case '.':
                    return BFToken.Write;
                case ',':
                    return BFToken.Read;
            }

            return null;
        }
    }
}
