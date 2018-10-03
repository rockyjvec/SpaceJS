namespace Esprima.Ast
{
    public class Node : INode
    {
        private Location _location;

        public Nodes Type { get; set; }

        public int[] Range { get; set; }

        public Location Location
        {
            get { _location  = _location ?? new Location(); return _location;}
            set { _location = value; }
        }
    }
}
