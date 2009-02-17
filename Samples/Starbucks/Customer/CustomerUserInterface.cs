using System;

namespace Starbucks.Customer
{
    public class CustomerUserInterface
    {
        public virtual bool ShouldPayForDrink(string name, decimal amount)
        {
            Console.WriteLine("{0}: Payment due is: {1} y/n?", name, amount);
            var key = Console.ReadKey();
            Console.WriteLine();
            return key.KeyChar == 'y';
        }

        public virtual void CoffeeRush(string name)
        {
            Console.WriteLine("{0}: Got the drink, coffee rush!", name);
        }
    }
}