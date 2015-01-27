using System;
using System.Collections;
using System.Collections.Generic;

namespace BrainfCompiler
{
    enum ASTNodeType
    {
        Plus,
        Right,
        Loop,
        Read,
        Write,
        Leaf,
        CopyLoop,
    };

    abstract class ASTNode
    {
        public ASTNodeType nodeType { get; protected set; }

        public abstract ASTNode nextChild();
        public abstract void setNext(ASTNode child);
    }

    class ASTNodeLeaf : ASTNode
    {
        public ASTNodeLeaf()
        {
            this.nodeType = ASTNodeType.Leaf;
        }

        public override ASTNode nextChild()
        {
            return null;
        }

        public override void setNext(ASTNode child)
        {
            throw new NotImplementedException();
        }
    }

    class ASTNodeUnary : ASTNode
    {
        protected ASTNode child;

        public ASTNodeUnary(ASTNodeType type, ASTNode child)
            : base()
        {
            if (child == null) throw new ArgumentNullException();
            switch (type)
            {
                case ASTNodeType.Read:
                case ASTNodeType.Write:
                    break;
                default:
                    throw new ArgumentException("Invalid unary node type");
            }

            this.child = child;
            this.nodeType = type;
        }

        protected ASTNodeUnary(ASTNode child)
            : base()
        {
            if (child == null) throw new ArgumentNullException();
            this.child = child;
        }

        public override ASTNode nextChild()
        {
            return this.child;
        }

        public override void setNext(ASTNode child)
        {
            if (child == null) throw new ArgumentNullException();
            this.child = child;
        }
    }

    class ASTNodePlus : ASTNodeUnary
    {
        public int amount;
        public int offset;

        public ASTNodePlus(ASTNode child, int amount, int offset)
            : base(child)
        {
            this.amount = amount;
            this.offset = offset;
            this.nodeType = ASTNodeType.Plus;
        }
    }

    class ASTNodeRight : ASTNodeUnary
    {
        public int amount;

        public ASTNodeRight(ASTNode child, int amount)
            : base(child)
        {
            this.amount = amount;
            this.nodeType = ASTNodeType.Right;
        }
    }

    class ASTNodeLoop : ASTNode
    {
        protected ASTNode loop_node, next_node;

        public ASTNodeLoop(ASTNode loop, ASTNode next)
        {
            if (loop == null) throw new ArgumentNullException();
            if (next == null) throw new ArgumentNullException();

            this.loop_node = loop;
            this.next_node = next;
            this.nodeType = ASTNodeType.Loop;
        }

        public override ASTNode nextChild()
        {
            return this.next_node;
        }

        public override void setNext(ASTNode child)
        {
            if (child == null) throw new ArgumentNullException();
            this.next_node = child;
        }

        public ASTNode innerChild()
        {
            return this.loop_node;
        }

        public void setInner(ASTNode child)
        {
            if (child == null) throw new ArgumentNullException();
            this.loop_node = child;
        }
    }

    class ASTNodeCopyLoop : ASTNodeUnary, IEnumerable<CopyLoopData>
    {
        protected Dictionary<int, int> copy_values;
        
        public ASTNodeCopyLoop(ASTNode next, Dictionary<int, int> values)
            : base(next)
        {
            this.copy_values = values;
            this.nodeType = ASTNodeType.CopyLoop;
        }

        public bool isEmpty()
        {
            return copy_values.Count == 0;
        }

        public IEnumerator<CopyLoopData> GetEnumerator()
        {
            foreach (var entry in this.copy_values)
            {
                yield return new CopyLoopData(entry.Key, entry.Value);
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    class CopyLoopData
    {
        public int offset, factor;

        public CopyLoopData(int offset, int factor)
        {
            this.offset = offset;
            this.factor = factor;
        }
    }
}
