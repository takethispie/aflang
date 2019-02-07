using System.Collections.Generic;

namespace aflang
{
    public class Call : INode 
    {
        public List<INode> Childrens { get; set; }
        public string Value { get; set; }

        public Call(string name)
        {
            this.Value = name;
        }
        
        public void Accept(IVisitor v)
        {
            throw new System.NotImplementedException();
        }
    }
}