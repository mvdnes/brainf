using System;

namespace BrainfCompiler
{
    class Parser
    {
        private Lexer lexer;

        private Parser(Lexer lexer)
        {
            this.lexer = lexer;
        }

        public static ASTNode run(Lexer lexer)
        {
            Parser parser = new Parser(lexer);
            return parser.getNode();
        }

        private ASTNode getNode()
        {
            return this.getNodeFromToken(this.lexer.getNext());
        }

        private ASTNode getNodeFromToken(BFToken token)
        {
            ASTNode next_node;
            switch (token)
            {
                case BFToken.LoopStart:
                    return this.getLoop();
                case BFToken.LoopEnd:
                case BFToken.EOF:
                    return new ASTNodeLeaf();
                case BFToken.Plus:
                case BFToken.Minus:
                    return this.getComposed(BFToken.Plus, BFToken.Minus, token, ASTNodeType.Plus);
                case BFToken.Left:
                case BFToken.Right:
                    return this.getComposed(BFToken.Right, BFToken.Left, token, ASTNodeType.Right);
                case BFToken.Read:
                    next_node = this.getNode();
                    return new ASTNodeUnary(ASTNodeType.Read, next_node);
                case BFToken.Write:
                    next_node = this.getNode();
                    return new ASTNodeUnary(ASTNodeType.Write, next_node);
                default:
                    throw new ArgumentOutOfRangeException("token");
            }
        }

        private ASTNode getLoop()
        {
            var inner = this.getNode();
            var next = this.getNode();

            return new ASTNodeLoop(inner, next);
        }

        private ASTNode getComposed(BFToken add, BFToken subtract, BFToken first, ASTNodeType nodeType)
        {
            int amount = (first == add) ? 1 : -1;
            BFToken next_token = this.lexer.getNext();

            while (next_token == add || next_token == subtract)
            {
                amount += (next_token == add) ? 1 : -1;
                next_token = this.lexer.getNext();
            }

            ASTNode next_node = this.getNodeFromToken(next_token);

            if (amount == 0) {
                return next_node;
            }
            else {
                switch (add)
                {
                    case BFToken.Plus:
                        return new ASTNodePlus(next_node, amount, 0);
                    case BFToken.Right:
                        return new ASTNodeRight(next_node, amount);
                    default:
                        throw new ArgumentException("add is not of type Plus or Right");
                }
            }
        }
    }
}
