using System.Linq;
using System.Text;

namespace Parrot.Nodes
{
    public class StatementList : AbstractNodeList<Statement>
    {

        public StatementList(params Statement[] nodes) : base()
        {
            _list.AddRange(nodes);
        }

        public StatementList(StatementList list, params Statement[] nodes) : base()
        {
            if (list != null)
            {
                _list.AddRange(list);
            }

            if (nodes != null)
            {
                _list.AddRange(nodes);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var element in this.Select(e => e.SetModel(Model)))
            {
                sb.Append(element.ToString());
            }

            return sb.ToString();
        }
    }

}