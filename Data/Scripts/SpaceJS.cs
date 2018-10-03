using VRage.Game.Components;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using Sandbox.Game.Entities.Character;
using VRage.Game;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using VRage.Game.GUI.TextPanel;
using Sandbox.Game.Entities;
using Esprima;

namespace rockyjvec.SpaceJS
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SpaceJS : MySessionComponentBase
    {
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            var parser = new JavaScriptParser("const answer = 42");
            var program = parser.ParseProgram();

            MyAPIGateway.Utilities.ShowMessage("SpaceJS", "Got Here!!" + program.Body);


        }
/*int interpret(tree t)
 { // left to right, top down scan of tree 
   switch (t->nodetype) {
     case NodeTypeInt:
        return t->value;
     case NodeTypeVariable:
        return t->symbtable_entry->value
     case NodeTypeAdd:
        { int leftvalue= interpret(t->leftchild);
          int rightvalue= interpret(t->rightchild);
          return leftvalue+rightvalue;
        }
     case NodeTypeMultiply:
        { int leftvalue= interpret(t->leftchild);
          int rightvalue= interpret(t->rightchild);
          return leftvalue*rightvalue;
        }
     ...
     case NodeTypeStatementSequence: // assuming a right-leaning tree
        { interpret(t->leftchild);
          interpret(t->rightchild);
          return 0;
        }
     case NodeTypeAssignment:
        { int right_value=interpret(t->rightchild);
          assert: t->leftchild->Nodetype==NodeTypeVariable;
          t->leftchild->symbtable_entry->value=right_value;
          return right_value;
        }
     case NodeTypeCompareForEqual:
        { int leftvalue= interpret(t->leftchild);
          int rightvalue= interpret(t->rightchild);
          return leftvalue==rightvalue;
        }
     case NodeTypeIfThenElse
        { int condition=interpret(t->leftchild);
          if (condition) interpret(t->secondchild);
          else intepret(t->thirdchild);
          return 0;
     case NodeTypeWhile
        { int condition;
          while (condition=interpret(t->leftchild))
                interpret(t->rightchild);
          return 0;

     ...
   }
 }*/
 }
}

