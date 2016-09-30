using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFV_ProxyPrinter
{
    public abstract class Command
    {
        public abstract void Execute();
    }

    public abstract class UndoableCommand : Command
    {
        public abstract void Undo();
    }

    public class UpdateCountCommand : UndoableCommand
    {
        private Card card;
        private int previousCount;
        private int newCount;

        public UpdateCountCommand(Card card, int newCount)
        {
            this.card = card;
            previousCount = card.Count;
            this.newCount = newCount;
        }

        public override void Execute()
        {
            card.Count = newCount;
        }

        public override void Undo()
        {
            card.Count = previousCount;
        }
    }

    public class RemoveCommand : UndoableCommand
    {
        MainWindow window;
        private Card toRemove;
        private int oldIndex;

        public RemoveCommand(MainWindow window, Card card)
        {
            this.window = window;
            toRemove = card;
            oldIndex = window.Cards.IndexOf(card);
        }

        public override void Execute()
        {
            window.Cards.Remove(toRemove);
        }

        public override void Undo()
        {
            window.Cards.Insert(oldIndex, toRemove);
        }
    }

    public class AddCommand : UndoableCommand
    {
        MainWindow window;
        private Card toAdd;

        public AddCommand(MainWindow window, Card card)
        {
            this.window = window;
            toAdd = card;
        }

        public override void Execute()
        {
            window.Cards.Add(toAdd);
        }

        public override void Undo()
        {
            window.Cards.Remove(toAdd);
        }
    }
}
