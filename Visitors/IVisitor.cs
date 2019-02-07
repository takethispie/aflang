using System;
using System.Collections.Generic;

namespace aflang
{
    public interface IVisitor
    {
        void Visit(Root n);
        void Visit(Store n);
        void Visit(Const n);
        void Visit(Function n);
        void Visit(Load n);
        void Visit(Add n);
        void Visit(Sub n);
        void Visit(Mul n);
        void Visit(Div n);
        void Visit(Bool n);
    }
}