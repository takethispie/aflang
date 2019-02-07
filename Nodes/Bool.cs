using System.Collections.Generic;

namespace aflang
{
    public class Bool : INode
    {
        public List<INode> Childrens { get; set; }
        public string Value { get; set; }

        public void Accept(IVisitor v)
        {
            //v.Visit(this);
        }
    }
}